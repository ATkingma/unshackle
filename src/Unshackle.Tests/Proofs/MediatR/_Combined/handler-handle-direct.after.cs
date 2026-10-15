using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record GetUser(int Id) : IRequest<int>;

public class GetUserHandler : IRequestHandler<GetUser, int>
{
    public ValueTask<int> Handle(GetUser request, CancellationToken cancellationToken) => ValueTask.FromResult(request.Id);
}

// Shape from madslundt/NetCoreMediatrSample: a unit test calls the handler directly and passes the call on as a Task.
public class GetUserTests
{
    public async Task Test()
    {
        var handler = new GetUserHandler();
        Func<Task<int>> act = () => handler.Handle(new GetUser(1), CancellationToken.None).AsTask();
        var direct = () => handler.Handle(new GetUser(2), CancellationToken.None).AsTask();
        int awaited = await handler.Handle(new GetUser(3), CancellationToken.None);
        await act();
        await direct();
    }
}
