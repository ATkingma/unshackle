using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;

namespace Moq
{
    // Minimal stand-in for Moq's Mock/Setup/ReturnsAsync shape (the real package is not referenced by the proofs).
    public class Mock<T>
    {
        public T Object => throw new NotImplementedException();

        public ISetup<T, TResult> Setup<TResult>(Func<T, TResult> expression) => throw new NotImplementedException();

        public void Verify<TResult>(Func<T, TResult> expression) => throw new NotImplementedException();
    }

    public interface ISetup<T, TResult>
    {
    }

    public static class It
    {
        public static TValue IsAny<TValue>() => default!;
    }

    public static class ReturnsExtensions
    {
        public static void ReturnsAsync<T, TResult>(this ISetup<T, Task<TResult>> setup, TResult value) => throw new NotImplementedException();

        public static void ReturnsAsync<T, TResult>(this ISetup<T, ValueTask<TResult>> setup, TResult value) => throw new NotImplementedException();

        public static void ThrowsAsync<T, TResult>(this ISetup<T, Task<TResult>> setup, Exception exception) => throw new NotImplementedException();

        public static void ThrowsAsync<T, TResult>(this ISetup<T, ValueTask<TResult>> setup, Exception exception) => throw new NotImplementedException();
    }
}

namespace Proofs
{
    public record AddProduct(string Name) : IRequest<string>;

    public class ExceptionEnrichingBehavior : IPipelineBehavior<AddProduct, string>
    {
        public async Task<string> Handle(AddProduct request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                ex.Data["ActionName"] = nameof(AddProduct);
                throw;
            }
        }
    }

    public class ExceptionEnrichingTests
    {
        private readonly Mock<RequestHandlerDelegate<string>> _handlerMock = new();
        private readonly ExceptionEnrichingBehavior _sut = new();

        public async Task Handle_Error_FillsActionName()
        {
            _handlerMock
                .Setup(x => x.Invoke())
                .ThrowsAsync(new Exception());

            Func<Task<string>> act = () =>
                _sut.Handle(
                    new AddProduct("a"),
                    _handlerMock.Object,
                    CancellationToken.None);

            await act();
        }

        public async Task Handle_NoError_ReturnsResult()
        {
            _handlerMock
                .Setup(x => x.Invoke(It.IsAny<CancellationToken>()))
                .ReturnsAsync("ok");

            string result = await _sut.Handle(new AddProduct("b"), _handlerMock.Object, CancellationToken.None);

            _handlerMock.Verify(x => x());
        }
    }
}
