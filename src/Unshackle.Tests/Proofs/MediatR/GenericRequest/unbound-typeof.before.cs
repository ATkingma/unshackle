using System;
using MediatR;

namespace Proofs;

public class Retried<TRequest, TResponse> : IRequest<TResponse>
    where TRequest : IRequest<TResponse>
{
    public Retried(TRequest request)
    {
        Request = request;
    }

    public TRequest Request { get; }
}

public static class RequestTypes
{
    public static bool IsRetried(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Retried<,>);
}