using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NSubstitute;

namespace Proofs;

public record Register(string Name) : IRequest<int>;

public class ValidationBehavior : IPipelineBehavior<Register, int>
{
    public async ValueTask<int> Handle(Register command, MessageHandlerDelegate<Register, int> next, CancellationToken token)
    {
        return command.Name.Length == 0 ? -1 : await next(command, token);
    }
}

public class ValidationBehaviorTests
{
    private readonly MessageHandlerDelegate<Register, int> _next = Substitute.For<MessageHandlerDelegate<Register, int>>();
    private readonly ValidationBehavior _behavior = new();

    public async Task Handle_ValidCommand_CallsNext()
    {
        _next.Invoke(Arg.Any<Register>(), Arg.Any<CancellationToken>()).Returns(5);

        int result = await _behavior.Handle(new Register("a"), _next, default);

        await _next.Received(1).Invoke(Arg.Any<Register>(), Arg.Any<CancellationToken>());
    }

    public async Task Handle_Local_UsesTaskFromResult()
    {
        var next = Substitute.For<MessageHandlerDelegate<Register, int>>();
        next.Invoke(Arg.Any<Register>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(7));

        int result = await _behavior.Handle(new Register("b"), next, CancellationToken.None);
    }
}
