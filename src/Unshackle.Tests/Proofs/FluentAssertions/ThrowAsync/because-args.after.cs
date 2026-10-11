using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class BecauseArgsCase
{
    public async Task Test(Func<Task> act)
    {
        await Should.ThrowAsync<InvalidOperationException>(act, string.Format(System.Globalization.CultureInfo.InvariantCulture, "a {0}", 1));
    }
}
