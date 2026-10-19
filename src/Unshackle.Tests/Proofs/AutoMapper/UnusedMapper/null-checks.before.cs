using System;
using AutoMapper;

namespace Proofs;

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(string name, IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (mapper == null)
        {
            throw new ArgumentNullException(nameof(mapper));
        }

        Name = name;
        this._mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public string Name { get; }
}
