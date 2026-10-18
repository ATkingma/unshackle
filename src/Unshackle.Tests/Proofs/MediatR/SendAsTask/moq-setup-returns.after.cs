using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Moq
{
    // Minimal stand-in for Moq's Setup/Returns shape (the real package is not referenced by the proofs).
    public class Mock<T>
    {
        public ISetup<T, TResult> Setup<TResult>(Expression<Func<T, TResult>> expression) => throw new NotImplementedException();
    }

    public interface ISetup<T, TResult>
    {
        void Returns(TResult value);
    }
}

namespace Proofs
{
    public record Ping : IRequest<int>;

    public class Setups
    {
        public void Configure(Moq.Mock<IMediator> mock)
        {
            mock.Setup(m => m.Send(new Ping(), CancellationToken.None)).Returns(ValueTask.FromResult(1));
        }
    }
}
