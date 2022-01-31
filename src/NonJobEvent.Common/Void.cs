namespace NonJobEvent.Common;

public readonly record struct Void
{
    public static Void Self() => new();
}
