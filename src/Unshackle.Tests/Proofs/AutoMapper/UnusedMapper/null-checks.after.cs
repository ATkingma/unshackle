using System;

namespace Proofs;

public class OrderService
{
    public OrderService(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
