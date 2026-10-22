using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// One stub of a mocked IMapper: mock.Setup(m =&gt; m.Map&lt;B&gt;(source)).Returns(value); (Moq) or mock.Map&lt;B&gt;(source).Returns(value); (NSubstitute).
/// </summary>
internal class MapStub
{
    /// <summary>The whole statement, removed when the stub goes.</summary>
    public required ExpressionStatementSyntax Statement { get; init; }

    /// <summary>The Map call inside it.</summary>
    public required InvocationExpressionSyntax Call { get; init; }

    /// <summary>The argument of the Map call.</summary>
    public required ExpressionSyntax Source { get; init; }

    /// <summary>The argument of Returns.</summary>
    public required ExpressionSyntax Value { get; init; }

    /// <summary>The source type of the map: the type argument A of Map&lt;A, B&gt;, else the type of the argument.</summary>
    public required ITypeSymbol? From { get; init; }

    /// <summary>The destination type B.</summary>
    public required ITypeSymbol To { get; init; }

    /// <summary>File:line of the statement.</summary>
    public required string Where { get; init; }
}
