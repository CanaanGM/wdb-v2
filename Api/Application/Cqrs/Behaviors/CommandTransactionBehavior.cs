using Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Application.Cqrs.Behaviors;

public sealed class CommandTransactionBehavior<TRequest, TResponse>(
    WorkoutLogDbContext dbContext,
    ILogger<CommandTransactionBehavior<TRequest, TResponse>> logger)
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

        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await next();
        }

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rolling back transaction for command {CommandName}", typeof(TRequest).Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
