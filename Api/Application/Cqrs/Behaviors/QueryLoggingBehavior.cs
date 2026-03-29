using System.Diagnostics;
using MediatR;

namespace Api.Application.Cqrs.Behaviors;

public sealed class QueryLoggingBehavior<TRequest, TResponse>(ILogger<QueryLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IQuery<TResponse>)
        {
            return await next();
        }

        var queryName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling query {QueryName}", queryName);

        try
        {
            var response = await next();
            logger.LogInformation(
                "Query {QueryName} handled in {ElapsedMilliseconds}ms",
                queryName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Query {QueryName} failed after {ElapsedMilliseconds}ms", queryName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
