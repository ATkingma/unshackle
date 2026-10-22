namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// The typed reason codes (council round 10, C1) of a mocked IMapper whose stubs or checks stay (TAAK-311, council round 11, A6–A8).
/// </summary>
internal enum MockSetUpCode
{
    /// <summary>A6: the test verifies a Map call of the mock (Verify, Received, DidNotReceive); no check remains once the class under test calls the generated method.</summary>
    VerifyOnMapper,

    /// <summary>A1/A7: the value a stub returns is not provably what the generated method returns for the object the test maps.</summary>
    MockedReturnNotProvable,

    /// <summary>A8: a mock without stubs whose class under test reads, checks or hands on the value Map returns (null from the mock, a real object after).</summary>
    MockWithoutStubsNotProvable,
}
