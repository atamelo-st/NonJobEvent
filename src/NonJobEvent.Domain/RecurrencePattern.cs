using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

public readonly record struct RecurrencePattern
{
    public string Pattern { get; }

    public static bool TryFrom(
        string pattern,
        [NotNullWhen(true)] out RecurrencePattern? recurrencePattern,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            recurrencePattern = null;
            errorMessage = "Pattern string cannot be null, empty or whitespace.";

            return false;
        }

        recurrencePattern = new RecurrencePattern(pattern);
        errorMessage = null;

        return true;
    }

    private RecurrencePattern(string pattern) => this.Pattern = pattern;

    public RecurrencePattern() => throw new InvalidOperationException("Use constructor wth parameters.");
}
