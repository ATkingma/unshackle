using FluentAssertions.Extensions;
using Shouldly;
using System;

namespace Proofs;

// The inner calls need using System; the And(...) left as Manual still needs FluentAssertions.Extensions (TAAK-242).
public class AndNoUsingSystemCase
{
    public object Test() => TimeSpan.FromSeconds(5).And(TimeSpan.FromSeconds(3));
}
