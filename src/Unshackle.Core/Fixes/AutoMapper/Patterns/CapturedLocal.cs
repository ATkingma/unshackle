using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A local of a profile constructor with a literal value (<c>int userId = 0;</c>) that a MapFrom lambda reads and
/// <c>ProjectTo&lt;B&gt;(config, new { userId })</c> fills per call (AutoMapper's parameterized projections, measured TAAK-265: Map and a
/// ProjectTo without parameters use the literal, a ProjectTo with the parameter replaces it). The generated ToB and projection take it
/// as an optional parameter with that literal as default (<c>ToB(this A source, int userId = 0)</c>), so a plain ToB() keeps what Map did.
/// </summary>
public class CapturedLocal
{
    public CapturedLocal(ILocalSymbol symbol, string type, string defaultValue)
    {
        Symbol = symbol;
        Type = type;
        DefaultValue = defaultValue;
    }

    public ILocalSymbol Symbol { get; }

    public string Name => Symbol.Name;

    public string Type { get; }

    public string DefaultValue { get; }

    /// <summary>The captured local a statement declares, or null when it is anything else (a literal initializer, one declarator, no modifiers).</summary>
    public static CapturedLocal? TryParse(SyntaxNode statement, SemanticModel model) =>
        statement is LocalDeclarationStatementSyntax { Modifiers.Count: 0, Declaration.Variables: [{ Initializer.Value: LiteralExpressionSyntax literal } declarator] } declaration
        && model.GetDeclaredSymbol(declarator) is ILocalSymbol symbol
        && symbol.Type.TypeKind != TypeKind.Error
            ? new CapturedLocal(symbol, symbol.Type.ToMinimalDisplayString(model, declaration.SpanStart), literal.ToString())
            : null;

    /// <summary>The locals among captured whose name a value reads, in declaration order.</summary>
    public static List<CapturedLocal> Used(IReadOnlyList<CapturedLocal> captured, IEnumerable<(string Name, string Value)> members)
    {
        if (captured.Count == 0)
        {
            return [];
        }

        HashSet<string> read = [.. members.SelectMany(m => SyntaxFactory.ParseExpression(m.Value).DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Where(MapAccessibility.IsReference).Select(n => n.Identifier.ValueText))];
        return [.. captured.Where(c => read.Contains(c.Name))];
    }

    /// <summary>", int userId = 0" for each local, to follow the first parameter of a method.</summary>
    public static string Declare(IEnumerable<CapturedLocal> locals) =>
        string.Concat(locals.Select(c => $", {c.Type} {c.Name} = {c.DefaultValue}"));
}
