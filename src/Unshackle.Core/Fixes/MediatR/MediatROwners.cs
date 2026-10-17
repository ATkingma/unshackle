using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR;

// TAAK-274: which code stays on MediatR when Manual findings block a full migration. Owners (MediatROwner) are joined in a
// union-find wherever they must use the same library: a handler with its message, a call site with what it sends, a field with
// the code that uses it. A link that cannot be followed statically (open generics, type parameters, Send(object)) joins the whole
// kind (requests, notifications, streams). The components of the roots stay; the rest moves. Over-linking only moves less.
internal static class MediatROwners
{
    public const string KeptPatternId = "mediatr.kept";

    private const string Request = "kind:request";
    private const string Notification = "kind:notification";
    private const string Stream = "kind:stream";

    private static readonly HashSet<string> s_assemblies = new(["MediatR", "MediatR.Contracts"], StringComparer.Ordinal);

    private static readonly Dictionary<string, string> s_kinds = new(StringComparer.Ordinal)
    {
        ["IBaseRequest"] = Request,
        ["IRequest"] = Request,
        ["IRequest`1"] = Request,
        ["IRequestHandler`1"] = Request,
        ["IRequestHandler`2"] = Request,
        ["IPipelineBehavior`2"] = Request,
        ["RequestHandlerDelegate`1"] = Request,
        ["IRequestPreProcessor`1"] = Request,
        ["IRequestPostProcessor`2"] = Request,
        ["IRequestExceptionHandler`2"] = Request,
        ["IRequestExceptionHandler`3"] = Request,
        ["IRequestExceptionAction`1"] = Request,
        ["IRequestExceptionAction`2"] = Request,
        ["RequestExceptionHandlerState`1"] = Request,
        ["INotification"] = Notification,
        ["INotificationHandler`1"] = Notification,
        ["INotificationPublisher"] = Notification,
        ["NotificationHandlerExecutor"] = Notification,
        ["IStreamRequest`1"] = Stream,
        ["IStreamRequestHandler`2"] = Stream,
        ["IStreamPipelineBehavior`2"] = Stream,
        ["StreamHandlerDelegate`1"] = Stream,
    };

    // The marker interfaces of a message: in a base list they make a message, anywhere else they stand for every message of the kind.
    private static readonly HashSet<string> s_markers = new(["IBaseRequest", "IRequest", "IRequest`1", "INotification", "IStreamRequest`1"], StringComparer.Ordinal);

    private static readonly Dictionary<string, string> s_sends = new(StringComparer.Ordinal)
    {
        ["Send"] = Request,
        ["Publish"] = Notification,
        ["CreateStream"] = Stream,
    };

    private static readonly Dictionary<string, string> s_lifetimes = new(StringComparer.Ordinal)
    {
        ["AddScoped"] = "Scoped",
        ["AddTransient"] = "Transient",
        ["AddSingleton"] = "Singleton",
    };

    public static async Task<PartialPlan?> PlanAsync(
        Solution solution, IReadOnlyList<Finding> roots, IReadOnlyList<VendoredPackage> vendored, CancellationToken cancellationToken)
    {
        HashSet<string> vendoredNames = [.. vendored.Select(v => v.Name)];
        // shortcut: a vendored package (its source migrated in) next to kept code is untested; all-or-nothing until a repo needs it.
        if (solution.Projects.SelectMany(p => p.MetadataReferences).Any(r => vendoredNames.Contains(Path.GetFileNameWithoutExtension(r.Display ?? string.Empty))))
        {
            return null;
        }

        var graph = new MediatROwnerGraph();
        var references = new Dictionary<DocumentId, List<(SimpleNameSyntax Name, ISymbol Symbol, string Owner)>>();
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root
                || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
            {
                continue;
            }

            // An alias of a MediatR type moves with no using swap: out of scope.
            if (root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => u.Alias is not null && u.Name is not null
                && model.GetSymbolInfo(u.Name, cancellationToken).Symbol is { } aliased && IsMediatR(aliased)))
            {
                return null;
            }

