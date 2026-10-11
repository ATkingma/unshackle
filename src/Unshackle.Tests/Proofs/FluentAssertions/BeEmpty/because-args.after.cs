using Shouldly;

namespace Proofs;

public class BecauseArgs
{
    public void Test(string text)
    {
        text.ShouldBeEmpty(string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", 1));
    }
}
