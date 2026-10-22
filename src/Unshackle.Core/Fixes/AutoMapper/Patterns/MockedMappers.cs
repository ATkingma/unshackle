using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// TAAK-311 (council A1): the mocked IMappers of a test file and whether their stubs can go. A stub goes only when the value it returns
/// provably equals what the generated mapping method returns for the object the test stubs: both are locals of the test member (or the
/// value is created in place), built with new and an object initializer, never changed afterwards, the map is a plain CreateMap&lt;A, B&gt;()
/// (no options), every settable member of B is a member of A with the same name and a simple type, and each member of the value is the
/// same constant as, or is read from, the source. The stubbed object itself is only compared member by member (or B is a record).
/// </summary>
internal static class MockedMappers
{
    /// <summary>Every mock of IMapper the file declares in a local, field or property, with its stubs and constructor uses.</summary>
    public static List<MockedMapper> Find(SyntaxNode root, SemanticModel model)
    {
        Dictionary<ISymbol, SyntaxNode> declared = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            (ISymbol? symbol, ExpressionSyntax? value) = node switch
            {
                VariableDeclaratorSyntax { Initializer.Value: { } initializer } declarator => (model.GetDeclaredSymbol(declarator), initializer),
                PropertyDeclarationSyntax { Initializer.Value: { } initializer } property => (model.GetDeclaredSymbol(property), initializer),
                AssignmentExpressionSyntax { Left: var left, Right: var right } => (model.GetSymbolInfo(left).Symbol, right),
                _ => (null, null),
            };
            if (symbol is ILocalSymbol or IFieldSymbol or IPropertySymbol && value is not null && IsMockCreation(value, model))
            {
                declared.TryAdd(symbol, node);
            }
        }

        List<MockedMapper> mocks = [];
        foreach ((ISymbol mock, SyntaxNode declaration) in declared)
        {
            List<INamedTypeSymbol> classes = [];
            List<MapStub> stubs = [];
            string? problem = null;
            foreach (IdentifierNameSyntax name in root.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(n => n.Identifier.ValueText == mock.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, mock)))
            {
                problem ??= Use(name, model, classes, stubs);
            }

