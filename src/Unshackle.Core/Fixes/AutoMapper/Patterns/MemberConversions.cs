using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// AutoMapper 14's built-in conversion of a member whose type differs from the destination's and has no map, written by hand with the same
/// result for every measured value, culture and null (TAAK-247 has the evidence): T? → T as GetValueOrDefault(), T → T? and widening numbers
/// as is, to string as ToString() (null stays null), other numbers through System.Convert (rounds half to even, throws on overflow),
/// enum ↔ enum by name with the value as fallback, enum ↔ its underlying type as a cast, string → number/bool/DateTime through
/// System.Convert (null gives 0) and string → enum through Enum.Parse (null or empty gives the default). Anything else is no rule (null)
/// or a precise reason.
/// </summary>
public class MemberConversions
{
    private static readonly Dictionary<SpecialType, string> s_convertible = new()
    {
        [SpecialType.System_Boolean] = "Boolean",
        [SpecialType.System_Char] = "Char",
        [SpecialType.System_SByte] = "SByte",
        [SpecialType.System_Byte] = "Byte",
        [SpecialType.System_Int16] = "Int16",
        [SpecialType.System_UInt16] = "UInt16",
        [SpecialType.System_Int32] = "Int32",
        [SpecialType.System_UInt32] = "UInt32",
        [SpecialType.System_Int64] = "Int64",
        [SpecialType.System_UInt64] = "UInt64",
        [SpecialType.System_Single] = "Single",
        [SpecialType.System_Double] = "Double",
        [SpecialType.System_Decimal] = "Decimal",
    };

    private readonly SemanticModel _model;
    private readonly int _position;
    private readonly bool _nullableWarnings;
    private readonly HashSet<string> _namespaces;

    public MemberConversions(SemanticModel model, int position, bool nullableWarnings, HashSet<string> namespaces)
    {
        _model = model;
        _position = position;
        _nullableWarnings = nullableWarnings;
        _namespaces = namespaces;
    }

    /// <summary>
    /// Why value (an expression of type from) cannot become AutoMapper's built-in conversion to to, or null with the expression in result;
    /// result null when this fix knows no such conversion (the member stays Manual with the generic reason). exact: result has type to
    /// itself, not one that converts to it implicitly (a lambda that converts collection elements needs that).
    /// </summary>
    public string? Problem(string what, string value, ITypeSymbol from, ITypeSymbol to, out string? result, out bool exact)
    {
        result = null;
        exact = true;
        if (from.TypeKind is TypeKind.Error or TypeKind.Dynamic or TypeKind.TypeParameter or TypeKind.Pointer
            || to.TypeKind is TypeKind.Error or TypeKind.Dynamic or TypeKind.TypeParameter or TypeKind.Pointer)
        {
            return null;
        }

        if (to.SpecialType == SpecialType.System_String)
        {
            return ToStringProblem(what, value, from, to, out result);
        }

        if (from.SpecialType == SpecialType.System_String)
        {
            return FromStringProblem(what, value, to, out result);
        }

        bool fromNullable = IsNullable(from, out ITypeSymbol fromValue);
        bool toNullable = IsNullable(to, out ITypeSymbol toValue);
        if (SymbolEqualityComparer.Default.Equals(fromValue, toValue))
        {
            // Same type but one side is Nullable<T>: AutoMapper gives null the default value (measured, also with an initializer on the member).
            result = fromNullable ? $"{Receiver(value)}.GetValueOrDefault()" : value;
            exact = fromNullable;
            return null;
        }

        // form: how a fromValue becomes a toValue, given the operand text. defaultSafe: default(fromValue) becomes default(toValue) without
        // throwing, so a null from can go through GetValueOrDefault(). lifted: the form for from? → to? (null: none). implicitly: the forms
        // leave the value to an implicit conversion.
        if (CoreProblem(what, value, fromValue, toValue, out Func<string, string>? form, out bool defaultSafe, out Func<string, string>? lifted, out bool implicitly) is string problem)
        {
            return problem;
        }

        if (form is null)
        {
            return null;
        }

        string Name(ITypeSymbol type) => type.ToMinimalDisplayString(_model, _position);
        exact = !implicitly;
        switch (fromNullable, toNullable)
        {
            case (false, true) when lifted is not null && !implicitly:
                // (int?)source.X rather than (int)source.X: the same value, typed as the destination (a Select over elements needs that).
                result = lifted(value);
                return null;
            case (false, true):
                result = form(value);
                exact = false;
                return null;
            case (false, false):
                result = form(value);
                return null;
            case (true, false) when defaultSafe:
                result = form($"{Receiver(value)}.GetValueOrDefault()");
                return null;
            case (true, false):
                return $"{what} is {Name(from)} but the destination is {Name(to)}: AutoMapper gives null the default {Name(toValue)}, which the conversion of {Name(fromValue)}'s default does not";
            case (true, true) when lifted is not null:
                result = lifted(value);
                return null;
            case (true, true) when !defaultSafe:
                return $"{what} is {Name(from)} but the destination is {Name(to)}: AutoMapper keeps null and maps any other value by name, which needs the switch over a non-null value";
            default:
                return $"{what} is {Name(from)} but the destination is {Name(to)}: AutoMapper keeps null and converts any other value with an overflow check, a cast has none";
        }
    }

