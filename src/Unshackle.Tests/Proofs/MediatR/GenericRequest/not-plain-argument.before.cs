using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class Counted<T> : IRequest<int>
{
    public Counted(T items)
    {
        Items = items;
    }

    public T Items { get; }
}

public class Client
{
    private readonly ISender _sender;

    public Client(ISender sender)
    {
        _sender = sender;
    }

    public Task<int> CountAsync(List<string> items) => _sender.Send(new Counted<List<string>>(items));
}