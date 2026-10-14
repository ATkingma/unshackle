using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record CancelOrderCommand(int OrderNumber) : IRequest<bool>;

public interface IOrderRepository
{
    Task<object?> GetAsync(int orderNumber);

    Task<bool> SaveAsync(CancellationToken cancellationToken);
}

// Shape from dotnet/eShop (MIT): CancelOrderCommandHandler with an early return and XML docs.
public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Handler which processes the command when the customer cancels an order.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The token.</param>
    /// <returns>Whether the order was cancelled.</returns>
    public async ValueTask<bool> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var orderToUpdate = await _orderRepository.GetAsync(command.OrderNumber);
        if (orderToUpdate == null)
        {
            return false;
        }

        return await _orderRepository.SaveAsync(cancellationToken);
    }
}
