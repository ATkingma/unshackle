using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// Writes the hand-written map methods of <see cref="CreateMapPattern"/>: the extension class, its method names, and the profile constructor or lambda without its maps.
/// </summary>
internal static class MapMethodWriter
{
    // <Profile>Extensions; a nested profile gets the names of its containing types in front (TodoItemDtoMappingExtensions), because the
    // jason style names every nested profile Mapping.
    internal static string ExtensionsName(ClassDeclarationSyntax profile) =>
        string.Concat(profile.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.ValueText)) + "Extensions";

    // The constructor without its maps: an empty block, also for an expression body (which then gets its own lines).
    internal static ConstructorDeclarationSyntax Emptied(ConstructorDeclarationSyntax constructor)
    {
        if (constructor.Body is { } body)
        {
            return constructor.WithBody(body.WithStatements([]));
        }

        string indent = constructor.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        string eol = constructor.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } found
            ? found
            : "\n";
        BlockSyntax block = SyntaxFactory.Block()
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.EndOfLine(eol), SyntaxFactory.Whitespace(indent)).WithTrailingTrivia(SyntaxFactory.EndOfLine(eol)))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(SyntaxFactory.Whitespace(indent)).WithTrailingTrivia(constructor.SemicolonToken.TrailingTrivia));
        ConstructorDeclarationSyntax headed = constructor.Initializer is { } initializer
            ? constructor.WithInitializer(initializer.WithoutTrailingTrivia())
            : constructor.WithParameterList(constructor.ParameterList.WithoutTrailingTrivia());
        return headed.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block);
    }

    // The lambda of a MapperConfiguration without its maps (TAAK-286): an empty block.
    internal static LambdaExpressionSyntax Emptied(LambdaExpressionSyntax lambda) =>
        lambda.Body is BlockSyntax block
            ? lambda.WithBody(block.WithStatements([]))
            : lambda.WithBody(SyntaxFactory.Block().WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.Space))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithTrailingTrivia(lambda.Body.GetTrailingTrivia())));

    // The lambda with only the maps that stay on AutoMapper (an expression body that stays is left as it is).
    internal static LambdaExpressionSyntax Trimmed(LambdaExpressionSyntax lambda, List<SyntaxNode> kept) =>
        lambda.Body is BlockSyntax block ? lambda.WithBody(block.WithStatements([.. block.Statements.Where(kept.Contains)])) : lambda;

    // The constructor with only the maps that stay on AutoMapper (TAAK-272), and the captured variables they read.
    internal static ConstructorDeclarationSyntax Trimmed(ConstructorDeclarationSyntax constructor, List<SyntaxNode> kept, SemanticModel model)
    {
        HashSet<string> read = [.. kept.SelectMany(k => k.DescendantNodes().OfType<IdentifierNameSyntax>()).Select(n => n.Identifier.ValueText)];
        return constructor.WithBody(constructor.Body!.WithStatements([.. constructor.Body.Statements
            .Where(s => kept.Contains(s) || (CapturedLocal.TryParse(s, model) is { } local && read.Contains(local.Name)))]));
    }

    // A type written in a nested profile, as the code next to the outermost type has to write it.
    internal static TypeSyntax AtAnchor(TypeSyntax written, ITypeSymbol type, SemanticModel model, int position) =>
        SymbolEqualityComparer.Default.Equals(model.GetSpeculativeTypeInfo(position, written, SpeculativeBindingOption.BindAsTypeOrNamespace).Type, type)
            ? written
            : SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, position));

    // ToB for CreateMap<A, B>: the last name of the destination type as written.
    // A keyword (a ConvertUsing to int, string, ...) gets the .NET name: ToInt32; string gets ToMappedString (see MethodName(string)).
    internal static string MethodName(TypeSyntax destination) =>
        MethodName(destination switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            PredefinedTypeSyntax predefined => predefined.Keyword.ValueText switch
            {
                "bool" => "Boolean", "byte" => "Byte", "sbyte" => "SByte", "short" => "Int16", "ushort" => "UInt16", "int" => "Int32", "uint" => "UInt32",
                "long" => "Int64", "ulong" => "UInt64", "float" => "Single", "double" => "Double", "decimal" => "Decimal", "char" => "Char",
                "string" => "String", _ => "Object",
            },
            _ => destination.ToString().Split('.')[^1],
        });

    // Every type has ToString(), which a call would bind before an extension method ToString (TAAK-273, SampleShop Money → string).
    internal static string MethodName(string destination) => destination == "String" ? "ToMappedString" : "To" + destination;

    // The static class goes right after the profile (the outermost type around a nested one), with its indentation and line endings.
    internal static MemberDeclarationSyntax Extensions(TypeDeclarationSyntax anchor, string className, string access,
        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members, bool MapInto, string Creation)> maps,
        List<(TypeSyntax Source, TypeSyntax Destination, string Name, List<(string Name, string Value)> Members, List<CapturedLocal> Parameters)> projections,
        List<CapturedLocal> captured)
    {
        string indent = anchor.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        string eol = anchor.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } found
            ? found
            : "\n";
        var text = new StringBuilder();
        text.Append(access).Append(" static class ").Append(className).Append(eol).Append(indent).Append('{').Append(eol);
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members, _, string creation) in maps)
        {
            if (text[^(eol.Length + 1)] != '{')
            {
                text.Append(eol);
            }

            if (creation.StartsWith(CreateMapProblems.BlockMarker))
            {
                // ConstructUsing with members left to assign (TAAK-266).
                text.Append(indent).Append("    ").Append(access).Append(" static ").Append(destination).Append(' ').Append(MethodName(destination))
                    .Append("(this ").Append(source).Append(" source)").Append(eol).Append(indent).Append("    {").Append(eol)
                    .Append(indent).Append("        ").Append(destination).Append(" destination = ").Append(creation[1..].Replace("\n", "\n" + indent + "        ", StringComparison.Ordinal)).Append(';').Append(eol);
                foreach ((string member, string value) in members)
                {
                    text.Append(indent).Append("        destination.").Append(member).Append(" = ").Append(value).Append(';').Append(eol);
                }

                text.Append(indent).Append("        return destination;").Append(eol).Append(indent).Append("    }").Append(eol);
                continue;
            }

            text.Append(indent).Append("    ").Append(access).Append(" static ").Append(destination).Append(' ').Append(MethodName(destination))
                .Append("(this ").Append(source).Append(" source").Append(CapturedLocal.Declare(CapturedLocal.Used(captured, members))).Append(") => ").Append(creation.Replace("\n", "\n" + indent + "    ", StringComparison.Ordinal));
            if (members.Count == 0)
            {
                text.Append(';').Append(eol);
                continue;
            }

            text.Append(eol).Append(indent).Append("    {").Append(eol);
            foreach ((string member, string value) in members)
            {
                text.Append(indent).Append("        ").Append(member).Append(" = ").Append(value).Append(',').Append(eol);
            }

            text.Append(indent).Append("    };").Append(eol);
        }

        // After all ToB methods: MapCallPattern pairs the i-th method with the i-th CreateMap.
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members, _, _) in maps.Where(m => m.MapInto))
        {
            text.Append(eol).Append(indent).Append("    ").Append(access).Append(" static ").Append(destination)
                .Append(" MapInto(this ").Append(source).Append(" source, ").Append(destination).Append(" destination)").Append(eol)
                .Append(indent).Append("    {").Append(eol);
            foreach ((string member, string value) in members)
            {
                text.Append(indent).Append("        destination.").Append(member).Append(" = ").Append(value).Append(';').Append(eol);
            }

            text.Append(indent).Append("        return destination;").Append(eol).Append(indent).Append("    }").Append(eol);
        }

        // Properties, not methods: MapCallPattern counts only the ToB methods.
        // A method when the map reads a captured variable of the profile constructor (ProjectTo's parameters), else a property.
        foreach ((TypeSyntax source, TypeSyntax destination, string name, List<(string Name, string Value)> members, List<CapturedLocal> parameters) in projections)
        {
            text.Append(eol).Append(indent).Append("    ").Append(access).Append(" static Expression<Func<").Append(source).Append(", ").Append(destination)
                .Append(">> ").Append(name).Append(parameters.Count == 0 ? "" : "(" + CapturedLocal.Declare(parameters)[2..] + ")").Append(" => source => new ").Append(destination);
            if (members.Count == 0)
            {
                text.Append("();").Append(eol);
                continue;
            }

            text.Append(eol).Append(indent).Append("    {").Append(eol);
            foreach ((string member, string value) in members)
            {
                text.Append(indent).Append("        ").Append(member).Append(" = ").Append(value).Append(',').Append(eol);
            }

            text.Append(indent).Append("    };").Append(eol);
        }

        text.Append(indent).Append('}').Append(eol);
        // The blank line before it is set as trivia: the parser would drop a leading line ending.
        return SyntaxFactory.ParseMemberDeclaration(text.ToString())!.WithLeadingTrivia(SyntaxFactory.EndOfLine(eol), SyntaxFactory.Whitespace(indent));
    }
}
