using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.EPPlus;

// Real assemblies for the EPPlus proofs (council round 8, E2): "before" is EPPlus 4.5.3.3 (LGPL, the last free version), "after" ClosedXML 0.105.1 (MIT).
public static class EPPlusReferences
{
    // E2: the test types are 4.5.3.3 only, but EPPlus 5+ code must still compile in a proof. The 5+ licence members (LicenseContext,
    // ExcelPackage.LicenseContext, ExcelPackage.License) are added here as C# 14 static extension members with the same names and
    // shapes; the patterns match them by syntax, as they do for real 5+ code whose symbols do not resolve against 4.5.3.3.
    private static readonly byte[] s_epplus5Surface = CompileEpplus5Surface();

    public static IReadOnlyList<MetadataReference> EPPlus { get; } =
    [
        .. TestReferences.Net,
        MetadataReference.CreateFromFile(typeof(global::OfficeOpenXml.ExcelPackage).Assembly.Location),
        MetadataReference.CreateFromImage(s_epplus5Surface),
    ];

    public static IReadOnlyList<MetadataReference> ClosedXml { get; } =
    [
        .. TestReferences.Net,
        MetadataReference.CreateFromFile(typeof(global::ClosedXML.Excel.XLWorkbook).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::DocumentFormat.OpenXml.Packaging.SpreadsheetDocument).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::DocumentFormat.OpenXml.OpenXmlElement).Assembly.Location),
    ];

    // The harness runs "before" snippets, which bind to the surface assembly by name: resolve it from the compiled image.
    static EPPlusReferences()
    {
        System.Reflection.Assembly? surface = null;
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            new System.Reflection.AssemblyName(args.Name).Name == "EPPlus5Surface"
                ? surface ??= System.Reflection.Assembly.Load(s_epplus5Surface)
                : null;
    }

    private static byte[] CompileEpplus5Surface()
    {
        const string source = """
            namespace OfficeOpenXml
            {
                public enum LicenseContext { Commercial = 0, NonCommercial = 1 }

                public class EPPlusLicense
                {
                    public void SetNonCommercialPersonal(string name) { }
                    public void SetNonCommercialOrganization(string name) { }
                    public void SetCommercial(string key) { }
                }

                public static class EPPlus5Surface
                {
                    extension(ExcelPackage)
                    {
                        public static LicenseContext? LicenseContext { get => null; set { } }
                        public static EPPlusLicense License => new EPPlusLicense();
                    }
                }
            }
            """;
        var compilation = CSharpCompilation.Create(
            "EPPlus5Surface",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            [.. TestReferences.Net, MetadataReference.CreateFromFile(typeof(global::OfficeOpenXml.ExcelPackage).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException("EPPlus5Surface does not compile: " + string.Join("; ", result.Diagnostics));
        }

        return stream.ToArray();
    }
}
