using System;
#if CUSTOM_ASSERTIONS
using System.Text;
#else
#endif

namespace Proofs;

public class Case
{
    public string Name() => nameof(Case);
}
