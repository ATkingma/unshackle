using MediatR;
using SampleShop.Dtos;

namespace SampleShop.Features.Orders;

public record GetOrderQuery(int Id) : IRequest<OrderDto?>;
