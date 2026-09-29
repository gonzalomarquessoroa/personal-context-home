using System.Text;
using System.Text.Json;
using PersonalContext.NativeHost;

namespace PersonalContext.Tests;

public sealed class NativeHostTests
{
    private const string Conversation = "1845e62f-0ca5-4f39-b114-ec862d795029";
    private const string User1 = "23349274-516a-447e-9d60-4396966d160f";
    private const string Assistant1 = "bd129339-a388-4c7b-9e66-6ca0548366cb";
    private const string UserEdited = "167264a3-c53b-456b-8863-bbfbead249e1";
    private const string AssistantRegenerated = "634ec162-a137-4522-974d-f1b3a46d5b4c";

    [Fact]
    public void FramingHandlesUtf8AndRejectsOversizedInput()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"body\":\"árbol\"}");
        using var stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes(bytes.Length));
        stream.Write(bytes);
        stream.Position = 0;
        Assert.Equal(bytes, NativeFrames.Read(stream));
        Assert.Null(NativeFrames.Read(stream));

        using var oversized = new MemoryStream(BitConverter.GetBytes(NativeFrames.MaxRequestBytes + 1));
        Assert.Throws<InvalidDataException>(() => NativeFrames.Read(oversized));
    }

    [Fact]
    public void VisibleBranchChangesAreCountedWithoutReturningContent()
    {
        var host = new ProbeHost();
        var first = Reply(host, [User1, Assistant1]);
        Assert.Equal(2, first.GetProperty("added").GetInt32());
        Assert.Equal(0, Reply(host, [User1, Assistant1]).GetProperty("added").GetInt32());
        var edited = Reply(host, [UserEdited, Assistant1]);
        Assert.Equal(1, edited.GetProperty("added").GetInt32());
        Assert.Equal(1, edited.GetProperty("noLongerVisible").GetInt32());
        var regenerated = Reply(host, [UserEdited, AssistantRegenerated]);
        Assert.Equal(1, regenerated.GetProperty("added").GetInt32());
        Assert.Equal(1, regenerated.GetProperty("noLongerVisible").GetInt32());
        Assert.DoesNotContain("ficticio", regenerated.GetRawText());
    }

    [Fact]
    public void InvalidObservationDoesNotChangePriorVisibleSet()
    {
        var host = new ProbeHost();
        Reply(host, [User1]);
        var invalid = JsonSerializer.SerializeToUtf8Bytes(new
        {
            kind = "observation", sequence = 2, conversationId = Conversation,
            messages = new[] { new { id = "bad", role = "user", ordinal = 0, body = "ficticio" } }
        });
        Assert.False(JsonSerializer.SerializeToElement(host.Handle(invalid)).GetProperty("ok").GetBoolean());
        var next = Reply(host, [User1, Assistant1]);
        Assert.Equal(1, next.GetProperty("added").GetInt32());
    }

    private static JsonElement Reply(ProbeHost host, string[] ids)
    {
        var request = JsonSerializer.SerializeToUtf8Bytes(new
        {
            kind = "observation", sequence = 1, conversationId = Conversation,
            messages = ids.Select((id, ordinal) => new
            {
                id, role = ordinal % 2 == 0 ? "user" : "assistant", ordinal,
                body = "Texto ficticio"
            }).ToArray()
        });
        return JsonSerializer.SerializeToElement(host.Handle(request));
    }
}
