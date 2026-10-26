namespace Unshackle.Core.Generics;

/// <summary>How a generic type is written at one place in the solution.</summary>
public enum GenericUseKind
{
    /// <summary>G&lt;X, Y&gt; with type arguments that contain no type parameter.</summary>
    Closed,

    /// <summary>The base class of a subclass: class S : G&lt;X, Y&gt; (its arguments may contain type parameters of S).</summary>
    SubclassBase,

    /// <summary>G&lt;T&gt; with a type parameter of the code around it, not in a <c>new</c>.</summary>
    Open,

    /// <summary>new G&lt;T&gt;(...) with a type parameter of the code around it.</summary>
    OpenCreation,

    /// <summary>typeof(G&lt;&gt;) or another unbound use.</summary>
    Unbound,
}
