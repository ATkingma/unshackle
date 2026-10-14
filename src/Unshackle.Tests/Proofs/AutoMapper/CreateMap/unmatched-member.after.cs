using System;
using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? Completed { get; set; }
}

public class Order
{
    public int Id { get; set; }
}

public class CustomerEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? Completed { get; set; }
    public bool IsCompleted => Completed.HasValue;
    public string Created { get; set; } = "now";
    public int Version { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
    }
}

public static class CustomerProfileExtensions
{
    public static CustomerEntity ToCustomerEntity(this Customer source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Completed = source.Completed,
    };
}
