using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// Keeps the preprocessor directives of a profile constructor balanced after its CreateMap statements are removed (TAAK-280). A directive is leading
/// trivia of the next token, so removing the statement that carried `#region X` leaves its `#endregion` behind (CS1028). A stray #region/#endregion
/// is dropped (it only groups lines); a stray #if/#else/#endif is not: the other branch is another build configuration, which is principally manual.
/// </summary>
internal static class ProfileDirectives
{
    // Null with the repaired constructor, or why the conditional compilation around the removed maps must stay Manual.
    public static string? Repair(ConstructorDeclarationSyntax constructor, out ConstructorDeclarationSyntax repaired)
    {
        repaired = constructor;
        if (constructor.Body is not { } body)
        {
            return null;
        }

        var open = new Stack<SyntaxTrivia>();
        var stray = new HashSet<SyntaxTrivia>();
        foreach (SyntaxTrivia trivia in body.DescendantTrivia().Where(t => t.IsDirective))
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.RegionDirectiveTrivia or SyntaxKind.IfDirectiveTrivia:
                    open.Push(trivia);
                    break;
                case SyntaxKind.EndRegionDirectiveTrivia:
                    Close(open, stray, trivia, SyntaxKind.RegionDirectiveTrivia);
                    break;
                case SyntaxKind.EndIfDirectiveTrivia:
                    Close(open, stray, trivia, SyntaxKind.IfDirectiveTrivia);
                    break;
                case SyntaxKind.ElseDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia when !(open.Count > 0 && open.Peek().IsKind(SyntaxKind.IfDirectiveTrivia)):
                    stray.Add(trivia);
                    break;
            }
        }

        stray.UnionWith(open);
        if (stray.Count == 0)
        {
            return null;
        }

        if (stray.Any(t => !t.IsKind(SyntaxKind.RegionDirectiveTrivia) && !t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)))
        {
            return "a CreateMap sits inside conditional compilation (#if), so the mapping method would exist in every build configuration; write it by hand";
        }

        repaired = constructor.ReplaceTokens(
            constructor.Body.DescendantTokens().Where(token => token.LeadingTrivia.Any(stray.Contains)),
            (token, _) => token.WithLeadingTrivia(WithoutStray(token.LeadingTrivia, stray)));
        return null;
    }

    private static void Close(Stack<SyntaxTrivia> open, HashSet<SyntaxTrivia> stray, SyntaxTrivia closer, SyntaxKind opener)
    {
        if (open.Count > 0 && open.Peek().IsKind(opener))
        {
            open.Pop();
        }
        else
        {
            stray.Add(closer);
        }
    }

    // The trivia without the stray directives and the indentation in front of each.
    private static SyntaxTriviaList WithoutStray(SyntaxTriviaList trivia, HashSet<SyntaxTrivia> stray)
    {
        var kept = new List<SyntaxTrivia>();
        foreach (SyntaxTrivia item in trivia)
        {
            if (stray.Contains(item))
            {
                if (kept.Count > 0 && kept[^1].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    kept.RemoveAt(kept.Count - 1);
                }

                continue;
            }

            kept.Add(item);
        }

        return SyntaxFactory.TriviaList(kept);
    }
}
