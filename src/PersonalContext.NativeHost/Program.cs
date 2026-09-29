using System.Buffers.Binary;
using System.Text.Json;

namespace PersonalContext.NativeHost;

public static class Program
{
    public static int Main()
    {
        var host = new ProbeHost();
        using var input = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();
        try
        {
            while (NativeFrames.Read(input) is { } request)
                NativeFrames.Write(output, host.Handle(request));
            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException)
        {
            Console.Error.WriteLine($"Native Messaging protocol error: {ex.GetType().Name}");
            return 1;
        }
    }
}

public static class NativeFrames
{
    public const int MaxRequestBytes = 512 * 1024;
    public const int MaxResponseBytes = 1024 * 1024;

    public static byte[]? Read(Stream input)
    {
        Span<byte> prefix = stackalloc byte[4];
        var first = input.Read(prefix[..1]);
        if (first == 0) return null;
        input.ReadExactly(prefix[1..]);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(prefix);
        if (length == 0 || length > MaxRequestBytes)
            throw new InvalidDataException("Invalid request length.");
        var payload = new byte[length];
        input.ReadExactly(payload);
        return payload;
    }

    public static void Write(Stream output, object response)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(response);
        if (payload.Length > MaxResponseBytes)
            throw new InvalidDataException("Response too large.");
        Span<byte> prefix = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(prefix, (uint)payload.Length);
        output.Write(prefix);
        output.Write(payload);
        output.Flush();
    }
}

public sealed class ProbeHost
{
    private readonly Dictionary<string, HashSet<string>> _visibleByConversation = new(StringComparer.Ordinal);

    public object Handle(byte[] request)
    {
        using var document = JsonDocument.Parse(request);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("kind", out var kind) ||
            kind.ValueKind != JsonValueKind.String)
            return Error("invalid_request");
        if (kind.GetString() == "ping")
            return new { ok = true, kind = "pong" };
        if (kind.GetString() != "observation") return Error("unsupported_kind");

        try
        {
            var sequence = root.GetProperty("sequence").GetInt32();
            var conversationId = root.GetProperty("conversationId").GetString() ?? "";
            if (!Guid.TryParse(conversationId, out _)) return Error("invalid_conversation_id", sequence);
            var messages = root.GetProperty("messages");
            if (messages.ValueKind != JsonValueKind.Array || messages.GetArrayLength() > 500)
                return Error("invalid_messages", sequence);

            var current = new HashSet<string>(StringComparer.Ordinal);
            var ordinal = 0;
            foreach (var message in messages.EnumerateArray())
            {
                var id = message.GetProperty("id").GetString() ?? "";
                var role = message.GetProperty("role").GetString();
                var body = message.GetProperty("body").GetString();
                if (!Guid.TryParse(id, out _) || !current.Add(id) ||
                    role is not ("user" or "assistant") ||
                    message.GetProperty("ordinal").GetInt32() != ordinal++ ||
                    body is null || body.Length > 8192)
                    return Error("invalid_message", sequence);
            }

            var prior = _visibleByConversation.GetValueOrDefault(conversationId) ?? [];
            var added = current.Except(prior).Count();
            var noLongerVisible = prior.Except(current).Count();
            _visibleByConversation[conversationId] = current;
            var projectId = root.TryGetProperty("projectId", out var project) ? project.GetString() : null;
            return new
            {
                ok = true, sequence, visible = current.Count, added, noLongerVisible,
                projectObserved = Guid.TryParse(projectId, out _),
                coverage = "visible_dom_only"
            };
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        {
            return Error("invalid_request");
        }
    }

    private static object Error(string code, int? sequence = null) => new { ok = false, code, sequence };
}
