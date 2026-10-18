using System;
using Shouldly;

namespace Proofs;

public class ValueFuncGenericCase<T>
{
    public void Test()
    {
        Func<T> local = () => throw new ArgumentException();
        Should.Throw<ArgumentException>(() => local());
    }
}
