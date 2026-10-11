namespace Proofs
{
    using System;
    using FluentAssertions;

    public class UsingInNamespace
    {
        public void Test(int result)
        {
            result.Should().Be(42);
        }
    }
}
