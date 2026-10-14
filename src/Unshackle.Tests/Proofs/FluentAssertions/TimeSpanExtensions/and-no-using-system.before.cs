using FluentAssertions.Extensions;

namespace Proofs;

// The inner calls need using System; the And(...) left as Manual still needs FluentAssertions.Extensions (TAAK-242).
public class AndNoUsingSystemCase
{
    public object Test() => 5.Seconds().And(3.Seconds());
}
