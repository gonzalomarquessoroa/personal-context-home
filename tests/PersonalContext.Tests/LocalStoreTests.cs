using PersonalContext.Core;
using PersonalContext.Storage;

namespace PersonalContext.Tests;

public sealed class LocalStoreTests
{
    [Fact]
    public void InitializesAndReopensLocalDatabase()
    {
        using var fixture = new StoreFixture();
        fixture.Store.Initialize();

        Assert.True(File.Exists(fixture.Store.DatabasePath));
        Assert.Empty(new LocalStore(fixture.Store.DatabasePath).GetConversations());
    }

    [Fact]
    public async Task ConcurrentInitializationUsesOneMigration()
    {
        using var fixture = new StoreFixture();
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            new LocalStore(fixture.Store.DatabasePath).Initialize())));

        Assert.Empty(fixture.Store.GetConversations());
    }

    [Fact]
    public void RepeatedBatchAddsOnlyNewMessages()
    {
        using var fixture = new StoreFixture();
        var firstBatch = Observation(120);
        var first = fixture.Store.Ingest(firstBatch);
        var retry = fixture.Store.Ingest(firstBatch);
        var extension = fixture.Store.Ingest(Observation(130));

        Assert.Equal(120, first.InsertedMessages);
        Assert.Equal(0, retry.InsertedMessages);
        Assert.Equal(120, retry.UnchangedMessages);
        Assert.Equal(10, extension.InsertedMessages);
        Assert.Equal(120, extension.UnchangedMessages);
        Assert.Equal(first.ConversationId, extension.ConversationId);
        Assert.Single(fixture.Store.GetConversations());
        Assert.Equal(130, fixture.Store.GetMessages(first.ConversationId).Count);
        Assert.Equal("observed_page", fixture.Store.GetCaptureStatus(first.ConversationId)?.Coverage);
    }

    [Fact]
    public void ChangedMessageKeepsPreviousVersion()
    {
        using var fixture = new StoreFixture();
        var first = fixture.Store.Ingest(Observation(1));
        var changed = Observation(1) with
        {
            Messages = [new MessageObservation("message-0", "user", 0, "Precio final: 750 €")]
        };
        var result = fixture.Store.Ingest(changed);
        var retry = fixture.Store.Ingest(changed);
        var message = Assert.Single(fixture.Store.GetMessages(first.ConversationId));

        Assert.Equal(1, result.RevisedMessages);
        Assert.Equal(0, retry.RevisedMessages);
        Assert.Equal("Precio final: 750 €", message.Body);
        Assert.Equal(1, message.RevisionCount);
    }

    [Fact]
    public void InvalidBatchWritesNoNewMessages()
    {
        using var fixture = new StoreFixture();
        var first = fixture.Store.Ingest(Observation(1));
        var invalid = Observation(3) with
        {
            Messages =
            [
                new MessageObservation("message-1", "assistant", 1, "válido"),
                new MessageObservation("", "user", 2, "inválido")
            ]
        };

        Assert.Throws<ArgumentException>(() => fixture.Store.Ingest(invalid));
        Assert.Single(fixture.Store.GetMessages(first.ConversationId));
    }

    [Fact]
    public void ProjectCanBeAssociatedOnLaterObservation()
    {
        using var fixture = new StoreFixture();
        var first = fixture.Store.Ingest(Observation(1));
        var withProject = Observation(1) with
        {
            ProjectExternalId = "project-123",
            ProjectName = "Automatización inmobiliaria"
        };
        fixture.Store.Ingest(withProject);

        var conversation = Assert.Single(fixture.Store.GetConversations());
        Assert.Equal(first.ConversationId, conversation.Id);
        Assert.Equal("Automatización inmobiliaria", conversation.ProjectName);
    }

    [Fact]
    public void RoleAndOrderChangesAreRevisions()
    {
        using var fixture = new StoreFixture();
        var first = fixture.Store.Ingest(Observation(1));
        var corrected = Observation(1) with
        {
            Messages = [new MessageObservation("message-0", "assistant", 2, "Contenido 0")]
        };

        Assert.Equal(1, fixture.Store.Ingest(corrected).RevisedMessages);
        var message = Assert.Single(fixture.Store.GetMessages(first.ConversationId));
        Assert.Equal("assistant", message.Role);
        Assert.Equal(2, message.Ordinal);
        Assert.Equal(1, message.RevisionCount);
    }

    [Fact]
    public void CaptureErrorIsVisibleAndClearsAfterSuccess()
    {
        using var fixture = new StoreFixture();
        var first = fixture.Store.Ingest(Observation(1));
        fixture.Store.RecordCaptureError(first.ConversationId, "No se pudo leer la página",
            DateTimeOffset.Parse("2026-09-29T12:05:00Z"));
        Assert.Equal("No se pudo leer la página",
            fixture.Store.GetCaptureStatus(first.ConversationId)?.LastError);

        fixture.Store.Ingest(Observation(1));
        Assert.Null(fixture.Store.GetCaptureStatus(first.ConversationId)?.LastError);
    }

    private static ProviderObservation Observation(int messageCount) => new(
        "chatgpt", "ChatGPT", "conversation-123", "Roadmap",
        null, null, DateTimeOffset.Parse("2026-09-29T12:00:00Z"),
        Enumerable.Range(0, messageCount)
            .Select(i => new MessageObservation($"message-{i}", i % 2 == 0 ? "user" : "assistant", i,
                $"Contenido {i}"))
            .ToArray());

    private sealed class StoreFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(), "PersonalContextHome.Tests", Guid.NewGuid().ToString("N"));

        public StoreFixture() => Store = new LocalStore(Path.Combine(_directory, "test.db"));

        public LocalStore Store { get; }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }
}
