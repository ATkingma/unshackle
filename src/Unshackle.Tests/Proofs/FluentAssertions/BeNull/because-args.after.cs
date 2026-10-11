using Shouldly;

namespace Proofs;

public class BecauseArgs
{
    public void Test(string? text, int id)
    {
        text.ShouldNotBeNull(string.Format(System.Globalization.CultureInfo.InvariantCulture, "missing {0}", id));
    }
}