    // AutoMapper's ToStringMapper runs before any conversion operator: ToString() for every type, null stays null (also over an initializer).
    private string? ToStringProblem(string what, string value, ITypeSymbol from, ITypeSymbol to, out string? result)
    {
        result = null;
        ITypeSymbol fromValue = IsNullable(from, out ITypeSymbol inner) ? inner : from;
        if (fromValue.TypeKind == TypeKind.Enum && HasEnumMember(fromValue))
        {
            return $"{what}: AutoMapper writes the [EnumMember] value of {fromValue.Name} to the string, ToString() writes the name";
        }

        if (from.IsValueType && !IsNullable(from, out _))
        {
            result = $"{Receiver(value)}.ToString()";
            return null;
        }

        // AutoMapper writes null into a non-nullable string too; the ! keeps that without a warning.
        result = $"{Receiver(value)}?.ToString()" + (_nullableWarnings && to.NullableAnnotation == NullableAnnotation.NotAnnotated ? "!" : "");
        return null;
    }

    private string? FromStringProblem(string what, string value, ITypeSymbol to, out string? result)
    {
        result = null;
        if (IsNullable(to, out ITypeSymbol toValue) && (toValue.TypeKind == TypeKind.Enum || s_convertible.ContainsKey(toValue.SpecialType)
            || toValue.SpecialType == SpecialType.System_DateTime))
        {
            return $"{what} is a string but the destination is {to.ToMinimalDisplayString(_model, _position)}: AutoMapper keeps null as null and parses any other string (an empty one too)";
        }

        if (to.TypeKind == TypeKind.Enum)
        {
            if (HasEnumMember(to))
            {
                return $"{what}: AutoMapper also accepts the [EnumMember] values of {to.Name}, Enum.Parse does not";
            }

            if (!MapCallPattern.HasNoSideEffects(SyntaxFactory.ParseExpression(value)))
            {
                return $"{what}: the string would be evaluated twice (null or empty gives the default, otherwise Enum.Parse)";
            }

            // Measured: null and "" give the default, any other string goes through Enum.Parse(ignoreCase: true) (names, numbers, flags, spaces).
            string type = to.ToMinimalDisplayString(_model, _position);
            _namespaces.Add("System");
            result = $"string.IsNullOrEmpty({value}) ? default : " + (HasGenericEnumParse()
                ? $"Enum.Parse<{type}>({value}, true)"
                : $"({type})Enum.Parse(typeof({type}), {value}, true)");
            return null;
        }

        // Convert.ToX(string) parses with the current culture and gives null the default, as AutoMapper does (measured with nl-NL).
        if (s_convertible.TryGetValue(to.SpecialType, out string? name) && to.SpecialType != SpecialType.System_Char)
        {
            _namespaces.Add("System");
            result = $"Convert.To{name}({value})";
            return null;
        }

        if (to.SpecialType == SpecialType.System_DateTime)
        {
            _namespaces.Add("System");
            result = $"Convert.ToDateTime({value})";
            return null;
        }

        return to.SpecialType == SpecialType.System_Char || to.ToDisplayString() is "System.Guid" or "System.TimeSpan" or "System.DateTimeOffset" or "System.DateOnly" or "System.TimeOnly"
            ? $"{what}: AutoMapper parses the string to {to.Name} and gives null the default {to.Name}, a Parse call throws on null"
            : null;
    }