            mocks.Add(new MockedMapper
            {
                Mock = mock,
                Key = Key(mock, root),
                Classes = [.. classes.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)],
                Stubs = stubs,
                Problem = problem,
                Where = MapOwners.Where(declaration),
            });
        }

        return mocks;
    }

    /// <summary>
    /// Why the value the stub returns may differ from what the generated method returns for its source (with file:line), or null when it
    /// provably equals it. <paramref name="isPlainMap"/> answers whether a pair is a plain CreateMap&lt;A, B&gt;() of a profile without settings.
    /// </summary>
    public static string? Disproof(MapStub stub, SemanticModel model, Func<ITypeSymbol, ITypeSymbol, bool> isPlainMap)
    {
        string where = stub.Where;
        if (stub.Statement.FirstAncestorOrSelf<MemberDeclarationSyntax>() is not { } member)
        {
            return $"the stub at {where} is not inside a member";
        }

        if (Creation(stub.Source, model, member, out ILocalSymbol? source) is not { } sourceCreation || source is null)
        {
            return $"the stub at {where} maps an object the test does not create itself with new in the same member (an argument matcher such as It.IsAny or Arg.Any answers any object with the same value)";
        }

        if (Creation(stub.Value, model, member, out ILocalSymbol? value) is not { } valueCreation)
        {
            return $"the stub at {where} returns an object the test does not create itself with new";
        }

        if (model.GetTypeInfo(sourceCreation).Type is not INamedTypeSymbol from || model.GetTypeInfo(valueCreation).Type is not INamedTypeSymbol to
            || !SymbolEqualityComparer.Default.Equals(to, stub.To) || (stub.From is not null && !SymbolEqualityComparer.Default.Equals(from, stub.From)))
        {
            return $"the stub at {where} creates objects of other types than the map it stubs";
        }

        if (!isPlainMap(from, to))
        {
            return $"the map {from.Name} → {to.Name} stubbed at {where} is not a plain CreateMap<{from.Name}, {to.Name}>() in a profile without settings";
        }

        if ((Unchanged(member, model, source) ?? (value is null ? null : Unchanged(member, model, value))) is { } changed)
        {
            return $"the test changes {changed} after creating it ({where})";
        }

        if (value is not null && !to.IsRecord && WholeUse(member, model, value, stub.Value) is { } use)
        {
            return $"the test uses the stubbed {to.Name} itself at {use}, not only its members; the generated method returns another object";
        }

        if (to.InstanceConstructors.Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length > 0))
        {
            return $"{to.Name} has a constructor with parameters, which the mapping may use (stub at {where})";
        }

        foreach (IPropertySymbol property in Settable(to))
        {
            if (Property(from, property.Name) is not { } read || !SymbolEqualityComparer.Default.Equals(read.Type, property.Type) || !IsSimple(property.Type))
            {
                return $"{to.Name}.{property.Name} is not a plain copy of a {from.Name} member of a simple type (stub at {where})";
            }

            string? expected = Value(sourceCreation, read, model, source, null);
            string? actual = Value(valueCreation, property, model, source, sourceCreation);
            if (expected is null || actual is null || expected != actual)
            {
                return $"the stubbed {to.Name}.{property.Name} is not provably the value the generated method copies from {from.Name}.{property.Name} (stub at {where})";
            }
        }

        return to.GetMembers().OfType<IFieldSymbol>().Any(f => f is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, IsImplicitlyDeclared: false })
            ? $"{to.Name} has public fields (stub at {where})"
            : null;
    }

    private static bool IsMockCreation(ExpressionSyntax value, SemanticModel model) =>
        value is BaseObjectCreationExpressionSyntax or InvocationExpressionSyntax && MapOwners.IsMockApi(model.GetSymbolInfo(value).Symbol)
        && model.GetTypeInfo(value).Type is { } type
        && (MapOwners.IsMapper(type) || type is INamedTypeSymbol { Name: "Mock", TypeArguments: [{ } mocked] } && MapOwners.IsMapper(mocked));

    private static string Key(ISymbol mock, SyntaxNode root) =>
        $"{root.SyntaxTree.FilePath}|{mock.ContainingSymbol?.ToDisplayString()}|{mock.Name}";

    // One reference to the mock: its creation, a constructor argument of a class of the solution, or a stub. Anything else: why, with file:line.
    private static string? Use(IdentifierNameSyntax name, SemanticModel model, List<INamedTypeSymbol> classes, List<MapStub> stubs)
    {
        ExpressionSyntax expression = name.Parent is MemberAccessExpressionSyntax { Name: var member } access && member == name ? access : name;
        if (expression.Parent is AssignmentExpressionSyntax { Left: var left, Right: var right } && left == expression)
        {
            return IsMockCreation(right, model) ? null : $"the mock is assigned another value at {MapOwners.Where(expression)}";
        }

        ExpressionSyntax used = expression.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Object" } moq && moq.Expression == expression ? moq : expression;
        if (used.Parent is ArgumentSyntax { Parent.Parent: BaseObjectCreationExpressionSyntax creation } && MapOwners.IsMapper(model.GetTypeInfo(used).Type)
            && model.GetTypeInfo(creation).Type is INamedTypeSymbol type && type.Locations.Any(l => l.IsInSource))
        {
            classes.Add(type);
            return null;
        }

        return used == expression && Stub(expression, model) is { } stub
            ? Add(stub)
            : $"the mock is used another way at {MapOwners.Where(expression)} ({Shorten(expression.FirstAncestorOrSelf<StatementSyntax>()?.ToString() ?? expression.Parent?.ToString() ?? "")})";

        string? Add(MapStub found)
        {
            stubs.Add(found);
            return null;
        }
    }

    // mock.Setup(m => m.Map<B>(source)).Returns(value); or mock.Map<B>(source).Returns(value); as a statement of its own, else null.
    private static MapStub? Stub(ExpressionSyntax mock, SemanticModel model)
    {
        InvocationExpressionSyntax? map = null;
        InvocationExpressionSyntax? stubbed = null;
        if (mock.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Setup", Parent: InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: SimpleLambdaExpressionSyntax { ExpressionBody: InvocationExpressionSyntax body } lambda }] } setUp }
            && MapOwners.IsMockApi(model.GetSymbolInfo(setUp).Symbol)
            && body.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax parameter } && parameter.Identifier.ValueText == lambda.Parameter.Identifier.ValueText)
        {
            (map, stubbed) = (body, setUp);
        }
        else if (mock.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax call } && call.Expression == mock.Parent)
        {
            (map, stubbed) = (call, call);
        }

        if (map is null || stubbed?.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Returns", Parent: InvocationExpressionSyntax { ArgumentList.Arguments: [{ } value] } returns }
            || returns.Parent is not ExpressionStatementSyntax statement || !MapOwners.IsMockApi(model.GetSymbolInfo(returns).Symbol)
            || !MapCallPattern.IsMapCall(map, model) || model.GetSymbolInfo(map).Symbol is not IMethodSymbol method)
        {
            return null;
        }

        ExpressionSyntax source = map.ArgumentList.Arguments[0].Expression;
        return new MapStub
        {
            Statement = statement,
            Call = map,
            Source = source,
            Value = value.Expression,
            From = method.TypeArguments.Length == 2 ? method.TypeArguments[0] : model.GetTypeInfo(source).Type,
            To = method.TypeArguments[^1],
            Where = MapOwners.Where(statement),
        };
    }

    // The new A { ... } the expression is (in place) or the local it names was declared with, in the member; local is that local (or null).
    private static BaseObjectCreationExpressionSyntax? Creation(ExpressionSyntax expression, SemanticModel model, MemberDeclarationSyntax member, out ILocalSymbol? local)
    {
        local = null;
        ExpressionSyntax? created = expression;
        if (expression is IdentifierNameSyntax && model.GetSymbolInfo(expression).Symbol is ILocalSymbol symbol)
        {
            local = symbol;
            created = member.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault(d => SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(d), symbol))?.Initializer?.Value;
        }

        return created is BaseObjectCreationExpressionSyntax creation && (creation.ArgumentList?.Arguments.Count ?? 0) == 0
            && (creation.Initializer is null || creation.Initializer.IsKind(SyntaxKind.ObjectInitializerExpression)
                && creation.Initializer.Expressions.All(e => e is AssignmentExpressionSyntax { Left: IdentifierNameSyntax }))
            ? creation
            : null;
    }

    // The local's name when the member assigns it again, changes one of its members, or passes it by ref or out; else null.
    private static string? Unchanged(MemberDeclarationSyntax member, SemanticModel model, ILocalSymbol local)
    {
        foreach (SyntaxNode node in member.DescendantNodes())
        {
            ExpressionSyntax? target = node switch
            {
                AssignmentExpressionSyntax assignment when assignment.Parent is not InitializerExpressionSyntax => assignment.Left,
                PostfixUnaryExpressionSyntax postfix => postfix.Operand,
                PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression) => prefix.Operand,
                ArgumentSyntax { RefKindKeyword.RawKind: not 0 } argument => argument.Expression,
                _ => null,
            };
            while (target is MemberAccessExpressionSyntax access)
            {
                target = access.Expression;
            }

            if (target is not null && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(target).Symbol, local))
            {
                return local.Name;
            }
        }

        return null;
    }

    // File:line of a use of the value local other than the Returns argument and reading one of its members; else null.
    private static string? WholeUse(MemberDeclarationSyntax member, SemanticModel model, ILocalSymbol local, ExpressionSyntax returned) =>
        member.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(n => n != returned && n.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, local))
            .Where(n => n.Parent is not MemberAccessExpressionSyntax { Expression: var owner } || owner != n)
            .Select(MapOwners.Where).FirstOrDefault();

    // The settable public instance properties of a type and its base types: the members a plain map writes.
    private static IEnumerable<IPropertySymbol> Settable(INamedTypeSymbol type)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (INamedTypeSymbol? current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p is { IsStatic: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public, SetMethod.DeclaredAccessibility: Accessibility.Public, IsImplicitlyDeclared: false }))
            {
                if (seen.Add(property.Name) && property.Name != "EqualityContract")
                {
                    yield return property;
                }
            }
        }
    }

    private static IPropertySymbol? Property(INamedTypeSymbol type, string name)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.GetMembers(name).OfType<IPropertySymbol>().FirstOrDefault(p => p is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod.DeclaredAccessibility: Accessibility.Public }) is { } property)
            {
                return property;
            }
        }

        return null;
    }

    private static bool IsSimple(ITypeSymbol type)
    {
        ITypeSymbol plain = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ } inner] } ? inner : type;
        return plain.TypeKind == TypeKind.Enum
            || plain.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte or SpecialType.System_Byte
                or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64
                or SpecialType.System_UInt64 or SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_String
                or SpecialType.System_DateTime
            || plain is INamedTypeSymbol { Name: "Guid" or "DateTimeOffset" or "TimeSpan" or "DateOnly" or "TimeOnly", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };
    }

    // The value of a member of a created object as text: "c:<constant>" for a constant (the initializer's or the property's own), "s:<name>"
    // for the value the source object holds when it is read from it (or is not constant there), null when it cannot be told.
    private static string? Value(BaseObjectCreationExpressionSyntax creation, IPropertySymbol property, SemanticModel model, ILocalSymbol source, BaseObjectCreationExpressionSyntax? sourceCreation)
    {
        ExpressionSyntax? assigned = creation.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>()
            .FirstOrDefault(a => a.Left is IdentifierNameSyntax { Identifier.ValueText: var name } && name == property.Name)?.Right;
        if (assigned is null)
        {
            return Default(property, model);
        }

        if (sourceCreation is not null && assigned is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax owner, Name.Identifier.ValueText: var read }
            && read == property.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(owner).Symbol, source)
            && model.GetTypeInfo(sourceCreation).Type is INamedTypeSymbol sourceType && Property(sourceType, read) is { } sourceProperty)
        {
            return Value(sourceCreation, sourceProperty, model, source, null);
        }

        Optional<object?> constant = model.GetConstantValue(assigned);
        return constant.HasValue ? Constant(constant.Value)
            : sourceCreation is null ? "s:" + property.Name
            : null;
    }

    // The value a property starts with: its constant initializer, or the type's default; null for any other initializer.
    private static string? Default(IPropertySymbol property, SemanticModel model)
    {
        if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax { Initializer.Value: { } initializer })
        {
            SemanticModel declaring = model.Compilation.GetSemanticModel(initializer.SyntaxTree);
            Optional<object?> constant = declaring.GetConstantValue(initializer);
            return constant.HasValue ? Constant(constant.Value) : null;
        }

        return property.Type.IsReferenceType || property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ? Constant(null)
            : property.Type.SpecialType == SpecialType.System_Boolean ? Constant(false)
            : property.Type.SpecialType is SpecialType.System_String or SpecialType.System_DateTime || property.Type.TypeKind != TypeKind.Enum && !IsNumeric(property.Type) ? "d:" + property.Type.ToDisplayString()
            : Constant(0);
    }

    private static bool IsNumeric(ITypeSymbol type) =>
        type.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32
            or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Decimal or SpecialType.System_Single
            or SpecialType.System_Double or SpecialType.System_Char;

    private static string Constant(object? value) =>
        "c:" + (value switch
        {
            null => "null",
            char c => ((int)c).ToString(CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        });

    private static string Shorten(string text)
    {
        string line = text.ReplaceLineEndings(" ").Trim();
        return line.Length <= 80 ? line : line[..77] + "...";
    }
}
