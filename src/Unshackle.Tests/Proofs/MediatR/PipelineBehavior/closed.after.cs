using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Register(string Name) : IRequest<int>;

public class ValidationBehavior : IPipelineBehavior<Register, int>
{
    public ValueTask<int> Handle(Register command, MessageHandlerDelegate<Register, int> next, CancellationToken token)
    {
        if (command.Name.Length == 0)
        {
            return ValueTask.FromResult(-1);
        }

        return next(command, token);
    }
}
