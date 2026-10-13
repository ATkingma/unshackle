using System;
using FluentAssertions;

namespace Proofs;

public class ResultWhereExpressionCase
{
    public void Test(Action act, System.Linq.Expressions.Expression<Func<ArgumentException, bool>> condition)
    {
        act.Should().Throw<ArgumentException>().Where(condition);
    }
}
