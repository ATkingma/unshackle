using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-309 (queue row 1): the EPPlus 5+ licence lines, <c>ExcelPackage.LicenseContext = ...;</c> and the 8+ <c>ExcelPackage.License.SetXxx(...);</c>,
/// are removed. ClosedXML has no licence switch, so nothing replaces them. Only a statement of its own in a block is removed; the same
/// expression anywhere else (an expression-bodied member, an embedded <c>if</c> statement) is Manual.
/// </summary>
public class LicenseContextPattern : IPattern
{
    public string Id => "epplus.license-context";

    public string Description => "ExcelPackage.LicenseContext = ...; / ExcelPackage.License.SetXxx(...); → removed (ClosedXML has no licence switch)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ExpressionSyntax>().Where(e => IsLicenseLine(e, model))
            .Select(e => e.Parent is ExpressionStatementSyntax statement ? (SyntaxNode)statement : e);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        node is ExpressionStatementSyntax { Parent: BlockSyntax or GlobalStatementSyntax }
            ? new FixedResult([], [])
            : PatternResult.Manual("EPPlus licence line that is not a statement of its own in a block; remove it by hand (ClosedXML has no licence switch).");

    // ExcelPackage.LicenseContext = x, or ExcelPackage.License.SetXxx(...).
    private static bool IsLicenseLine(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "LicenseContext" } left } =>
            EPPlusSyntax.IsPackageType(left.Expression, model),
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var method, Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "License" } license } } =>
            method.StartsWith("Set", StringComparison.Ordinal) && EPPlusSyntax.IsPackageType(license.Expression, model),
        _ => false,
    };
}
