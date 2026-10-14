using FluentAssertions;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): ReplicatorMessageSerializerSpec.CheckSerialization<T> (TAAK-200).
public class ObjectSubjectGenericCase
{
    public object FromBinary(object value) => value;

    public void CheckSerialization<T>(T expected)
    {
        object actual = FromBinary(expected!);
        actual.Should().Be(expected);
    }
}
