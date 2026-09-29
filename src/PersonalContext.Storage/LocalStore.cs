using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using PersonalContext.Core;

namespace PersonalContext.Storage;

public sealed class LocalStore
{
    private readonly string _databasePath;

    public LocalStore(string? databasePath = null)
    {
        _databasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PersonalContextHome", "data", "personal-context.db");
    }

    public string DatabasePath => _databasePath;

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_databasePath))!);
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var versionCommand = connection.CreateCommand();
        versionCommand.Transaction = transaction;
        versionCommand.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(versionCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (version == 1)
        {
            transaction.Commit();
            return;
        }
        if (version != 0) throw new NotSupportedException($"Unsupported database version: {version}");

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE providers (
                id INTEGER PRIMARY KEY,
                key TEXT NOT NULL UNIQUE,
                display_name TEXT NOT NULL
            );
            CREATE TABLE projects (
                id INTEGER PRIMARY KEY,
                provider_id INTEGER NOT NULL REFERENCES providers(id),
                external_id TEXT NOT NULL,
                name TEXT NOT NULL,
                observed_at TEXT NOT NULL,
                UNIQUE(provider_id, external_id)
            );
            CREATE TABLE conversations (
                id INTEGER PRIMARY KEY,
                provider_id INTEGER NOT NULL REFERENCES providers(id),
                external_id TEXT NOT NULL,
                project_id INTEGER REFERENCES projects(id),
                title TEXT NOT NULL,
                first_seen_at TEXT NOT NULL,
                last_seen_at TEXT NOT NULL,
                UNIQUE(provider_id, external_id)
            );
            CREATE INDEX ix_conversations_project ON conversations(project_id);
            CREATE TABLE messages (
                id INTEGER PRIMARY KEY,
                conversation_id INTEGER NOT NULL REFERENCES conversations(id),
                source_key TEXT NOT NULL,
                role TEXT NOT NULL CHECK(role IN ('user', 'assistant', 'system', 'tool')),
                ordinal INTEGER NOT NULL,
                body TEXT NOT NULL,
                body_hash TEXT NOT NULL,
                observed_at TEXT NOT NULL,
                UNIQUE(conversation_id, source_key)
            );
            CREATE INDEX ix_messages_order ON messages(conversation_id, ordinal);
            CREATE TABLE message_revisions (
                id INTEGER PRIMARY KEY,
                message_id INTEGER NOT NULL REFERENCES messages(id),
                role TEXT NOT NULL,
                ordinal INTEGER NOT NULL,
                body TEXT NOT NULL,
                body_hash TEXT NOT NULL,
                observed_at TEXT NOT NULL,
                replaced_at TEXT NOT NULL
            );
            CREATE INDEX ix_message_revisions_message ON message_revisions(message_id);
            CREATE TABLE capture_state (
                conversation_id INTEGER PRIMARY KEY REFERENCES conversations(id),
                provider_id INTEGER NOT NULL REFERENCES providers(id),
                last_observed_at TEXT NOT NULL,
                last_success_at TEXT NOT NULL,
                last_error TEXT,
                coverage TEXT NOT NULL
            );
            PRAGMA user_version = 1;
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public IngestResult Ingest(ProviderObservation observation)
    {
        ObservationValidator.Validate(observation);
        Initialize();
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var now = observation.ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture);

        Execute(connection, transaction,
            "INSERT INTO providers(key, display_name) VALUES ($key, $name) " +
            "ON CONFLICT(key) DO UPDATE SET display_name = excluded.display_name;",
            ("$key", observation.ProviderKey), ("$name", observation.ProviderName));
        var providerId = ScalarLong(connection, transaction,
            "SELECT id FROM providers WHERE key = $key;", ("$key", observation.ProviderKey));

        long? projectId = null;
        if (observation.ProjectExternalId is not null)
        {
            Execute(connection, transaction,
                "INSERT INTO projects(provider_id, external_id, name, observed_at) " +
                "VALUES ($provider, $external, $name, $seen) " +
                "ON CONFLICT(provider_id, external_id) DO UPDATE SET " +
                "name = excluded.name, observed_at = excluded.observed_at;",
                ("$provider", providerId), ("$external", observation.ProjectExternalId),
                ("$name", observation.ProjectName), ("$seen", now));
            projectId = ScalarLong(connection, transaction,
                "SELECT id FROM projects WHERE provider_id = $provider AND external_id = $external;",
                ("$provider", providerId), ("$external", observation.ProjectExternalId));
        }

        Execute(connection, transaction,
            "INSERT INTO conversations(provider_id, external_id, project_id, title, first_seen_at, last_seen_at) " +
            "VALUES ($provider, $external, $project, $title, $seen, $seen) " +
            "ON CONFLICT(provider_id, external_id) DO UPDATE SET " +
            "project_id = COALESCE(excluded.project_id, conversations.project_id), " +
            "title = excluded.title, last_seen_at = excluded.last_seen_at;",
            ("$provider", providerId), ("$external", observation.ConversationExternalId),
            ("$project", projectId), ("$title", observation.Title), ("$seen", now));
        var conversationId = ScalarLong(connection, transaction,
            "SELECT id FROM conversations WHERE provider_id = $provider AND external_id = $external;",
            ("$provider", providerId), ("$external", observation.ConversationExternalId));

        var inserted = 0;
        var revised = 0;
        var unchanged = 0;
        foreach (var message in observation.Messages)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(message.Body)));
            using var lookup = CreateCommand(connection, transaction,
                "SELECT id, role, ordinal, body, body_hash, observed_at FROM messages " +
                "WHERE conversation_id = $conversation AND source_key = $key;",
                ("$conversation", conversationId), ("$key", message.SourceKey));
            using var reader = lookup.ExecuteReader();
            if (!reader.Read())
            {
                reader.Close();
                Execute(connection, transaction,
                    "INSERT INTO messages(conversation_id, source_key, role, ordinal, body, body_hash, observed_at) " +
                    "VALUES ($conversation, $key, $role, $ordinal, $body, $hash, $seen);",
                    ("$conversation", conversationId), ("$key", message.SourceKey),
                    ("$role", message.Role), ("$ordinal", message.Ordinal),
                    ("$body", message.Body), ("$hash", hash), ("$seen", now));
                inserted++;
                continue;
            }

            var messageId = reader.GetInt64(0);
            var previousRole = reader.GetString(1);
            var previousOrdinal = reader.GetInt32(2);
            var previousBody = reader.GetString(3);
            var previousHash = reader.GetString(4);
            var previousObservedAt = reader.GetString(5);
            reader.Close();
            if (previousHash == hash && previousBody == message.Body &&
                previousRole == message.Role && previousOrdinal == message.Ordinal)
            {
                unchanged++;
                continue;
            }

            Execute(connection, transaction,
                "INSERT INTO message_revisions(message_id, role, ordinal, body, body_hash, observed_at, replaced_at) " +
                "VALUES ($id, $role, $ordinal, $body, $hash, $seen, $replaced);",
                ("$id", messageId), ("$role", previousRole), ("$ordinal", previousOrdinal),
                ("$body", previousBody), ("$hash", previousHash),
                ("$seen", previousObservedAt), ("$replaced", now));
            Execute(connection, transaction,
                "UPDATE messages SET role = $role, ordinal = $ordinal, body = $body, " +
                "body_hash = $hash, observed_at = $seen WHERE id = $id;",
                ("$id", messageId), ("$role", message.Role), ("$ordinal", message.Ordinal),
                ("$body", message.Body), ("$hash", hash), ("$seen", now));
            revised++;
        }

        Execute(connection, transaction,
            "INSERT INTO capture_state(conversation_id, provider_id, last_observed_at, " +
            "last_success_at, last_error, coverage) VALUES ($conversation, $provider, $seen, $seen, NULL, 'observed_page') " +
            "ON CONFLICT(conversation_id) DO UPDATE SET last_observed_at = excluded.last_observed_at, " +
            "last_success_at = excluded.last_success_at, last_error = NULL, coverage = excluded.coverage;",
            ("$conversation", conversationId), ("$provider", providerId), ("$seen", now));
        transaction.Commit();
        return new IngestResult(conversationId, inserted, revised, unchanged);
    }

    public void RecordCaptureError(long conversationId, string error, DateTimeOffset observedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(error) || error.Length > 2000)
            throw new ArgumentException("Error must contain 1 to 2000 characters.", nameof(error));
        if (observedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Observed time must be in UTC.", nameof(observedAtUtc));

        Initialize();
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, null,
            "UPDATE capture_state SET last_observed_at = $seen, last_error = $error " +
            "WHERE conversation_id = $conversation;",
            ("$seen", observedAtUtc.ToString("O", CultureInfo.InvariantCulture)),
            ("$error", error), ("$conversation", conversationId));
        if (command.ExecuteNonQuery() == 0)
            throw new ArgumentException("Unknown conversation or capture state.", nameof(conversationId));
    }

    public IReadOnlyList<ConversationSummary> GetConversations()
    {
        Initialize();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id, p.key, c.external_id, c.title, pr.name, COUNT(m.id)
            FROM conversations c
            JOIN providers p ON p.id = c.provider_id
            LEFT JOIN projects pr ON pr.id = c.project_id
            LEFT JOIN messages m ON m.conversation_id = c.id
            GROUP BY c.id
            ORDER BY c.last_seen_at DESC, c.id DESC;
            """;
        using var reader = command.ExecuteReader();
        var result = new List<ConversationSummary>();
        while (reader.Read())
            result.Add(new ConversationSummary(reader.GetInt64(0), reader.GetString(1),
                reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetInt32(5)));
        return result;
    }

    public IReadOnlyList<StoredMessage> GetMessages(long conversationId)
    {
        Initialize();
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, null,
            "SELECT m.id, m.source_key, m.role, m.ordinal, m.body, " +
            "(SELECT COUNT(*) FROM message_revisions r WHERE r.message_id = m.id) " +
            "FROM messages m WHERE m.conversation_id = $conversation ORDER BY m.ordinal, m.id;",
            ("$conversation", conversationId));
        using var reader = command.ExecuteReader();
        var result = new List<StoredMessage>();
        while (reader.Read())
            result.Add(new StoredMessage(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                reader.GetInt32(3), reader.GetString(4), reader.GetInt32(5)));
        return result;
    }

    public CaptureStatus? GetCaptureStatus(long conversationId)
    {
        Initialize();
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, null,
            "SELECT last_observed_at, last_success_at, last_error, coverage " +
            "FROM capture_state WHERE conversation_id = $conversation;",
            ("$conversation", conversationId));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new CaptureStatus(conversationId,
            DateTimeOffset.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
            DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
            reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetString(3));
    }

    private SqliteConnection OpenConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            DefaultTimeout = 5,
            Pooling = false
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        return connection;
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction? transaction,
        string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction,
        string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = CreateCommand(connection, transaction, sql, parameters);
        command.ExecuteNonQuery();
    }

    private static long ScalarLong(SqliteConnection connection, SqliteTransaction transaction,
        string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = CreateCommand(connection, transaction, sql, parameters);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
