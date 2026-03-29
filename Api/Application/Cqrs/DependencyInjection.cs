using Api.Application.Cqrs.Behaviors;
using MediatR;

namespace Api.Application.Cqrs;

public static class DependencyInjection
{
    public static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(CommandLoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CommandTransactionBehavior<,>));
            cfg.AddOpenBehavior(typeof(QueryLoggingBehavior<,>));
        });

        return services;
    }
}
