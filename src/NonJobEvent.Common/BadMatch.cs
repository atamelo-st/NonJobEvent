namespace NonJobEvent.Common;

public static class BadMatch
{
    public static InvalidOperationException ShouldNotHappen() => new("Case shouldn't happen.");
}
