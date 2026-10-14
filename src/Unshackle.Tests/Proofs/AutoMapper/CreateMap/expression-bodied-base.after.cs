using AutoMapper;

namespace Shop
{
    public class Customer
    {
        public int Id { get; set; }
    }

    public class CustomerDto
    {
        public int Id { get; set; }
    }

    public class CustomerProfile : Profile
    {
        // Named profile.
        public CustomerProfile() : base("Customers")
        {
        } // only map
    }

    public static class CustomerProfileExtensions
    {
        public static CustomerDto ToCustomerDto(this Customer source) => new()
        {
            Id = source.Id,
        };
    }
}