            // Declarations are found by their tree's path: a document without one could not be linked.
            if (string.IsNullOrEmpty(document.FilePath) || root.SyntaxTree.FilePath != document.FilePath)
            {
                return null;
            }

            references[document.Id] = Visit(graph, document.FilePath, root, model, cancellationToken);
        }

        graph.JoinKinds();
        HashSet<string> kept = [];
        Dictionary<string, Finding> rootOf = new(StringComparer.Ordinal);
        foreach (Finding root in roots)
        {
            if (graph.OwnerAt(root.FilePath, root.Line) is not MediatROwner owner)
            {
                return null;
            }

            string component = graph.Find(owner.Key);
            kept.Add(component);
            rootOf.TryAdd(component, root);
        }

        // MediatR stays registered for the kept code (its hand-written registration); Mediator gets a registration of its own.
        List<MediatROwner> runtime = [.. graph.Owners.Values.Where(o => o.NamesRuntime)];
        if (graph.Owners.Values.Any(o => o.CallsAddMediatR) || runtime.Count == 0)
        {
            // shortcut: splitting an AddMediatR(cfg => ...) into a kept and a moving registration is not built (TAAK-274 scope).
            return null;
        }

        kept.UnionWith(runtime.Select(o => graph.Find(o.Key)));
        List<MediatROwner> keptOwners = [.. graph.Owners.Values.Where(o => kept.Contains(graph.Find(o.Key)))];
        if (!graph.Owners.Values.Any(o => o.NamesMediatR && !kept.Contains(graph.Find(o.Key))))
        {
            return null;
        }

        Solution planned = solution;
        HashSet<string> keptKeys = [.. keptOwners.Select(o => o.Key)];
        MediatROwner host = runtime[0];
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            if (!references.TryGetValue(document.Id, out List<(SimpleNameSyntax Name, ISymbol Symbol, string Owner)>? found))
            {
                continue;
            }

            string path = document.FilePath!;
            List<MediatROwner> owners = [.. keptOwners.Where(o => o.Path == path)];
            if (owners.Count == 0)
            {
                continue;
            }

            var root = (CompilationUnitSyntax)(await document.GetSyntaxRootAsync(cancellationToken))!;
            // Inactive #if code is fixed in a later pass over a reparsed copy, without the annotations (Fixer, TAAK-223).
            if (root.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.IfDirectiveTrivia)))
            {
                return null;
            }

            List<TextChange> changes = Qualify(found, keptKeys);
            if (host.Path == path)
            {
                if (Registration(root, host) is not TextChange registration)
                {
                    return null;
                }

                changes.Add(registration);
            }

            SourceText text = (await document.GetTextAsync(cancellationToken)).WithChanges(changes.OrderBy(c => c.Span.Start));
            SyntaxNode newRoot = CSharpSyntaxTree.ParseText(text, (CSharpParseOptions)document.Project.ParseOptions!, path, cancellationToken: cancellationToken)
                .GetRoot(cancellationToken);
            List<SyntaxNode> nodes = [];
            foreach (MediatROwner owner in owners)
            {
                var span = TextSpan.FromBounds(Shift(changes, owner.Span.Start), Shift(changes, owner.Span.End));
                SyntaxNode node = newRoot.FindNode(span);
                if (node.Span != span)
                {
                    return null;
                }

                nodes.Add(node);
            }

            planned = planned.WithDocumentSyntaxRoot(
                document.Id, newRoot.ReplaceNodes(nodes, (_, rewritten) => rewritten.WithAdditionalAnnotations(new SyntaxAnnotation(PartialPlan.KeptAnnotationKind))));
        }

        if (!await RegistersMediatRAsync(planned, host, cancellationToken))
        {
            return null;
        }

        return new PartialPlan { Solution = planned, Findings = Rows(graph, keptOwners, roots, rootOf) };
    }

    // The owners, their links and the messages of one document; returns its references to MediatR types and members.
    private static List<(SimpleNameSyntax Name, ISymbol Symbol, string Owner)> Visit(
        MediatROwnerGraph graph, string path, CompilationUnitSyntax root, SemanticModel model, CancellationToken cancellationToken)
    {
        List<(SimpleNameSyntax Name, ISymbol Symbol, string Owner)> found = [];
        foreach (BaseTypeDeclarationSyntax declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration, cancellationToken) is INamedTypeSymbol type)
            {
                foreach (string kind in type.AllInterfaces.Where(IsMediatR).Select(i => s_kinds.GetValueOrDefault(i.OriginalDefinition.MetadataName)).OfType<string>()
                    .Where(k => s_markers.Any(m => s_kinds[m] == k)).Distinct())
                {
                    graph.AddMessage(MediatROwnerGraph.Key(path, declaration), kind);
                }

                foreach (SyntaxReference other in type.DeclaringSyntaxReferences)
                {
                    graph.Union(MediatROwnerGraph.Key(path, declaration), MediatROwnerGraph.Key(other.SyntaxTree.FilePath, other.GetSyntax(cancellationToken)));
                }
            }
        }

        foreach (SimpleNameSyntax name in root.DescendantNodes(n => n is not UsingDirectiveSyntax).OfType<SimpleNameSyntax>())
        {
            SymbolInfo info = model.GetSymbolInfo(name, cancellationToken);
            if ((info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is not ISymbol symbol || symbol is INamespaceSymbol)
            {
                continue;
            }

            bool mediatR = IsMediatR(symbol);
            bool holder = !mediatR && symbol is IFieldSymbol or IPropertySymbol or IParameterSymbol or ILocalSymbol or IMethodSymbol or IEventSymbol
                && NamesMediatR(symbol) && symbol.DeclaringSyntaxReferences.Length > 0;
            bool baseType = !mediatR && symbol is INamedTypeSymbol { DeclaringSyntaxReferences.Length: > 0 } source && DerivesFromMediatR(source)
                && name.Ancestors().Any(a => a is BaseListSyntax or TypeParameterConstraintClauseSyntax);
            if ((!mediatR && !holder && !baseType) || OwnerOf(name) is not SyntaxNode ownerNode)
            {
                continue;
            }

            MediatROwner owner = graph.Owner(path, ownerNode);
            if (ownerNode is StatementSyntax statement && statement.DescendantNodesAndSelf().Any(n => n is ReturnStatementSyntax or YieldStatementSyntax)
                && MemberOf(statement) is SyntaxNode member)
            {
                graph.Union(owner.Key, MediatROwnerGraph.Key(path, member));
            }

            if (ownerNode is MemberDeclarationSyntax and not BaseTypeDeclarationSyntax)
            {
                LinkImplementation(graph, path, ownerNode, model, cancellationToken);
            }

            if (mediatR)
            {
                found.Add((name, symbol, owner.Key));
                owner.NamesMediatR = true;
                owner.CallsAddMediatR |= symbol is IMethodSymbol { Name: "AddMediatR" };
                owner.NamesRuntime |= symbol is INamedTypeSymbol { Name: "Mediator", TypeKind: TypeKind.Class };
                LinkMediatR(graph, name, symbol, owner.Key, model, cancellationToken);
            }
            else
            {
                foreach (SyntaxReference declaration in (baseType ? ((INamedTypeSymbol)symbol).OriginalDefinition : symbol.OriginalDefinition).DeclaringSyntaxReferences)
                {
                    SyntaxNode declared = declaration.GetSyntax(cancellationToken);
                    if (baseType)
                    {
                        graph.Union(owner.Key, MediatROwnerGraph.Key(declaration.SyntaxTree.FilePath, declared));
                    }
                    else if (OwnerOf(declared) is SyntaxNode declaringOwner)
                    {
                        graph.Union(owner.Key, MediatROwnerGraph.Key(declaration.SyntaxTree.FilePath, declaringOwner));
                    }
                }
            }
        }

        return found;
    }

    // A reference to a MediatR type or member: its type arguments, what a Send/Publish/CreateStream sends, or the whole kind.
    private static void LinkMediatR(MediatROwnerGraph graph, SimpleNameSyntax name, ISymbol symbol, string owner, SemanticModel model, CancellationToken cancellationToken)
    {
        if (symbol is IMethodSymbol method && s_sends.TryGetValue(method.Name, out string? sendKind))
        {
            ITypeSymbol? sent = name.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax { ArgumentList.Arguments: [var first, ..] } }
                ? model.GetTypeInfo(first.Expression, cancellationToken).Type
                : null;
            LinkType(graph, sent, owner, sendKind, cancellationToken);
            return;
        }

        if (symbol is not INamedTypeSymbol type || !s_kinds.TryGetValue(type.OriginalDefinition.MetadataName, out string? kind))
        {
            return;
        }

        if (name is GenericNameSyntax generic)
        {
            if (generic.IsUnboundGenericName)
            {
                graph.Link(owner, kind);
                return;
            }

            foreach (TypeSyntax argument in generic.TypeArgumentList.Arguments)
            {
                LinkType(graph, model.GetTypeInfo(argument, cancellationToken).Type, owner, kind, cancellationToken);
            }
        }
        else if (s_markers.Contains(type.OriginalDefinition.MetadataName) && !name.Ancestors().Any(a => a is BaseListSyntax))
        {
            graph.Link(owner, kind);
        }
    }

    // A message type of the solution joins its declaration; anything else the code can be called with joins the whole kind.
    private static void LinkType(MediatROwnerGraph graph, ITypeSymbol? type, string owner, string kind, CancellationToken cancellationToken)
    {
        if (type is INamedTypeSymbol { DeclaringSyntaxReferences.Length: > 0 } source && DerivesFromMediatR(source))
        {
            foreach (SyntaxReference declaration in source.OriginalDefinition.DeclaringSyntaxReferences)
            {
                graph.Union(owner, MediatROwnerGraph.Key(declaration.SyntaxTree.FilePath, declaration.GetSyntax(cancellationToken)));
            }
        }
        else if (type is null or ITypeParameterSymbol || IsMediatR(type) || type.SpecialType == SpecialType.System_Object
            || (type is INamedTypeSymbol { DeclaringSyntaxReferences.Length: 0 } && type.AllInterfaces.Any(IsMediatR)))
        {
            graph.Link(owner, kind);
        }
    }

    // A member that implements a MediatR interface member (Handle) moves with its type; one that overrides or implements a member of
    // the solution moves with that member.
    private static void LinkImplementation(MediatROwnerGraph graph, string path, SyntaxNode member, SemanticModel model, CancellationToken cancellationToken)
    {
        if (model.GetDeclaredSymbol(member, cancellationToken) is not ISymbol symbol || symbol.ContainingType is not INamedTypeSymbol container)
        {
            return;
        }

        foreach (INamedTypeSymbol contract in container.AllInterfaces)
        {
            foreach (ISymbol required in contract.GetMembers())
            {
                if (!SymbolEqualityComparer.Default.Equals(container.FindImplementationForInterfaceMember(required), symbol))
                {
                    continue;
                }

                IEnumerable<SyntaxReference> targets = IsMediatR(contract)
                    ? container.DeclaringSyntaxReferences
                    : required.DeclaringSyntaxReferences;
                foreach (SyntaxReference target in targets)
                {
                    SyntaxNode node = target.GetSyntax(cancellationToken);
                    graph.Union(MediatROwnerGraph.Key(path, member), MediatROwnerGraph.Key(target.SyntaxTree.FilePath, IsMediatR(contract) ? node : OwnerOf(node) ?? node));
                }
            }
        }

        ISymbol? overridden = symbol switch
        {
            IMethodSymbol m => m.OverriddenMethod,
            IPropertySymbol p => p.OverriddenProperty,
            _ => null,
        };
        foreach (SyntaxReference target in overridden?.DeclaringSyntaxReferences ?? [])
        {
            graph.Union(MediatROwnerGraph.Key(path, member), MediatROwnerGraph.Key(target.SyntaxTree.FilePath, OwnerOf(target.GetSyntax(cancellationToken)) ?? target.GetSyntax(cancellationToken)));
        }
    }

    // The owner of a node: a stand-alone typeof(Open<>), the outermost statement in a body, else the member, else the type.
    internal static SyntaxNode? OwnerOf(SyntaxNode node)
    {
        SyntaxNode? statement = null;
        foreach (SyntaxNode ancestor in node.AncestorsAndSelf())
        {
            switch (ancestor)
            {
                case TypeOfExpressionSyntax { Type: GenericNameSyntax { IsUnboundGenericName: true } } typeOf when statement is null:
                    return typeOf;
                case GlobalStatementSyntax global:
                    return global;
                // A body (of a member, accessor or lambda) holds the statements; it is not one itself.
                case BlockSyntax block when block.Parent is not StatementSyntax:
                    break;
                case StatementSyntax s:
                    statement = s;
                    break;
                case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                    return statement ?? ancestor;
                case MemberDeclarationSyntax and not BaseNamespaceDeclarationSyntax:
                    return statement ?? ancestor;
            }
        }

        return statement;
    }

    private static SyntaxNode? MemberOf(SyntaxNode node) =>
        node.Ancestors().FirstOrDefault(a => a is MemberDeclarationSyntax and not BaseNamespaceDeclarationSyntax);

    // global::MediatR.X for every kept reference to a MediatR type: the using swap is the whole migration of a swapped type
    // (INotification), so an unqualified kept name would move along with it.
    private static List<TextChange> Qualify(List<(SimpleNameSyntax Name, ISymbol Symbol, string Owner)> found, HashSet<string> keptKeys)
    {
        List<TextChange> changes = [];
        HashSet<int> positions = [];
        foreach ((SimpleNameSyntax name, ISymbol symbol, _) in found.Where(r => r.Symbol is INamedTypeSymbol && r.Name is not IdentifierNameSyntax { IsVar: true } && keptKeys.Contains(r.Owner)))
        {
            if (UsingDirectives.IsGlobalQualified(name))
            {
                continue;
            }

            ExpressionSyntax? left = name.Parent switch
            {
                QualifiedNameSyntax qualified when qualified.Right == name => qualified.Left,
                MemberAccessExpressionSyntax member when member.Name == name => member.Expression,
                AliasQualifiedNameSyntax alias when alias.Name == name => alias,
                _ => null,
            };
            if (left is null)
            {
                if (positions.Add(name.SpanStart))
                {
                    changes.Add(new TextChange(new TextSpan(name.SpanStart, 0), $"global::{symbol.ContainingNamespace.ToDisplayString()}."));
                }

                continue;
            }

            while (left is QualifiedNameSyntax or MemberAccessExpressionSyntax)
            {
                left = left is QualifiedNameSyntax q ? q.Left : ((MemberAccessExpressionSyntax)left).Expression;
            }

            // MediatR.Unit written by the user: from global:: as well. A nested name (Outer.Inner) gets its prefix with Outer.
            if (left is IdentifierNameSyntax { Identifier.ValueText: "MediatR" } && positions.Add(left.SpanStart))
            {
                changes.Add(new TextChange(new TextSpan(left.SpanStart, 0), "global::"));
            }
        }

        return changes;
    }

    // The registration Mediator needs for the code that moves, next to the MediatR registration that stays:
    // services.AddMediatR(cfg => ...) with the same lifetime, which mediatr.add-mediatr turns into services.AddMediator(...).
    private static TextChange? Registration(CompilationUnitSyntax root, MediatROwner host)
    {
        if (root.FindNode(host.Span) is not ExpressionStatementSyntax
            {
                Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var method, Expression: var services } },
            } statement
            || !s_lifetimes.TryGetValue(method, out string? lifetime)
            || statement.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { TypeParameterList: null } type)
        {
            return null;
        }

        string indentation = string.Concat(statement.GetLeadingTrivia().Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse());
        string newLine = statement.GetTrailingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToFullString() is { Length: > 0 } eol ? eol : "\n";
        string text = $"{newLine}{indentation}{services}.AddMediatR(cfg =>{newLine}{indentation}{{{newLine}"
            + $"{indentation}    cfg.RegisterServicesFromAssembly(typeof({type.Identifier.ValueText}).Assembly);{newLine}"
            + $"{indentation}    cfg.Lifetime = ServiceLifetime.{lifetime};{newLine}{indentation}}});";
        return new TextChange(new TextSpan(statement.Span.End, 0), text);
    }

    // The inserted AddMediatR binds to MediatR's, so mediatr.add-mediatr finds it and the project gets Mediator's generator.
    private static async Task<bool> RegistersMediatRAsync(Solution solution, MediatROwner host, CancellationToken cancellationToken)
    {
        foreach (Document document in solution.Projects.SelectMany(p => p.Documents).Where(d => d.FilePath == host.Path))
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            SemanticModel model = (await document.GetSemanticModelAsync(cancellationToken))!;
            if (!root.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddMediatR" }
                && model.GetSymbolInfo(i, cancellationToken).Symbol is IMethodSymbol m && IsMediatR(m)))
            {
                return false;
            }
        }

        return true;
    }

    // A position in the original text, moved past the insertions before it: an insertion at an owner's start is inside the owner,
    // one at its end (the registration after the kept statement) is not.
    private static int Shift(List<TextChange> changes, int position) =>
        position + changes.Where(c => c.Span.Start < position).Sum(c => c.NewText!.Length);

    private static List<Finding> Rows(MediatROwnerGraph graph, List<MediatROwner> keptOwners, IReadOnlyList<Finding> roots, Dictionary<string, Finding> rootOf)
    {
        HashSet<(string, int)> reported = [.. roots.Select(r => (r.FilePath, r.Line))];
        List<Finding> rows = [.. roots];
        foreach (MediatROwner owner in keptOwners.Where(o => o.NamesMediatR).OrderBy(o => o.Path, StringComparer.Ordinal).ThenBy(o => o.StartLine))
        {
            if (!reported.Add((owner.Path, owner.StartLine)))
            {
                continue;
            }

            string why = rootOf.TryGetValue(graph.Find(owner.Key), out Finding? root)
                ? $"it is tied to {Path.GetFileName(root.FilePath)}:{root.Line}, which cannot be migrated"
                : "MediatR's own registration stays for the code that cannot be migrated";
            rows.Add(new Finding
            {
                FilePath = owner.Path,
                Line = owner.StartLine,
                FixId = "mediatr",
                PatternId = KeptPatternId,
                Result = PatternResult.Manual($"Stays on MediatR next to Mediator: {why}. Migrate them together by hand to drop MediatR."),
            });
        }

        return rows;
    }

    private static bool IsMediatR(ISymbol symbol) => symbol.ContainingAssembly is { } assembly && s_assemblies.Contains(assembly.Name);

    // A type of the solution that is a MediatR message, handler or behavior (through any base type or interface).
    private static bool DerivesFromMediatR(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (IsMediatR(current) || current.Interfaces.Any(IsMediatR))
            {
                return true;
            }
        }

        return type.AllInterfaces.Any(IsMediatR);
    }

    // A symbol whose type (or signature) names a MediatR type: a field of IMediator, a parameter of RequestHandlerDelegate<T>.
    private static bool NamesMediatR(ISymbol symbol) => symbol switch
    {
        IFieldSymbol f => NamesMediatR(f.Type),
        IPropertySymbol p => NamesMediatR(p.Type),
        IParameterSymbol p => NamesMediatR(p.Type),
        ILocalSymbol l => NamesMediatR(l.Type),
        IEventSymbol e => NamesMediatR(e.Type),
        IMethodSymbol m => NamesMediatR(m.ReturnType) || m.Parameters.Any(p => NamesMediatR(p.Type)),
        _ => false,
    };

    private static bool NamesMediatR(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => NamesMediatR(array.ElementType),
        INamedTypeSymbol named => IsMediatR(named) || named.TypeArguments.Any(NamesMediatR),
        _ => false,
    };
}
