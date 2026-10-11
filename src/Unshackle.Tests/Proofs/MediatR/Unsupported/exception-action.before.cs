using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Pipeline;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class LogAction : IRequestExceptionAction<Query, InvalidOperationException>
{
    public Task Execute(Query request, InvalidOperationException exception, CancellationToken cancellationToken) => Task.CompletedTask;
}
