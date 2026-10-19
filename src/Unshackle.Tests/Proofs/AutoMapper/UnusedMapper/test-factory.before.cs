using AutoMapper;

namespace Proofs;

public class OrderProfile : Profile
{
}

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }
}

public static class MapperFactory
{
    public static IMapper Create()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>());
        return configuration.CreateMapper();
    }
}

public class OrderServiceTests
{
    private IMapper _mapper = null!;
    private OrderService _service = null!;

    public void SetUp()
    {
        _mapper = MapperFactory.Create();
        _service = new OrderService(_mapper);
    }

    public OrderService Service => _service;
}
