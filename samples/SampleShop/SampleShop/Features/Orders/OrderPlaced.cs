using MediatR;

namespace SampleShop.Features.Orders;

public record OrderPlaced(int OrderId) : INotification;
