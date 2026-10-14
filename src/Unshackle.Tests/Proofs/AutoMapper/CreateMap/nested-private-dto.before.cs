using AutoMapper;

namespace Shop.Orders;

public class Order
{
    public int Id { get; set; }
}

public class GetOrders
{
    private class OrderDto
    {
        public int Id { get; set; }

        private class Mapping : Profile
        {
            public Mapping()
            {
                CreateMap<Order, OrderDto>();
            }
        }
    }
}
