using System.Runtime.CompilerServices;
using AutoMapper;
using MediatR;
using SampleShop.Data;
using SampleShop.Dtos;

namespace SampleShop.Features.Customers;

public record StreamCustomersQuery(string? City) : IStreamRequest<CustomerDto>;

public class StreamCustomersHandler : IStreamRequestHandler<StreamCustomersQuery, CustomerDto>
{
    private readonly CustomerRepository _repository;
    private readonly IMapper _mapper;

    public StreamCustomersHandler(CustomerRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async IAsyncEnumerable<CustomerDto> Handle(StreamCustomersQuery request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var customer in _repository.Customers.Where(c => request.City is null || c.Address.City == request.City))
        {
            await Task.Yield();
            yield return _mapper.Map<CustomerDto>(customer);
        }
    }
}
