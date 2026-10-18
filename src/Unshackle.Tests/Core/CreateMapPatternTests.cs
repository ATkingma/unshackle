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
    [InlineData("public class A { public int Other { get; set; } } public class B { public int Id { get; set; } } public static class T { public static void M(AutoMapper.MapperConfiguration c) => c.AssertConfigurationIsValid(); }", "the solution calls AssertConfigurationIsValid")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; private set; } }", "has a non-public setter")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; protected internal set; } }", "has a non-public setter")]
    [InlineData("public class A { public System.Collections.Generic.List<int> Id { get; set; } = new(); } public class B { public System.Collections.Generic.IReadOnlyList<int> Id { get; } = new System.Collections.Generic.List<int>(); }", "is a get-only collection")]
    [InlineData("public class A { public int Id { get; set; } } public abstract class B { public int Id { get; set; } }", "no public parameterless constructor")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int[] Id { get; set; } = []; }", "(AutoMapper converts)")]
    [InlineData("public class A { public int Id { get; set; } } public class B { private B() { } private B(int id) { } }", "which is private or protected")]
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

    // Measured (TAAK-261): AutoMapper leaves a destination member without a source alone (its initializer or default stays), and a
    // get-only member that is not a collection; without AssertConfigurationIsValid in the code the method leaves them out too.
    [Theory]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; set; } public string Other { get; set; } = \"x\"; }", "Other")]
    [InlineData("public struct S { public int X { get; set; } } public class A { public int Id { get; set; } public S? P { get; set; } } public class B { public int Id { get; set; } public int PX { get; set; } }", "PX")]
    [InlineData("public class A { public int Id { get; set; } public int[] Codes { get; set; } = []; } public class B { public int Id { get; set; } public int[] Codes { get; } = []; }", "Codes")]
    [InlineData("public class A { public int Id { get; set; } public string Name { get; set; } = \"\"; } public class B { public int Id { get; set; } public string Name { get; } = \"\"; }", "Name")]
    [InlineData("public class A { public int Id { get; set; } public int Total { get; set; } } public class B { public int Id { get; set; } public int Total => 0; }", "Total")]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Id { get; set; } public System.Collections.Generic.List<int> Items { get; } = new(); }", "Items")]
    public void Rewrite_MemberAutoMapperDoesNotWrite_LeftOut(string types, string member)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(types + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string written = result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString();
        written.ShouldContain("Id = source.Id");
        written.ShouldNotContain(member + " =");
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

    // AutoMapper 14's other name rules as measured (TAAK-256): any case, a Get prefix (also on a property), flattening with the shortest
    // prefix first and a direct match winning; the proofs in CreateMap/flattening-* show the common ones.
    [Theory]
    [InlineData("public class A { public int id { get; set; } } public class B { public int Id { get; set; } }", "Id = source.id,")]
    [InlineData("public class A { public string GetCode { get; set; } = \"\"; } public class B { public string Code { get; set; } = \"\"; }", "Code = source.GetCode,")]
    [InlineData("public class A { public int Total; public int GetTotal() => 2; } public class B { public int Total { get; set; } }", "Total = source.Total,")]
    [InlineData("public class A { public string Name { get; set; } = \"\"; } public class B { public int NameLength { get; set; } }", "NameLength = source.Name?.Length ?? default,")]
    [InlineData("public class A { public string Name { get; set; } = \"\"; } public class B { public string NameTrim { get; set; } = \"\"; }", "NameTrim = source.Name?.Trim(),")]
    [InlineData("public class C { public string X { get; set; } = \"\"; } public class A { public C Customer { get; set; } = new(); public string CustomerName { get; set; } = \"\"; } public class B { public string CustomerName { get; set; } = \"\"; }", "CustomerName = source.CustomerName,")]
    [InlineData("public class N { public string X { get; set; } = \"\"; } public class A { public string Customer { get; set; } = \"\"; public N CustomerName2 { get; set; } = new(); } public class B { public string CustomerName2X { get; set; } = \"\"; }", "CustomerName2X = source.CustomerName2?.X,")]
    [InlineData("public class C { public int? When { get; set; } } public class A { public C Customer { get; set; } = new(); } public class B { public int CustomerWhen { get; set; } }", "CustomerWhen = (source.Customer?.When).GetValueOrDefault(),")]
    public void Rewrite_OtherNameRule_ReadsWhatAutoMapperReads(string types, string assignment)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(types + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString().ShouldContain(assignment);
    }

    [Theory]
    [InlineData("public class A { public int Name() => 1; public int GetName() => 2; public int name; } public class B { public int Name { get; set; } }", "Name and name of A both match Name")]
    [InlineData("public class C { public int Age { get; set; } } public class A { public C Customer { get; set; } = new(); } public class B { public long? CustomerAge { get; set; } }", "may meet a null object")]
    public void Rewrite_OtherNameRuleUnsure_ReportsManual(string types, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(types + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
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

    // TAAK-273: every type has ToString(), so a map to string gets ToMappedString, which a call does bind.
    [Fact]
    public void Rewrite_ConvertUsingToString_WritesToMappedString()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile("""
            using AutoMapper;
            public class A { public string Name { get; set; } }
            public class P : Profile { public P() { CreateMap<A, string>().ConvertUsing(s => s.Name); } }
            """);
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString().ShouldContain("public static string ToMappedString(this A source) => source.Name;");
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
        ".ForPath(d => d.Id, o => o.MapFrom(s => s.Id)).ReverseMap()", "ReverseMap reverses the ForPath")]
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

    // Condition and ForPath on a ReverseMap (TAAK-264): the reverse map takes them like any map, but a ForPath of the forward map is reversed by AutoMapper
    // (measured), so a forward ForPath next to ReverseMap, a ForPath into a member the reversed MapFrom fills, and an initializer a Condition would keep are Manual.
    [Theory]
    [InlineData("public class A { public int Id { get; set; } } public class B { public int Code { get; set; } }",
        ".ForPath(d => d.Code, o => o.MapFrom(s => s.Id)).ReverseMap()", "ReverseMap reverses the ForPath of the forward map")]
    [InlineData("public class A { public int Id { get; set; } public bool Flag { get; set; } } public class B { public int Id { get; set; } public bool Flag { get; set; } }",
        ".ReverseMap().ForMember(d => d.Id, o => o.Condition((s, d) => d.Id == 0))", "Condition for Id is not a lambda of the source alone")]
    [InlineData("public class A { public int Id { get; set; } public string Name { get; set; } = \"x\"; } public class B { public int Id { get; set; } public string Name { get; set; } = \"\"; public bool Flag { get; set; } }",
        ".ReverseMap().ForMember(d => d.Name, o => o.Condition(s => s.Flag))", "A.Name has an initializer")]
    [InlineData("public class H { public string V { get; set; } } public class A { public H X { get; set; } } public class B { public H Y { get; set; } }",
        ".ForMember(d => d.Y, o => o.MapFrom(s => s.X)).ReverseMap().ForPath(d => d.X.V, o => o.MapFrom(s => s.Y.V))", "ForPath writes into X, which the reversed MapFrom of the forward map also fills")]
    public void Rewrite_ConditionOrForPathOnReverseMapAutoMapperDoesMoreFor_ReportsManual(string types, string chain, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(types + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>()" + chain + "; } }");
        CreateMapPattern pattern = chain.Contains("ForMember(", StringComparison.Ordinal) && chain.Contains("MapFrom", StringComparison.Ordinal) ? new ForMemberMapFromPattern() : new CreateMapPattern();

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

    // Measured (TAAK-266): AutoMapper takes the constructor with the most parameters it can resolve (also over a parameterless one, any
    // accessibility), the first declared on a tie; a parameter counts as resolvable with a same-named source member (any case) or a default.
    [Theory]
    [InlineData("public B() { } public B(int id) { Id = id; }", "ToB(this A source) => new(source.Id)")]
    [InlineData("public B(int id) { Id = id; } public B(int id, string name) { Id = id; Name = name; }", "ToB(this A source) => new(source.Id, source.Name)")]
    [InlineData("public B(int id, string other) { Id = id; } public B(int id) { Id = id; }", "ToB(this A source) => new(source.Id)")]
    [InlineData("public B(string name) { Name = name; } public B(int id) { Id = id; }", "ToB(this A source) => new(source.Name)")]
    [InlineData("public B(int id) { Id = id; } internal B(int id, string name) { Id = id; Name = name; }", "ToB(this A source) => new(source.Id, source.Name)")]
    [InlineData("public B(int id, string other = \"\") { Id = id; } public B(int id) { Id = id; }", "ToB(this A source) => new(source.Id)")]
    public void Rewrite_SeveralConstructors_TakesTheOneAutoMapperPicks(string constructors, string expected)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } public string Name { get; set; } = \"\"; } public class B { " + constructors + " public int Id { get; } public string Name { get; } = \"\"; }"
            + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert (the last is the optional-parameter tie: more parameters win, which stays Manual below)
        if (constructors.Contains("other = ", StringComparison.Ordinal))
        {
            result.ShouldBeOfType<ManualResult>();
            return;
        }

        result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString().ShouldContain(expected);
    }

    [Theory]
    [InlineData("public B(int id) { Id = id; } private B(int id, string name) { Id = id; Name = name; }", "which is private or protected")]
    [InlineData("public B(int id) { Id = id; } public B(int id, string nameLength) { Id = id; }", "by flattening or a Get method")]
    [InlineData("public B(string x) { } public B(string y, int z) { }", "can resolve no constructor")]
    public void Rewrite_SeveralConstructorsNotDecided_ReportsManual(string constructors, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } public string Name { get; set; } = \"\"; } public class B { " + constructors + " public int Id { get; } public string Name { get; } = \"\"; }"
            + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>(); } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
    }

    // Measured (TAAK-266): ConstructUsing makes the destination, then the members are mapped as usual (a settable one is overwritten).
    [Theory]
    [InlineData(".ConstructUsing(s => new B(s.Id * 10))", "ToB(this A source) => new B(source.Id * 10)\n")]
    [InlineData(".ConstructUsing((s) => new B(s.Id * 10))", "ToB(this A source) => new B(source.Id * 10)\n")]
    [InlineData(".ConstructUsing(s => new B(s.Id) { Name = \"x\" })", "B destination = new B(source.Id) { Name = \"x\" };")]
    [InlineData(".ConstructUsing(s => Make(s.Id))", "B destination = Make(source.Id);")]
    public void Rewrite_ConstructUsing_UsesTheExpressionAsCreation(string chain, string expected)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } public string Name { get; set; } = \"\"; } public class B { public B(int id) { } public int Id { get; set; } public string Name { get; set; } = \"\"; public static B Make(int id) => new(id); }"
            + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>()" + chain.Replace("Make(", "B.Make(", StringComparison.Ordinal) + "; } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string text = result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString();
        text.ShouldContain(expected.Replace("B.Make", "Make", StringComparison.Ordinal).Replace("Make(", "B.Make(", StringComparison.Ordinal).Replace("\n", "\n", StringComparison.Ordinal));
        text.ShouldContain("Id = source.Id");
        text.ShouldContain("Name = source.Name");
    }

    [Theory]
    [InlineData(".ConstructUsing((s, ctx) => new B(s.Id))", "ResolutionContext")]
    [InlineData(".ConstructUsing(s => new B(scale))", "not available outside the profile")]
    [InlineData(".ConstructUsing(s => new B(s.Id)).ReverseMap().ConstructUsing(d => new A())", "ConstructUsing after ReverseMap")]
    public void Rewrite_ConstructUsingAutoMapperDoesMoreFor_ReportsManual(string chain, string reason)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } } public class B { public B(int id) { } public int Id { get; set; } }"
            + " public class P : AutoMapper.Profile { public P(int scale) { CreateMap<A, B>()" + chain + "; } }");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain(reason);
    }

    // Measured (TAAK-266): MaxDepth(n) from 2 up changes nothing on an acyclic nested copy; 1 cuts a collection member off.
    [Theory]
    [InlineData(64, true)]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public void Rewrite_MaxDepthOnNestedCopy_DroppedFromTwoUp(int depth, bool fixedResult)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class I { public int V { get; set; } } public class ID { public int V { get; set; } } public class A { public I Inner { get; set; } = new(); } public class B { public ID Inner { get; set; } = new(); }"
            + $" public class P : AutoMapper.Profile {{ public P() {{ CreateMap<A, B>().MaxDepth({depth}); CreateMap<I, ID>().MaxDepth({depth}); }} }}");
        var pattern = new CreateMapPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        if (fixedResult)
        {
            result.ShouldBeOfType<FixedResult>();
        }
        else
        {
            result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("MaxDepth is only dropped from 2 up");
        }
    }

    // Measured (TAAK-266): a ForMember on the member of a parameter's name does not make that parameter resolvable, so the shorter constructor is taken.
    [Fact]
    public void Rewrite_ForMemberOnUnresolvedParameter_TakesTheShorterConstructor()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public class A { public int Id { get; set; } public string Title { get; set; } = \"\"; } public class B { public B(int id) { Id = id; } public B(int id, string name) { Id = id; Name = name; } public int Id { get; } public string Name { get; set; } = \"\"; }"
            + " public class P : AutoMapper.Profile { public P() { CreateMap<A, B>().ForMember(d => d.Name, o => o.MapFrom(s => s.Title)); } }");
        var pattern = new ForMemberMapFromPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        string text = result.ShouldBeOfType<FixedResult>().NewNodes[1].ToFullString();
        text.ShouldContain("new(source.Id)");
        text.ShouldContain("Name = source.Title,");
    }

    // The common DI shape (dasblog): the maps sit in the one constructor that takes a service.
    [Fact]
    public void Rewrite_ProfileMapsInConstructorWithService_NamesTheService()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Compile(
            "public interface IS { string Url(int id); } public class A { public int Id { get; set; } } public class B { public string Url { get; set; } = \"\"; }"
            + " public class P : AutoMapper.Profile { private readonly IS _s; public P() { } public P(IS s) { _s = s; CreateMap<A, B>().ForMember(d => d.Url, o => o.MapFrom(a => _s.Url(a.Id))); } }");
        var pattern = new ForMemberMapFromPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).ShouldHaveSingleItem(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("constructor (IS s) and read _s, s");
    }

    private static (SyntaxNode Root, SemanticModel Model) Compile(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test", [tree], TestReferences.AutoMapper, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return (tree.GetRoot(), compilation.GetSemanticModel(tree));
    }
}
