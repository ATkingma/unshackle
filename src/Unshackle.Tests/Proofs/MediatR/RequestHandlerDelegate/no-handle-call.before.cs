using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;

namespace Proofs;

public class OnlyConfigured
{
    private readonly RequestHandlerDelegate<int> _next = Substitute.For<RequestHandlerDelegate<int>>();

    public void Configure()
    {
        _next.Invoke().Returns(1);
    }
}
