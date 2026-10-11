using System;
using FluentAssertions;

namespace Proofs;

public class Score : IComparable<Score>, IComparable<int>
{
    public int CompareTo(Score? other) => 0;

    public int CompareTo(int other) => 0;
}

public class ValueTypeDiffersCase
{
    public void Test(Score score)
    {
        score.Should<int>().BeGreaterThan(5);
    }
}
