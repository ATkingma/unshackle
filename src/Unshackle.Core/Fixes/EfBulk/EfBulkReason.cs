namespace Unshackle.Core.Fixes.EfBulk;

// Typed Manual codes of the efbulk fix (council round 10, C1): the report prints "Code: reason".
public enum EfBulkReason
{
    // The project has no EF Core 7+ relational provider, so ExecuteDelete/ExecuteUpdate do not exist.
    NoNativeExecute,

    // An overload with a batch builder (Action<BatchDelete>/Action<BatchUpdate>) or another argument without a native twin.
    BatchOptions,

    // The update factory is not an inline lambda that creates the entity with plain property assignments.
    UpdateFactoryShape,

    // A value or target in the initializer that EF Core may translate differently from the Z library (TAAK-342 council question 4).
    UpdateValueNotProvable,
}
