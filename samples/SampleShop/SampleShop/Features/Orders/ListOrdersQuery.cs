using MediatR;
using SampleShop.Dtos;

namespace SampleShop.Features.Orders;

public record ListOrdersQuery : IRequest<List<OrderDto>>;