    // The conversion between two non-nullable types other than string, or a reason, or no form at all (no rule).
    private string? CoreProblem(string what, string value, ITypeSymbol from, ITypeSymbol to, out Func<string, string>? form, out bool defaultSafe,
        out Func<string, string>? lifted, out bool implicitly)
    {
        form = null;
        defaultSafe = true;
        lifted = null;
        implicitly = false;
        string fromName = from.ToMinimalDisplayString(_model, _position);
        string toName = to.ToMinimalDisplayString(_model, _position);
        bool fromEnum = from is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: not null };
        bool toEnum = to is INamedTypeSymbol { TypeKind: TypeKind.Enum };
        if (fromEnum && toEnum)
        {
            return EnumProblem(what, value, (INamedTypeSymbol)from, (INamedTypeSymbol)to, out form, out defaultSafe, out lifted);
        }

        if (fromEnum || toEnum)
        {
            var enumType = (INamedTypeSymbol)(fromEnum ? from : to);
            ITypeSymbol other = fromEnum ? to : from;
            if (SymbolEqualityComparer.Default.Equals(enumType.EnumUnderlyingType, other))
            {
                // EnumToUnderlyingType / UnderlyingTypeToEnum: a plain conversion of the value (undefined values too).
                form = operand => $"({toName}){Receiver(operand)}";
                lifted = operand => $"({toName}?){Receiver(operand)}";
                return null;
            }

            return s_convertible.ContainsKey(other.SpecialType)
                ? $"{what}: AutoMapper 14 throws at run time for {fromName} to {toName} (an enum converts only to and from its underlying type {enumType.EnumUnderlyingType!.Name})"
                : null;
        }

        if (s_convertible.ContainsKey(from.SpecialType) && s_convertible.TryGetValue(to.SpecialType, out string? convertName))
        {
            bool charAndOther = (from.SpecialType == SpecialType.System_Char || to.SpecialType == SpecialType.System_Char)
                && (from.SpecialType is SpecialType.System_Boolean or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal
                    || to.SpecialType is SpecialType.System_Boolean or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal);
            if (charAndOther)
            {
                return $"{what}: AutoMapper 14 throws InvalidCastException at run time for {fromName} to {toName}";
            }

            if (_model.Compilation.ClassifyConversion(from, to) is { IsImplicit: true, IsNumeric: true })
            {
                // Widening: Convert gives the same value as the implicit conversion (measured, also int → float rounding).
                form = operand => operand;
                lifted = operand => operand;
                implicitly = true;
                return null;
            }

            // AutoMapper's ConvertMapper calls System.Convert: half to even and OverflowException, where a cast truncates and wraps.
            _namespaces.Add("System");
            form = operand => $"Convert.To{convertName}({operand})";
            return null;
        }

        if (from.ToDisplayString() == "System.DateTime" && to.ToDisplayString() == "System.DateTimeOffset")
        {
            // The implicit operator, as AutoMapper's ConversionOperatorMapper; default(DateTime) can throw for a positive local offset.
            form = operand => operand;
            lifted = operand => operand;
            defaultSafe = false;
            implicitly = true;
            return null;
        }

