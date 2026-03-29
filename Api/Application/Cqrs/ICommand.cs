using MediatR;

namespace Api.Application.Cqrs;

public interface ICommand<out TResponse> : IRequest<TResponse>;
