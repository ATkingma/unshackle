using System;
using MediatR;

namespace Proofs;

public class Wrapped<T> : INotification
{
    public Wrapped(T value)
    {
        Value = value;
    }

    public T Value { get; }
}

public static class Factory
{
    public static INotification Wrap(object value) =>
        (INotification)Activator.CreateInstance(typeof(Wrapped<>).MakeGenericType(value.GetType()), value)!;
}