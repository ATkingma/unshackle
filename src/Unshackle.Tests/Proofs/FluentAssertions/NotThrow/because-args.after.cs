using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(Action act)
    {
        Should.NotThrow(act, string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", 1));
    }
}
