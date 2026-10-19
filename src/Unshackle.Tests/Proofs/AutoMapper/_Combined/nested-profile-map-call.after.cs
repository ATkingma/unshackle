using AutoMapper;

namespace CleanArchitecture.Blazor.Application.Features.Customers.Commands.AddEdit;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class AddEditCustomerCommand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    private class Mapping : Profile
    {
        public Mapping()
        {
        }
    }
}

public static class AddEditCustomerCommandMappingExtensions
{
    public static AddEditCustomerCommand ToAddEditCustomerCommand(this CustomerDto source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
    };

    public static Customer ToCustomer(this AddEditCustomerCommand source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
    };

    public static Customer MapInto(this AddEditCustomerCommand source, Customer destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.Description = source.Description;
        return destination;
    }
}

public class AddEditCustomerCommandHandler
{
    public AddEditCustomerCommandHandler()
    {
    }

    public AddEditCustomerCommand Edit(CustomerDto dto) => dto.ToAddEditCustomerCommand();

    public Customer Handle(AddEditCustomerCommand request, Customer item)
    {
        if (request.Id > 0)
        {
            item = request.MapInto(item);
            return item;
        }

        return request.ToCustomer();
    }
}
