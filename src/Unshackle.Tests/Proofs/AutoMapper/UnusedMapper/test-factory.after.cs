using AutoMapper;

namespace Proofs;

public class OrderProfile : Profile
{
}

public class OrderService
{
    public OrderService()
    {
    }
}

public static class MapperFactory
{
}

public class OrderServiceTests
{
    private OrderService _service = null!;

    public void SetUp()
    {
        _service = new OrderService();
    }

    public OrderService Service => _service;
}
