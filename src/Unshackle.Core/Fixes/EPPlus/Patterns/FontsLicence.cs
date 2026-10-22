using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// Council round 10, E7: ClosedXML 0.105.x depends on SixLabors.Fonts [1.0.0, 3.0.0). 1.x is Apache-2.0; 2.x is under the Six Labors Split
// License. Every migrated package gets this warning. The pin itself (a PackageAddEdit) needs a current assets file, which Core cannot read yet.
internal static class FontsLicence
{
    public const string Package = "SixLabors.Fonts";
    public const string PinVersion = "1.0.0";

    public const string Warning = "ClosedXML depends on SixLabors.Fonts [1.0.0, 3.0.0); 2.x is under the Six Labors Split License (paid for some "
        + "commercial use). Pin it below 2.0 with <PackageReference Include=\"SixLabors.Fonts\" Version=\"1.0.0\" /> unless you have a licence.";

    // The SixLabors.Fonts version the project's compilation references (restored from its assets file), or null when none.
    public static Version? Resolved(SemanticModel model) =>
        model.Compilation.ReferencedAssemblyNames.FirstOrDefault(a => a.Name == "SixLabors.Fonts")?.Version;

    // The same from the project's references (the restored assets), read from the assembly's own metadata.
    public static Version? Resolved(Project project) =>
        project.MetadataReferences.OfType<PortableExecutableReference>()
            .Select(Identity)
            .FirstOrDefault(a => a?.Name == "SixLabors.Fonts")?.Version;

    private static (string Name, Version Version)? Identity(PortableExecutableReference reference)
    {
        try
        {
            if (reference.GetMetadata() is not AssemblyMetadata { } metadata)
            {
                return null;
            }

            System.Reflection.Metadata.MetadataReader reader = metadata.GetModules()[0].GetMetadataReader();
            System.Reflection.Metadata.AssemblyDefinition definition = reader.GetAssemblyDefinition();
            return (reader.GetString(definition.Name), definition.Version);
        }
        catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
