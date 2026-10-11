using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Unshackle.Tests.Helpers;

// Loads a source generator (e.g. Mediator's) from the test output, so proofs can run it like the compiler would.
public class AnalyzerLoader : IAnalyzerAssemblyLoader
{
    public void AddDependencyLocation(string fullPath)
    {
    }

    public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
}
