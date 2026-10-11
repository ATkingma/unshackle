using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Register(string Name) : IRequest<int>;

public class ValidationBehavior : IPipelineBehavior<Register, int>
{
    public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token)
    {
        if (command.Name.Length == 0)
        {
            return Task.FromResult(-1);
        }

        return next();
    }
}
