namespace PersonalContext.Core;

public static class ObservationValidator
{
    public const int MaxMessagesPerBatch = 1000;
    public const int MaxMessageLength = 2_000_000;

    public static void Validate(ProviderObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        Required(observation.ProviderKey, 64, nameof(observation.ProviderKey));
        if (observation.ProviderKey.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-')))
            throw new ArgumentException("ProviderKey must contain lowercase ASCII letters, digits, _ or -.");

        Required(observation.ProviderName, 128, nameof(observation.ProviderName));
        Required(observation.ConversationExternalId, 512, nameof(observation.ConversationExternalId));
        Required(observation.Title, 512, nameof(observation.Title));

        if ((observation.ProjectExternalId is null) != (observation.ProjectName is null))
            throw new ArgumentException("ProjectExternalId and ProjectName must be supplied together.");
        if (observation.ProjectExternalId is not null)
        {
            Required(observation.ProjectExternalId, 512, nameof(observation.ProjectExternalId));
            Required(observation.ProjectName!, 512, nameof(observation.ProjectName));
        }

        if (observation.ObservedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("ObservedAtUtc must be in UTC.");
        ArgumentNullException.ThrowIfNull(observation.Messages);
        if (observation.Messages.Count > MaxMessagesPerBatch)
            throw new ArgumentException($"A batch may contain at most {MaxMessagesPerBatch} messages.");

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var message in observation.Messages)
        {
            ArgumentNullException.ThrowIfNull(message);
            Required(message.SourceKey, 512, nameof(message.SourceKey));
            if (!keys.Add(message.SourceKey))
                throw new ArgumentException($"Duplicate SourceKey in batch: {message.SourceKey}");
            if (message.Role is not ("user" or "assistant" or "system" or "tool"))
                throw new ArgumentException($"Unsupported role: {message.Role}");
            if (message.Ordinal < 0)
                throw new ArgumentException("Ordinal must be nonnegative.");
            Required(message.Body, MaxMessageLength, nameof(message.Body));
        }
    }

    private static void Required(string value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw new ArgumentException($"{name} must contain 1 to {maxLength} characters.", name);
    }
}
