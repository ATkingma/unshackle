using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.AutoMapper.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class CreateMapPatternTests
{
    // An emptied profile from an earlier run is not found again: a second run gives no Manual that would undo the first (AllOrNothing).
    [Fact]
    public void Find_EmptiedProfile_FindsNothing()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("public class P : AutoMapper.Profile { public P() { } }");

        // Act + Assert
        new CreateMapPattern().Find(root, model).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("public class A { public int id { get; set; } } public class B { public int Id { get; set; } }", "has no member with the same name")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; private set; } }", "has no public setter")]
    [InlineData("public class A { public int Id { get; set; } } public abstract class B { public int Id { get; set; } }", "no public parameterless constructor")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int[] Id { get; set; } = []; }", "(AutoMapper converts)")]
    [InlineData("public class A { public int Id { get; set; } } public class B { private B() { } public B(int id) { } }", "has several constructors or none that is public")]
    [InlineData("public class A { public int Id { get; set; } public int ID; } public class B { public B(int id) { } }", "matches several members of A")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public B(ref int id) { } }", "has a ref, out or params parameter id")]
    public void Rewrite_MapAutoMapperDoesMoreFor_ReportsManual(string types, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(types + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
    }

    // AutoMapper 14 reads the member list only in AssertConfigurationIsValid (TAAK-246): every constant form gives the map without it.
    [Theory]
    [InlineData("MemberList.Destination")]
    [InlineData("MemberList.Source")]
    [InlineData("memberList: MemberList.None")]
    [InlineData("AutoMapper.MemberList.None")]
    [InlineData("(MemberList)1")]
    public void Rewrite_ConstantMemberList_SameAsWithout(string argument)
    {
        // Arrange
        const string Types = "using AutoMapper; public class A { public int Id { get; set; } public string Extra { get; set; } = \"\"; } public class B { public int Id { get; set; } }";
        (SyntaxNode root, SemanticModel model) = Compile(Types + " public class P : Profile { public P() { CreateMap<A, B>(" + argument + "); } }");
        (SyntaxNode plainRoot, SemanticModel plainModel) = Compile(Types + " public class P : Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);
        PatternResult plain = pattern.Rewrite(pattern.Find(plainRoot, plainModel).ShouldHaveSingleItem(), plainModel);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToFullString())
            .ShouldBe(plain.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToFullString()));
    }

    // AutoMapper 14 fills a constructor parameter from the source member of the same name in any case (TAAK-251).
    [Fact]
    public void Rewrite_ConstructorParameterOtherCase_PassesMember()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } public string Name { get; set; } = \"\"; } public class B { public B(int id) { Id = id; } public int Id { get; set; } public string Name { get; set; } = \"\"; } public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string text = result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString();
        text.ShouldContain("public static B ToB(this A source) => new(source.Id)");
        text.ShouldContain("Name = source.Name,");
        text.ShouldNotContain("Id = source.Id");
    }

    // Dropping an expression that is not a constant could drop a side effect.
    [Fact]
    public void Rewrite_MemberListParameter_ReportsManual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } } public class B { public int Id { get; set; } } public class P : AutoMapper.Profile { public P(AutoMapper.MemberList list) { CreateMap<A, B>(list); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("is not a CreateMap<A, B>() without options");
    }

    // AutoMapper 14's built-in conversions as measured (TAAK-247); the proofs in CreateMap/conversion-* show the common ones.
    [Theory]
    [InlineData("System.DateTime", "System.DateTimeOffset", "X = source.X,")]
    [InlineData("System.DateTime", "System.DateTimeOffset?", "X = source.X,")]
    [InlineData("int", "char", "X = Convert.ToChar(source.X),")]
    [InlineData("char", "int", "X = source.X,")]
    [InlineData("int", "long?", "X = source.X,")]
    [InlineData("int?", "long?", "X = source.X,")]
    [InlineData("E", "int?", "X = (int?)source.X,")]
    [InlineData("E?", "int", "X = (int)source.X.GetValueOrDefault(),")]
    [InlineData("E?", "F?", "X = (F?)source.X,")]
    [InlineData("int?", "E?", "X = (E?)source.X,")]
    [InlineData("object", "string", "X = source.X?.ToString(),")]
    [InlineData("string", "float", "X = Convert.ToSingle(source.X),")]
    [InlineData("System.Collections.Generic.List<int>", "System.Collections.Generic.List<int?>", "X = source.X?.Select(x => (int?)x).ToList() ?? [],")]
    [InlineData("System.Collections.Generic.List<E>", "System.Collections.Generic.List<int?>", "X = source.X?.Select(x => (int?)x).ToList() ?? [],")]
    [InlineData("System.Collections.Generic.List<string>", "System.Collections.Generic.List<E>", "X = source.X?.Select(x => string.IsNullOrEmpty(x) ? default : Enum.Parse<E>(x, true)).ToList() ?? [],")]
    public void Rewrite_BuiltInConversion_WritesIt(string from, string to, string assignment)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            $"public enum E {{ A, B }} public enum F {{ A, B }} public class A {{ public {from} X {{ get; set; }} }} public class B {{ public {to} X {{ get; set; }} }} public class P : AutoMapper.Profile {{ public P() {{ CreateMap<A, B>(); }} }}");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString().ShouldContain(assignment);
    }

    [Theory]
    [InlineData("public enum E { Red = 0, Rouge = 0 } public enum F { Rouge = 1, Red = 0 }", "E", "F", "has several names for the value 0 that F maps differently")]
    [InlineData("public enum E { Red } public enum F { Red = 1, RED = 2 }", "E", "F", "F has several members named Red ignoring case")]
    [InlineData("public enum E { A } public enum F : byte { A }", "E", "F", "have different underlying types")]
    [InlineData("public enum E { A } public enum F { A }", "E", "byte", "AutoMapper 14 throws at run time for E to byte")]
    [InlineData("public enum E { A } public enum F { A }", "double", "char", "throws InvalidCastException at run time")]
    [InlineData("public enum E { A } public enum F { A }", "System.DateTimeOffset", "System.DateTime", "AutoMapper 14 throws at run time")]
    [InlineData("public enum E { A } public enum F { A }", "System.DateTime?", "System.DateTimeOffset", "AutoMapper gives null the default System.DateTimeOffset")]
    [InlineData("public enum E { A, B } public enum F { B, A }", "E?", "F?", "maps any other value by name")]
    [InlineData("public enum E { A } public enum F { A }", "string", "char", "a Parse call throws on null")]
    [InlineData("public enum E { A } public enum F { A }", "string", "E?", "AutoMapper keeps null as null")]
    [InlineData("public enum E { A } public enum F { A }", "int", "System.Guid", "(AutoMapper converts)")]
    [InlineData("public enum E { A } public enum F { [System.Runtime.Serialization.EnumMember(Value = \"a\")] A }", "string", "F", "also accepts the [EnumMember] values of F")]
    [InlineData("public enum E { A } public enum F { [System.Runtime.Serialization.EnumMember(Value = \"a\")] A }", "E", "F", "has [EnumMember] names")]
    public void Rewrite_BuiltInConversionNotWritable_ReportsManual(string enums, string from, string to, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            $"{enums} public class A {{ public {from} X {{ get; set; }} }} public class B {{ public {to} X {{ get; set; }} }} public class P : AutoMapper.Profile {{ public P() {{ CreateMap<A, B>(); }} }}");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
    }

    // The switch and the null-or-empty check read the value twice: a MapFrom expression that may have a side effect stays Manual.
    [Theory]
    [InlineData("public string Read() => \"\";", "string", "E", "s => s.Read()", "the string would be evaluated twice")]
    [InlineData("public E Read() => E.A;", "E", "F", "s => s.Read()", "would be evaluated twice in the switch")]
    [InlineData("public E Read() => E.A;", "E", "string", "s => s.Read()", "X = source.Read().ToString(),")]
    public void Rewrite_ConversionOfMapFromCall_OnlyWhenReadOnce(string method, string from, string to, string mapFrom, string expected)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            $"public enum E {{ A, B }} public enum F {{ B, A }} public class A {{ {method} public {from} Y {{ get; set; }} }} public class B {{ public {to} X {{ get; set; }} }} public class P : AutoMapper.Profile {{ public P() {{ CreateMap<A, B>().ForMember(d => d.X, o => o.MapFrom({mapFrom})); }} }}");
        var pattern = new ForMemberMapFromPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string text = result switch
        {
            FixedResult fixedResult => fixedResult.NewNodes[1].ToFullString(),
            ManualResult manual => manual.Reason,
            _ => throw new System.Diagnostics.UnreachableException(),
        };
        text.ShouldContain(expected);
    }

    // AllowNullDestinationValues changes what AutoMapper writes for a null value; a conversion written for the default would differ.
    [Fact]
    public void Rewrite_ConversionWithNullSetting_ReportsManual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("""
            using AutoMapper;
            public class A { public int? X { get; set; } }
            public class B { public string X { get; set; } }
            public class P : Profile { public P() { CreateMap<A, B>(); } }
            public static class Setup { public static MapperConfiguration Create() => new(cfg => { cfg.AllowNullDestinationValues = false; cfg.AddProfile<P>(); }); }
            """);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("the null handling that AllowNullDestinationValues at");
    }

    // ToString() can return null and AutoMapper writes null into a non-nullable string too: the ! keeps the warning away, as for nested maps.
    [Fact]
    public void Rewrite_ConversionToNonNullableString_SuppressesTheWarning()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("""
            #nullable enable
            public class A { public int? X { get; set; } public object? Y { get; set; } public int? Z { get; set; } }
            public class B { public string X { get; set; } = ""; public string Y { get; set; } = ""; public string? Z { get; set; } }
            public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }
            """);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string extensions = result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString();
        extensions.ShouldContain("X = source.X?.ToString()!,");
        extensions.ShouldContain("Y = source.Y?.ToString()!,");
        extensions.ShouldContain("Z = source.Z?.ToString(),");
    }

    // What AutoMapper 14's ReverseMap does beyond plain assignments (measured, TAAK-249) stays Manual.
    [Theory]
    [InlineData("public class A { public int Id { get; set; } } public class B { public A Inner { get; set; } }",
        ".ForMember(d => d.Inner, o => o.MapFrom(s => s))", "maps the whole A")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public long Big { get; set; } }",
        ".ForMember(d => d.Big, o => o.MapFrom(s => (long)s.Id))", "casts")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int X { get; set; } public int Y { get; set; } }",
        ".ForMember(d => d.X, o => o.MapFrom(s => s.Id)).ForMember(d => d.Y, o => o.MapFrom(s => s.Id))", "both read Id")]
    [InlineData("public class A { public int Id { get; } = 1; } public class B { public int Code { get; set; } }",
        ".ForMember(d => d.Code, o => o.MapFrom(s => s.Id))", "cannot be written")]
    [InlineData("public class C { public string Name { get; set; } } public class A { public int Id { get; set; } public C Customer { get; set; } } public class B { public int Id { get; set; } public string CustomerName { get; } }",
        "", "may be unflattened into A.Customer")]
    [InlineData("public class A { public int Id { get; set; } public string Code { get; set; } } public class B { public int Id { get; set; } public string code() => \"\"; }",
        "", "A.Code has no member with the same name in B")]
    public void Rewrite_ReverseMapAutoMapperDoesMoreFor_ReportsManual(string types, string forward, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(types + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>()" + forward + ".ReverseMap(); } }");
        CreateMapPattern pattern = forward.Contains("MapFrom", StringComparison.Ordinal) ? new ForMemberMapFromPattern()
            : forward.Contains("Ignore", StringComparison.Ordinal) ? new ForMemberIgnorePattern() : new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("ReverseMap of CreateMap<A, B>: ");
        ((ManualResult)result).Reason.ShouldContain(reason);
    }

    // Measured (TAAK-249): a ReverseMap().ForMember option wins over the reversed MapFrom of the same member.
    [Fact]
    public void Rewrite_ReverseOptionAndReversedMapFrom_OptionWins()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } public string Note { get; set; } public string Other { get; set; } } public class B { public int Id { get; set; } public string Note { get; set; } }"
            + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>().ForMember(d => d.Note, o => o.MapFrom(s => s.Other)).ReverseMap().ForMember(d => d.Other, o => o.Ignore()); } }");
        var pattern = new ForMemberMapFromPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string extensions = result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString().ReplaceLineEndings("\n");
        extensions.ShouldContain("public static A ToA(this B source) => new()\n    {\n        Id = source.Id,\n        Note = source.Note,\n    };");
    }

    // TAAK-253: a converter that needs what a ToB method does not have (the destination, the ResolutionContext, a service provider) stays Manual.
    [Theory]
    [InlineData("CreateMap<A, B>().ConvertUsing((s, d) => d ?? new B());", "", "ConvertUsing reads the destination")]
    [InlineData("CreateMap<A, B>().ConvertUsing((s, d, c) => c.Mapper.Map<B>(s));", "", "ConvertUsing uses the ResolutionContext")]
    [InlineData("CreateMap<A, B>().ConvertUsing((s, d) => { return new B(); });", "", "a lambda with a block body")]
    [InlineData("CreateMap<A, B>().ConvertUsing<C>();", "public class C : ITypeConverter<A, B> { public B Convert(A s, B d, ResolutionContext c) => c.Mapper.Map<B>(s); }", "C.Convert uses the ResolutionContext")]
    [InlineData("CreateMap<A, B>().ConvertUsing<C>();", "public class C : ITypeConverter<A, B> { public B Convert(A s, B d, ResolutionContext c) => d; }", "C.Convert reads the destination")]
    [InlineData("CreateMap<A, B>().ConvertUsing<C>();", "public class C : ITypeConverter<A, B> { B ITypeConverter<A, B>.Convert(A s, B d, ResolutionContext c) => new B(); }", "implements ITypeConverter explicitly")]
    [InlineData("CreateMap<A, B>().ConvertUsing(new C(1));", "public class C : ITypeConverter<A, B> { public C(int x) { } public B Convert(A s, B d, ResolutionContext c) => new B(); }", "is not new C() or a static field")]
    [InlineData("CreateMap<A, string>().ConvertUsing(s => s.Name);", "", "A already has a member ToString")]
    [InlineData("CreateMap<A, B>().ForMember(d => d.Name, o => o.ConvertUsing(new V())).ConvertUsing(s => new B());", "public class V : IValueConverter<string, string> { public string Convert(string s, ResolutionContext c) => s; }", "is not a CreateMap<A, B>() without options")]
    [InlineData("CreateMap<A, B>().ForMember(d => d.Name, o => o.ConvertUsing(new V(), \"Name\"));", "public class V : IValueConverter<string, string> { public string Convert(string s, ResolutionContext c) => s; }", "names its source member with a string")]
    [InlineData("CreateMap<A, B>().ForMember(d => d.Name, o => o.ConvertUsing(new V()));", "public class V : IValueConverter<string, string> { public string Convert(string s, ResolutionContext c) => c.Mapper.Map<string>(s); }", "V.Convert uses the ResolutionContext")]
    public void Rewrite_ConvertUsingNotWritable_ReportsManual(string map, string converter, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile($$"""
            using AutoMapper;
            public class A { public string Name { get; set; } }
            public class B { public string Name { get; set; } }
            {{converter}}
            public class P : Profile { public P() { {{map}} } }
            """);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
    }

    // Several Convert overloads would make Convert(source, null, null) ambiguous: a typed default picks the implementation of the map's pair.
    [Fact]
    public void Rewrite_ConverterWithSeveralOverloads_PassesATypedDestination()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("""
            using AutoMapper;
            public class A { } public class B { } public class D { }
            public class C : ITypeConverter<A, B>, ITypeConverter<A, D>
            {
                public B Convert(A s, B d, ResolutionContext c) => new B();
                public D Convert(A s, D d, ResolutionContext c) => new D();
            }
            public class P : Profile { public P() { CreateMap<A, B>().ConvertUsing<C>(); } }
            """);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString().ShouldContain("public static B ToB(this A source) => new C().Convert(source, default(B), null);");
    }

    // AutoMapper calls a converter with a null member too (measured, TAAK-253): no ?. but a ! for a maybe-null member, null! for unread parameters.
    [Fact]
    public void Rewrite_ConverterInNullableContext_CallsItWithoutNullCheck()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("""
            #nullable enable
            using AutoMapper;
            public class Tag { public string V { get; set; } = ""; }
            public class TagDto { public string V { get; set; } = ""; }
            public class A { public Tag? Tag { get; set; } public Tag Main { get; set; } = new(); }
            public class B { public TagDto? Tag { get; set; } public TagDto Main { get; set; } = new(); }
            public class C : ITypeConverter<Tag?, TagDto> { public TagDto Convert(Tag? s, TagDto d, ResolutionContext c) => new() { V = s?.V ?? "" }; }
            public class P : Profile { public P() { CreateMap<Tag, TagDto>().ConvertUsing<C>(); CreateMap<A, B>(); } }
            """);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string extensions = result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString();
        extensions.ShouldContain("public static TagDto ToTagDto(this Tag source) => new C().Convert(source, null!, null!);");
        extensions.ShouldContain("Tag = source.Tag!.ToTagDto(),");
        extensions.ShouldContain("Main = source.Main.ToTagDto(),");
    }

    // Member options the rewrite cannot reproduce exactly (TAAK-255) stay Manual with their own reason.
    [Theory]
    [InlineData("public class A { public int Id { get; set; } public string Name { get; set; } } public class B { public B(int id) { } public string Name { get; set; } }",
        ".ForMember(d => d.Name, o => o.Condition(s => s.Id > 0))", "is created through its constructor")]
    [InlineData("public class C { public string Name { get; set; } } public class A { public C Inner { get; set; } public string Name { get; set; } } public class B { public C Inner { get; set; } }",
        ".ForPath(d => d.Inner.Name, o => o.MapFrom(s => s.Name))", "which AutoMapper also maps itself")]
    [InlineData("public struct C { public string Name { get; set; } } public class A { public string Name { get; set; } } public class B { public C Inner { get; set; } }",
        ".ForPath(d => d.Inner.Name, o => o.MapFrom(s => s.Name))", "is not a class with a public parameterless constructor")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; set; } }",
        ".ForMember(d => d.Id, o => o.Condition(s => s.Id > 0)).ReverseMap()", "not migrated together with ReverseMap")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; set; } }",
        ".ForMember(d => d.Id, o => o.UseDestinationValue())", "UseDestinationValue for Id is not migrated")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; set; } }",
        ".ForMember(d => d.Id, o => o.Condition((s, d, sm, dm) => sm > dm))", "Condition for Id is not a lambda of the source alone")]
    public void Rewrite_MemberOptionAutoMapperDoesMoreFor_ReportsManual(string types, string options, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(types + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>()" + options + "; } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
    }

    // A conditional MapFrom value is grouped, so cond ? (a ? b : c) : default keeps its meaning.
    [Fact]
    public void Rewrite_ConditionOnConditionalValue_GroupsTheValue()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("public class A { public bool Flag { get; set; } } public class B { public int X { get; set; } }"
            + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>().ForMember(d => d.X, o => { o.Condition(s => s.Flag); o.MapFrom(s => s.Flag ? 1 : 2); }); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString().ShouldContain("X = source.Flag ? (source.Flag ? 1 : 2) : default,");
    }

    private static (SyntaxNode Root, SemanticModel Model) Compile(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return (tree.GetRoot(), compilation.GetSemanticModel(tree));
    }
}
