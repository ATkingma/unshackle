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
        public CustomerProfile() : base("Customers") =>
            CreateMap<Customer, CustomerDto>(); // only map
    }
}
