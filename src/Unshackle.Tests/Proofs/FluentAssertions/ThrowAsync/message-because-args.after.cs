using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class MessageBecauseArgsCase
{
    public async Task Test(Func<Task> act)
    {
        (await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldContain("x", Shouldly.Case.Insensitive, string.Format(System.Globalization.CultureInfo.InvariantCulture, "a {0}", 1));
    }
}
