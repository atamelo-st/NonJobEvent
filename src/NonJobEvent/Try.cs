namespace Experiment;

public abstract record Try
{
    public static Success<TData> SucceedWith<TData>(TData data) => new(data);

    public static Failure<TFailure> FailWith<TFailure>(TFailure failure) => new(failure);

    public interface Success { }

    public interface Failure { }

    public sealed record Success<TData>(TData Data) : Try.Of<TData>, Success;

    public sealed record Failure<TFailure>(TFailure FailedWith) : Try.Of<TFailure>, Failure;

    public abstract record Of<TExpectedData>
    {
        public static Failure<TFailure> FailWith<TFailure>(TFailure failure) => new(failure);

        public sealed record Failure<TFailure>(TFailure FailedWith) : Try.Of<TExpectedData>, Failure
        {
            public static implicit operator Failure<TFailure>(Try.Failure<TFailure> failure) => new(failure.FailedWith);
        }
    }
}


public class Test
{
    public void Method()
    {
        Try.Success r0 = Try.SucceedWith(1);
        Try.Of<string> r1 = Try.SucceedWith("Hello");
        Try.Of<string> r2 = Try.Of<string>.FailWith(Http.NotFound(4)); // Result<string>.OfFailure(new Http.NotFound(3));
        Try.Of<string>.Failure<Http.Failure.NotFound> r3 = Try.FailWith(Http.NotFound(4));
        Try.Of<string> r4 = r3;
        Try.Of<string> r5 = Try.Of<string>.FailWith(Http.NotFound(4));

        string s = r2 switch
        {
            Try.Success<string> success => "",
            Try.Success => "",
            Try.Of<string>.Failure<Http.Failure.NotFound> notFound => $"{notFound.FailedWith.Code}",
            // Try.Failure<Http.Failure.NotFound> notFounf => "nf",
            Try.Failure => "failure",
            _ => throw new NotImplementedException(),
        };
    }

    public abstract record Http
    {
        public static Failure.NotFound NotFound(int code) => new(code);

        public abstract record Failure
        {
            public record NotFound(int Code) : Failure;
        }
    }
}