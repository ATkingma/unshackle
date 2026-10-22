using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-309 (queue row 8): a statement that sets one style property through an EPPlus <c>ExcelStyle</c> (<c>x.Style.Font.Bold = true;</c>)
/// becomes the ClosedXML statement on the same receiver (<c>x.Style.Font.Bold = true;</c>, <c>x.Style.Alignment.Horizontal = ...</c>).
/// Only the shapes in <see cref="StylesMapping"/> are Fixed, each proven equal per cell by the harness (council round 8, E1); any other
/// style statement is Manual. The receiver in front of <c>.Style</c> is left to the other rows.
/// </summary>
public class StylesPattern : IPattern
{


    // On every statement this pattern wrote: its style tail is ClosedXML, only the receiver in front of .Style may still be EPPlus.
    public const string MigratedAnnotationKind = "Unshackle.EPPlus.StyleMigrated";

    private const string ClosedXmlNamespace = "ClosedXML.Excel";

    public string Id => "epplus.styles";

    public string Description => "x.Style.Font/Fill/Border/Numberformat/alignment/WrapText = ... → the ClosedXML style statement (harness-proven shapes only)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ExpressionStatementSyntax>().Where(s => StylesMapping.Split(Target(s.Expression), model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var statement = (ExpressionStatementSyntax)node;
        (ExpressionSyntax style, IReadOnlyList<string> path) = StylesMapping.Split(Target(statement.Expression), model)!.Value;
        string receiver = style.WithoutTrivia().ToString();
        string shape = string.Join(".", path);
        switch (statement.Expression)
        {
            case AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment:
                if (shape == "Fill.PatternType")
                {
                    return IsSolid(assignment.Right, model) && Neighbour(statement, 1) is { } next && IsBackgroundSetColor(next, receiver, model)
                        ? new FixedResult([], [])
                        : Manual(EPPlusReason.FillWithoutSolidPattern, shape, "a fill pattern other than Solid, or not directly followed by Fill.BackgroundColor.SetColor on the same style");
                }

                if (StylesMapping.Assignment(shape, assignment.Right, model) is not (string target, string value))
                {
                    return Manual(EPPlusReason.UnmappedEPPlusStyle, shape, "this style property or value is not mapped yet");
                }

                return PatternResult.Fixed(Migrated($"{receiver}.{target} = {value};"), ClosedXmlNamespace);
            case InvocationExpressionSyntax { ArgumentList.Arguments: [var color] } when shape == "Fill.BackgroundColor.SetColor" && IsColor(color.Expression, model):
                return Neighbour(statement, -1) is { } previous && IsSolidAssignment(previous, receiver, model)
                    ? PatternResult.Fixed(Migrated($"{receiver}.Fill.BackgroundColor = XLColor.FromColor({color.Expression.WithoutTrivia()});"), ClosedXmlNamespace)
                    : Manual(EPPlusReason.FillWithoutSolidPattern, shape, "the colour is not directly preceded by Fill.PatternType = ExcelFillStyle.Solid on the same style");
            case InvocationExpressionSyntax { ArgumentList.Arguments: [var color] } when shape == "Font.Color.SetColor" && IsColor(color.Expression, model):
                return PatternResult.Fixed(Migrated($"{receiver}.Font.FontColor = XLColor.FromColor({color.Expression.WithoutTrivia()});"), ClosedXmlNamespace);
            default:
                return Manual(EPPlusReason.UnmappedEPPlusStyle, shape, "this style call is not mapped yet");
        }
    }

    // The member access the statement starts from: the left side of an assignment, or the method of a call.
    private static ExpressionSyntax? Target(ExpressionSyntax expression) => expression switch
    {
        AssignmentExpressionSyntax assignment => assignment.Left,
        InvocationExpressionSyntax invocation => invocation.Expression,
        _ => null,
    };

    // The rewritten statement, marked so a later pattern can tell it is already ClosedXML while its receiver is not yet (rows 3-4).
    private static StatementSyntax Migrated(string text) =>
        SyntaxFactory.ParseStatement(text).WithAdditionalAnnotations(new SyntaxAnnotation(MigratedAnnotationKind));

    private static PatternResult Manual(EPPlusReason code, string shape, string why) =>
        PatternResult.Manual(code, $"EPPlus style '{shape}': {why} (queue row 8). Migrate it by hand.");

    private static ExpressionStatementSyntax? Neighbour(ExpressionStatementSyntax statement, int offset)
    {
        if (statement.Parent is not BlockSyntax block)
        {
            return null;
        }

        int index = block.Statements.IndexOf(statement) + offset;
        return index >= 0 && index < block.Statements.Count ? block.Statements[index] as ExpressionStatementSyntax : null;
    }

    private static bool IsSolidAssignment(ExpressionStatementSyntax statement, string receiver, SemanticModel model) =>
        statement.Expression is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment
        && StylesMapping.Split(assignment.Left, model) is ({ } style, var path) && string.Join(".", path) == "Fill.PatternType"
        && style.WithoutTrivia().ToString() == receiver && IsSolid(assignment.Right, model);

    private static bool IsBackgroundSetColor(ExpressionStatementSyntax statement, string receiver, SemanticModel model) =>
        statement.Expression is InvocationExpressionSyntax { ArgumentList.Arguments: [var color] } invocation
        && StylesMapping.Split(invocation.Expression, model) is ({ } style, var path) && string.Join(".", path) == "Fill.BackgroundColor.SetColor"
        && style.WithoutTrivia().ToString() == receiver && IsColor(color.Expression, model);

    private static bool IsSolid(ExpressionSyntax value, SemanticModel model) => StylesMapping.EnumMember(value, model, "ExcelFillStyle") == "Solid";

    private static bool IsColor(ExpressionSyntax value, SemanticModel model) =>
        model.GetTypeInfo(value).Type is { Name: "Color", ContainingNamespace: { Name: "Drawing", ContainingNamespace.Name: "System" } };
}
