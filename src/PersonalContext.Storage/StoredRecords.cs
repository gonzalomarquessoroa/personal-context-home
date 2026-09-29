namespace PersonalContext.Storage;

public sealed record ConversationSummary(
    long Id,
    string ProviderKey,
    string ExternalId,
    string Title,
    string? ProjectName,
    int MessageCount);

public sealed record StoredMessage(
    long Id,
    string SourceKey,
    string Role,
    int Ordinal,
    string Body,
    int RevisionCount);

public sealed record CaptureStatus(
    long ConversationId,
    DateTimeOffset LastObservedAtUtc,
    DateTimeOffset LastSuccessAtUtc,
    string? LastError,
    string Coverage);
