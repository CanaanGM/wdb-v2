using System.Diagnostics;
using MediatR;

namespace Api.Application.Cqrs.Behaviors;

public sealed class CommandLoggingBehavior<TRequest, TResponse>(ILogger<CommandLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommand<TResponse>)
        {
            return await next();
        }

        var commandName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling command {CommandName}", commandName);

        try
        {
            var response = await next();
            logger.LogInformation(
                "Command {CommandName} handled in {ElapsedMilliseconds}ms",
                commandName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Command {CommandName} failed after {ElapsedMilliseconds}ms", commandName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
