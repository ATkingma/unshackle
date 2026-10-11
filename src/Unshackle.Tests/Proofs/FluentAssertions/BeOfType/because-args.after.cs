using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(object obj)
    {
        obj.ShouldBeOfType<Dto>(string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 1));
    }
}

public class Dto
{
    public string Name { get; set; } = "";
}
