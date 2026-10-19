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
        }
    }

    public static class OrderProfileExtensions
    {
        public static OrderDto ToOrderDto(this Order source) => new()
        {
            Id = source.Id,
        };
    }

    // TAAK-289: once its only Map call is migrated, the IMapper goes, with the argument in the test and the registration.
    public class OrderService
    {
        public OrderService()
        {
        }

        public OrderDto Get(Order order) => order.ToOrderDto();
    }

    public static class Registration
    {
        public static IServiceCollection AddShop(this IServiceCollection services)
        {
            services.AddTransient<OrderService>();
            return services;
        }
    }

    public class OrderServiceTests
    {
        public OrderDto Get()
        {
            var service = new OrderService();
            return service.Get(new Order { Id = 1 });
        }
    }
}
