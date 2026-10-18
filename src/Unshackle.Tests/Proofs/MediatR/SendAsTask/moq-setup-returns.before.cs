using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Moq
{
    // Minimal stand-in for Moq's Setup/Returns shape (the real package is not referenced by the proofs).
    public class Mock<T>
    {
        public ISetup<T, TResult> Setup<TResult>(Expression<Func<T, TResult>> expression) => throw new NotImplementedException();
    }

    public interface ISetup<T, TResult> : Language.IReturns<TResult>
    {
    }
}

namespace Moq.Language
{
    public interface IReturns<TResult>
    {
        void Returns(TResult value);
    }
}

namespace Proofs
{
    public record Ping : IRequest<int>;

    public record Stop : IRequest;

    public class Setups
    {
        public void Configure(Moq.Mock<IMediator> mock)
        {
            mock.Setup(m => m.Send(new Ping(), CancellationToken.None)).Returns(Task.FromResult(1));
            mock.Setup(m => m.Send(new Stop(), CancellationToken.None)).Returns(Task.FromResult(Unit.Value));
        }
    }
}
