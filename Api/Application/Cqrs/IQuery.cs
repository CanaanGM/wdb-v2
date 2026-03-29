using MediatR;

namespace Api.Application.Cqrs;

public interface IQuery<out TResponse> : IRequest<TResponse>;
