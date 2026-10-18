using System;
#if CUSTOM_ASSERTIONS
using System.Text;
#else
using FluentAssertions;
#endif

namespace Proofs;

public class Case
{
    public string Name() => nameof(Case);
}
