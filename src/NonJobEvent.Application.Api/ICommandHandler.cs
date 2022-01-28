namespace NonJobEvent.Application.Api;

public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}

public delegate Task<TResult> CommandHandler<TCommand, TResult>(TCommand command);
