using System;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Shop
{
    public class Order
    {
        public int Id { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
    }

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>();
        }
    }

    // TAAK-289: once its only Map call is migrated, the IMapper goes, with the argument in the test and the registration.
    public class OrderService
    {
        private readonly IMapper _mapper;

        public OrderService(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public OrderDto Get(Order order) => _mapper.Map<OrderDto>(order);
    }

    public static class Registration
    {
        public static IServiceCollection AddShop(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(OrderProfile).Assembly);
            services.AddTransient<OrderService>();
            return services;
        }
    }

    public class OrderServiceTests
    {
        public OrderDto Get()
        {
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>()).CreateMapper();
            var service = new OrderService(mapper);
            return service.Get(new Order { Id = 1 });
        }
    }
}
