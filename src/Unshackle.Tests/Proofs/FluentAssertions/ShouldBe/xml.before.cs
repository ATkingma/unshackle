using System.Xml.Linq;
using FluentAssertions;

namespace Proofs;

public class Xml
{
    public void Test(XElement actual, XElement expected)
    {
        actual.Should().Be(expected);
    }
}
