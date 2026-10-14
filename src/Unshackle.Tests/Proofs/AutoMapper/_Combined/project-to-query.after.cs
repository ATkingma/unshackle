using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Customer
    {
        public string Name { get; set; } = "";
    }

    public class OrderLine
    {
        public int Quantity { get; set; }
        public string Product { get; set; } = "";
    }

    public class Order
    {
        public int Id { get; set; }
        public string Note { get; set; } = "";
        public Customer? Customer { get; set; }
        public List<OrderLine> Lines { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    public class CustomerDto
    {
        public string Name { get; set; } = "";
    }

    public class OrderLineDto
    {
        public int Quantity { get; set; }
        public string Product { get; set; } = "";
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string Summary { get; set; } = "";
        public string Note { get; set; } = "";
        public CustomerDto? Customer { get; set; }
        public List<OrderLineDto> Lines { get; set; } = [];
        public List<string> Tags { get; set; } = [];
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
            Summary = source.Note + " #" + source.Id,
            Customer = source.Customer?.ToCustomerDto(),
            Lines = source.Lines?.Select(x => x?.ToOrderLineDto()).ToList() ?? [],
            Tags = source.Tags?.ToList() ?? [],
        };

        public static CustomerDto ToCustomerDto(this Customer source) => new()
        {
            Name = source.Name,
        };

        public static OrderLineDto ToOrderLineDto(this OrderLine source) => new()
        {
            Quantity = source.Quantity,
            Product = source.Product,
        };

        public static Expression<Func<Order, OrderDto>> OrderToOrderDtoProjection => source => new OrderDto
        {
            Id = source.Id,
            Summary = source.Note + " #" + source.Id,
            Customer = source.Customer == null ? null : new CustomerDto { Name = source.Customer.Name },
            Lines = source.Lines.Select(x => new OrderLineDto { Quantity = x.Quantity, Product = x.Product }).ToList(),
            Tags = source.Tags,
        };
    }
}

namespace Shop.Orders
{
    public class OrderQueries
    {
        private readonly IQueryable<Order> _orders;
        private readonly IMapper _mapper;

        public OrderQueries(IQueryable<Order> orders, IMapper mapper)
        {
            _orders = orders;
            _mapper = mapper;
        }

        public List<OrderDto> Open(int minimum) =>
            _orders
                .Where(o => o.Id >= minimum)
                .OrderBy(o => o.Id)
                .Select(OrderProfileExtensions.OrderToOrderDtoProjection)
                .ToList();

        public OrderDto? One(int id) => _orders.Where(o => o.Id == id).Select(OrderProfileExtensions.OrderToOrderDtoProjection).FirstOrDefault();

        public IQueryable<OrderDto> Query() => _orders.Select(OrderProfileExtensions.OrderToOrderDtoProjection);
    }
}
