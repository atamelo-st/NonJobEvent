namespace NonJobAppointment.Common;

public readonly record struct OneOf<TFirst, TSecond> 
    where TFirst : class
    where TSecond : class
{
    public object TheOne { get; }

    public OneOf(TFirst first)
    {
        ArgumentNullException.ThrowIfNull(first, nameof(first));
       
        this.TheOne = first;
    }

    public OneOf(TSecond second)
    {
        ArgumentNullException.ThrowIfNull(second, nameof(second));

        this.TheOne = second;
    }


    public static implicit operator OneOf<TFirst, TSecond>(Those<TFirst> oneOfThose)
    {
        return new OneOf<TFirst, TSecond>(oneOfThose.Item);
    }

    public static implicit operator OneOf<TFirst, TSecond>(Those<TSecond> oneOfThose)
    {
        return new OneOf<TFirst, TSecond>(oneOfThose.Item);
    }

}

public readonly record struct Those<T> where T : class
{
    public T Item { get; }

    public Those(T item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        this.Item = item;
    }
}

public static class OneOf
{
    public static Those<T> Those<T>(T item) where T : class => new(item);
}