using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public class OrderService
{
    public object Update(IMapper mapper, object order, object dto) => mapper.Map(order, dto);
}
