using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class Tests
{
    public void Setup()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<Query>(), Arg.Any<CancellationToken>()).Returns(call => Task.FromResult(call.Arg<Query>().Id));
    }
}
