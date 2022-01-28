namespace NonJobEvent.Application.Api;

public abstract record Result
{
    // TODO: add metadata
    public interface Success { }

    // TODO: should there be commom Success and Failure that are shared between Command and Query?
    public interface Failure
    {
        string Message { get; }
    }

    public abstract record OfQuery : Result
    {
        public static Success<TData> OfSuccess<TData>(TData data) => new(data);

        new public interface Failure : Result.Failure
        {
            public interface NotFound : Failure { }
        }

        public sealed record Success<TData> : Result.OfQuery<TData>, Result.Success
        {
            public TData Data { get; }

            internal Success(TData data) => this.Data = data;
        }
    }

    public abstract record OfQuery<TExpectedData> : OfQuery
    {
        public static class OfFailure
        {
            public static Failure.NotFound NotFound(string message = "Not found.") => new(message);
        }

        new public abstract record Failure : OfQuery<TExpectedData>, OfQuery.Failure
        {
            public string Message { get; }

            private Failure(string message) => this.Message = message;

            public sealed record NotFound : Failure, OfQuery.Failure.NotFound
            {
                internal NotFound(string message) : base(message) { }
            }
        }
    }

    public abstract record OfCommand : Result
    {
        public static Success<TData> OfSuccess<TData>(TData data) => new(data);

        public static Success OfSuccess() => Success.Self;

        new public sealed record Success : Result.OfCommand, Result.Success
        {
            internal static readonly Success Self = new Success();

            private Success() { }
        }

        public sealed record Success<TData> : Result.OfCommand<TData>, Result.Success
        {
            public TData Data { get; }

            internal Success(TData data) => this.Data = data;
        }

        public static class OfFailure
        {
            public static Failure.NotFound NotFound(string message = "Not found.") => new(message);

            public static Failure.AlreadyExists AlreadyExists(string message = "Already exists.") => new(message);

            public static Failure.ConcurrencyConflict ConcurrencyConflict(string message = "Concurrency conflict.")
                => new(message);
        }

        new public abstract record Failure : OfCommand, Result.Failure
        {
            public string Message { get; }

            private Failure(string message) => this.Message = message;

            public sealed record NotFound : Failure
            {
                internal NotFound(string message) : base(message) { }
            }

            public sealed record AlreadyExists : Failure
            {
                internal AlreadyExists(string message) : base(message) { }
            }
            public sealed record ConcurrencyConflict : Failure
            {
                internal ConcurrencyConflict(string message) : base(message) { }
            }
        }
    }

    public abstract record OfCommand<TExpectedData> : OfCommand
    {
        new public static class OfFailure
        {
        }
    }
}
