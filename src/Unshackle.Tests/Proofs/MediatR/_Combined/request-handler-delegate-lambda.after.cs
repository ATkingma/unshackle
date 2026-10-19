using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Register(string Name) : IRequest<int>;

public class ValidationBehavior : IPipelineBehavior<Register, int>
{
    public async ValueTask<int> Handle(Register command, MessageHandlerDelegate<Register, int> next, CancellationToken token)
    {
        if (command.Name.Length == 0)
        {
            throw new ArgumentException("Name is required.");
        }

        return await next(command, token);
    }
}

public class ValidationBehaviorTests
{
    private readonly ValidationBehavior _behavior = new();

    public async Task Handle_BlockLambda_CallsNext()
    {
        var called = false;
        MessageHandlerDelegate<Register, int> next = (_, _) => { called = true; return ValueTask.FromResult(1); };

        int result = await _behavior.Handle(new Register("a"), next, default);
    }

    public async Task Handle_Invalid_Throws()
    {
        MessageHandlerDelegate<Register, int> next = (_, _) => ValueTask.FromResult(1);

        Func<Task> act = async () => await _behavior.Handle(new Register(""), next, default);

        await act();
    }

    public async Task Handle_AsyncLambda_UsesToken()
    {
        MessageHandlerDelegate<Register, int> next = async (_, token) => { await Task.Delay(1, token); return 2; };

        int result = await _behavior.Handle(new Register("b"), next, CancellationToken.None);
    }

    public async Task Handle_OtherTask_IsWrapped()
    {
        MessageHandlerDelegate<Register, int> next = (_, _) => new ValueTask<int>(Task.Run(() => 3));

        int result = await _behavior.Handle(new Register("c"), next, default);
    }
}
