using MediatR;
using SampleShop.Dtos;

namespace SampleShop.Features.Orders;

public record PlaceOrderCommand(string CustomerName, List<OrderLineDto> Lines) : IRequest<int>;
