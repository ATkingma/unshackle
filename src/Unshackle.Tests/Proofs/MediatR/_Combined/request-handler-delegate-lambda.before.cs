using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Register(string Name) : IRequest<int>;

public class ValidationBehavior : IPipelineBehavior<Register, int>
{
    public async Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token)
    {
        if (command.Name.Length == 0)
        {
            throw new ArgumentException("Name is required.");
        }

        return await next();
    }
}

public class ValidationBehaviorTests
{
    private readonly ValidationBehavior _behavior = new();

    public async Task Handle_BlockLambda_CallsNext()
    {
        var called = false;
        RequestHandlerDelegate<int> next = _ => { called = true; return Task.FromResult(1); };

        int result = await _behavior.Handle(new Register("a"), next, default);
    }

    public async Task Handle_Invalid_Throws()
    {
        RequestHandlerDelegate<int> next = _ => Task.FromResult(1);

        Func<Task> act = async () => await _behavior.Handle(new Register(""), next, default);

        await act();
    }

    public async Task Handle_AsyncLambda_UsesToken()
    {
        RequestHandlerDelegate<int> next = async token => { await Task.Delay(1, token); return 2; };

        int result = await _behavior.Handle(new Register("b"), next, CancellationToken.None);
    }

    public async Task Handle_OtherTask_IsWrapped()
    {
        RequestHandlerDelegate<int> next = _ => Task.Run(() => 3);

        int result = await _behavior.Handle(new Register("c"), next, default);
    }
}
