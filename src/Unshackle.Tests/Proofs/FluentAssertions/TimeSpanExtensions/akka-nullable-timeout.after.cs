using System;
using Shouldly;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): TestKit methods take a nullable timeout, e.g. ExpectMsg(msg, 3.Seconds()).
public class NullableTimeoutCase
{
    public T ExpectMsg<T>(T message, TimeSpan? timeout = null) => message;

    public void Test()
    {
        ExpectMsg("done", TimeSpan.FromSeconds(3));
    }
}