        return (from.ToDisplayString(), to.ToDisplayString()) is ("System.DateTimeOffset", "System.DateTime") or ("System.DateTime", "System.DateOnly") or ("System.DateOnly", "System.DateTime")
            ? $"{what}: AutoMapper 14 throws at run time for {fromName} to {toName} (no built-in conversion)"
            : null;
    }

    // EnumToEnumMapper: a defined source value whose name (ToString()) the destination has, ignoring case, gets that member; anything else
    // the same number. A cast when every name lands on its own value, otherwise a switch with an arm per name that does not.
    private string? EnumProblem(string what, string value, INamedTypeSymbol from, INamedTypeSymbol to, out Func<string, string>? form, out bool defaultSafe, out Func<string, string>? lifted)
    {
        form = null;
        lifted = null;
        defaultSafe = true;
        string toName = to.ToMinimalDisplayString(_model, _position);
        if (!SymbolEqualityComparer.Default.Equals(from.EnumUnderlyingType, to.EnumUnderlyingType))
        {
            return $"{what}: {from.Name} and {to.Name} have different underlying types";
        }

        if (HasEnumMember(from) || HasEnumMember(to))
        {
            return $"{what}: {from.Name} or {to.Name} has [EnumMember] names";
        }

        List<IFieldSymbol> toFields = [.. to.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue)];
        List<(IFieldSymbol From, IFieldSymbol To)> arms = [];
        foreach (IGrouping<object?, IFieldSymbol> sameValue in from.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue).GroupBy(f => f.ConstantValue))
        {
            List<IFieldSymbol?> targets = [];
            foreach (IFieldSymbol field in sameValue)
            {
                List<IFieldSymbol> named = [.. toFields.Where(f => string.Equals(f.Name, field.Name, StringComparison.OrdinalIgnoreCase))];
                if (named.Count > 1)
                {
                    return $"{what}: {to.Name} has several members named {field.Name} ignoring case";
                }

                targets.Add(named is [{ } target] && !Equals(target.ConstantValue, field.ConstantValue) ? target : null);
            }

            if (targets.Distinct(SymbolEqualityComparer.Default).Count() > 1)
            {
                // ToString() picks one of the names of a value; which one decides the result.
                return $"{what}: {from.Name} has several names for the value {sameValue.Key} that {to.Name} maps differently";
            }

            if (targets[0] is { } differs)
            {
                arms.Add((sameValue.First(), differs));
            }
        }

        if (arms.Count == 0)
        {
            form = operand => $"({toName}){Receiver(operand)}";
            lifted = operand => $"({toName}?){Receiver(operand)}";
            return null;
        }

        // default(from) may be renamed to a non-zero member: a null source would differ, so no GetValueOrDefault() and no lifted form.
        defaultSafe = false;
        if (!MapCallPattern.HasNoSideEffects(SyntaxFactory.ParseExpression(value)))
        {
            return $"{what}: the {from.Name} value would be evaluated twice in the switch that converts it to {to.Name}";
        }

        form = operand => $"{Receiver(operand)} switch {{ "
            + string.Concat(arms.Select(a => $"{a.From.ToMinimalDisplayString(_model, _position)} => {a.To.ToMinimalDisplayString(_model, _position)}, "))
            + $"_ => ({toName}){Receiver(operand)} }}";
        return null;
    }

    private bool HasGenericEnumParse() =>
        _model.Compilation.GetSpecialType(SpecialType.System_Enum).GetMembers("Parse").OfType<IMethodSymbol>()
            .Any(m => m.IsGenericMethod && m.Parameters is [{ Type.SpecialType: SpecialType.System_String }, { Type.SpecialType: SpecialType.System_Boolean }]);

    private static bool HasEnumMember(ITypeSymbol type) =>
        type.GetMembers().OfType<IFieldSymbol>().Any(f => f.GetAttributes().Any(a => a.AttributeClass is
        {
            Name: "EnumMemberAttribute",
            ContainingNamespace: { Name: "Serialization", ContainingNamespace: { Name: "Runtime", ContainingNamespace: { Name: "System" } } },
        }));

    private static bool IsNullable(ITypeSymbol type, out ITypeSymbol value)
    {
        value = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ } argument] } ? argument : type;
        return !ReferenceEquals(value, type);
    }

    private static string Receiver(string value) =>
        MapCallPattern.NeedsParentheses(SyntaxFactory.ParseExpression(value)) ? $"({value})" : value;
}
