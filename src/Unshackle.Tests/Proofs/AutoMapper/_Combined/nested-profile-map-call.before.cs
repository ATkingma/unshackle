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
            CreateMap<CustomerDto,AddEditCustomerCommand>(MemberList.None);
            CreateMap<AddEditCustomerCommand,Customer>(MemberList.None);
        }
    }
}

public class AddEditCustomerCommandHandler
{
    private readonly IMapper _mapper;

    public AddEditCustomerCommandHandler(IMapper mapper)
    {
        _mapper = mapper;
    }

    public AddEditCustomerCommand Edit(CustomerDto dto) => _mapper.Map<AddEditCustomerCommand>(dto);

    public Customer Handle(AddEditCustomerCommand request, Customer item)
    {
        if (request.Id > 0)
        {
            item = _mapper.Map(request, item);
            return item;
        }

        return _mapper.Map<Customer>(request);
    }
}
