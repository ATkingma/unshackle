using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateHelperMethodCase
{
    public void Test(Func<Task<int>> work)
    {
        Action fail = () => Run(work);
        fail.Should().Throw<InvalidOperationException>();
    }

    private static int Run(Func<Task<int>> work)
    {
        Task<int> task = work();
        task.Wait(TimeSpan.FromSeconds(3));
        return task.Result;
    }
}
