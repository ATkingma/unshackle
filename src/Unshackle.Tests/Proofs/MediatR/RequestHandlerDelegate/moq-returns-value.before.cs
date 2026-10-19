using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;

namespace Moq
{
    // Minimal stand-in for Moq's Mock/Setup/Returns shape (the real package is not referenced by the proofs).
    public class Mock<T>
    {
        public T Object => throw new NotImplementedException();

        public ISetup<TResult> Setup<TResult>(Func<T, TResult> expression) => throw new NotImplementedException();
    }

    public interface ISetup<TResult>
    {
        void Returns(Func<TResult> valueFunction);
    }
}

namespace Proofs
{
    public record Register(string Name) : IRequest<int>;

    public class ValidationBehavior : IPipelineBehavior<Register, int>
    {
        public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) => next();
    }

    public class ValidationBehaviorTests
    {
        public async Task Handle_ReturnsCallback()
        {
            var next = new Mock<RequestHandlerDelegate<int>>();
            next.Setup(x => x.Invoke()).Returns(() => Task.FromResult(1));

            int result = await new ValidationBehavior().Handle(new Register("a"), next.Object, default);
        }
    }
}
