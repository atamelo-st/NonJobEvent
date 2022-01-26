using System.Diagnostics.CodeAnalysis;

namespace NonJobEvent.Domain;

public readonly record struct RecurrencePattern
{
    public string Value { get; }

    public static RecurrencePattern From(string pattern)
    {
        if (!TryFrom(pattern, out RecurrencePattern? recurrencePattern, out string? errorMessage))
        {
           throw new ArgumentException(errorMessage);
        }

        return recurrencePattern.Value;
    }

    public static bool TryFrom(
        string pattern,
        [NotNullWhen(true)] out RecurrencePattern? recurrencePattern) => TryFrom(pattern, out recurrencePattern, out _);

    public static bool TryFrom(
        string pattern,
        [NotNullWhen(true)] out RecurrencePattern? recurrencePattern,
        [NotNullWhen(false)] out string? errorMessage)
    {
        // TODO: replace this check with a RRULE parser

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

    private RecurrencePattern(string pattern) => this.Value = pattern;

    public RecurrencePattern() => throw new InvalidOperationException("Use constructor wth parameters.");
}
