namespace Proofs
{
    using System;
    using Shouldly;

    public class UsingInNamespace
    {
        public void Test(int result)
        {
            result.ShouldBe(42);
        }
    }
}
