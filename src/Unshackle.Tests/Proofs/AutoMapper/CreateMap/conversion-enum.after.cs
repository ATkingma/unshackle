using AutoMapper;

namespace Shop;

public enum Status { Open, Closed }

public enum StatusDto { Open, Closed }

public enum Priority { Low, Normal, High }

public enum PriorityDto { HIGH, NORMAL, LOW, Urgent }

public class Order
{
    public Status Status { get; set; }
    public Priority Priority { get; set; }
    public Status Code { get; set; }
    public int Kind { get; set; }
    public Status? Previous { get; set; }
    public Status Next { get; set; }
}

public class OrderDto
{
    public StatusDto Status { get; set; }
    public PriorityDto Priority { get; set; }
    public int Code { get; set; }
    public Status Kind { get; set; }
    public int? Previous { get; set; }
    public StatusDto? Next { get; set; }
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
        Status = (StatusDto)source.Status,
        Priority = source.Priority switch { Priority.Low => PriorityDto.LOW, Priority.High => PriorityDto.HIGH, _ => (PriorityDto)source.Priority },
        Code = (int)source.Code,
        Kind = (Status)source.Kind,
        Previous = (int?)source.Previous,
        Next = (StatusDto?)source.Next,
    };
}
