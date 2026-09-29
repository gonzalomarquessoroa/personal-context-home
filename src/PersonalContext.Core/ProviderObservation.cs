namespace PersonalContext.Core;

/// <summary>A provider-independent observation of one conversation.</summary>
public sealed record ProviderObservation(
    string ProviderKey,
    string ProviderName,
    string ConversationExternalId,
    string Title,
    string? ProjectExternalId,
    string? ProjectName,
    DateTimeOffset ObservedAtUtc,
    IReadOnlyList<MessageObservation> Messages);

/// <summary>SourceKey must identify the same logical message on every retry.</summary>
public sealed record MessageObservation(
    string SourceKey,
    string Role,
    int Ordinal,
    string Body);

public sealed record IngestResult(
    long ConversationId,
    int InsertedMessages,
    int RevisedMessages,
    int UnchangedMessages);
