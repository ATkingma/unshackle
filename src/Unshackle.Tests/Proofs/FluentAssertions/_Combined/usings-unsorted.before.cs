using Helpers;
using FluentAssertions;
using System.Collections.Generic;

namespace Proofs
{
    public class Case
    {
        public void Test(List<int> values)
        {
            Calc.Sum(values).Should().Be(3);
        }
    }
}

namespace Helpers
{
    public static class Calc
    {
        public static int Sum(List<int> values)
        {
            int total = 0;
            foreach (int value in values)
            {
                total += value;
            }

            return total;
        }
    }
}
