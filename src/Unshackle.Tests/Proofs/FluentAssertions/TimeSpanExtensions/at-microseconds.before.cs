using System;
using FluentAssertions.Extensions;

namespace Proofs;

public class AtMicrosecondsCase
{
    public DateTime Test(DateTime date) => date.At(1, 2, 3, 4, 5);
}
