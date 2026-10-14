using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class AtVariableCase
{
    public DateTime Test(DateTime date) => date.At(9, 15, 30);

    public DateTime WithMilliseconds(DateTime date) => date.At(9, 15, 30, 250);
}
