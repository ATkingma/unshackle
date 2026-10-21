using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

internal class MapperRegistrationScope
{
    public MapperRegistrationScope(string? call, string? failure, KeptReason code)
    {
        Call = call;
        Failure = failure;
        Code = code;
    }

    public string? Call { get; }

    public string? Failure { get; }

    public KeptReason Code { get; }
}
