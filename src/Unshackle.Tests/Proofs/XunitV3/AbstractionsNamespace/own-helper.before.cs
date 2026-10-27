using System.Collections.Generic;
using Xunit.Abstractions;

public class ListOutput : ITestOutputHelper
{
    public List<string> Lines { get; } = new List<string>();

    public void WriteLine(string message) => Lines.Add(message);

    public void WriteLine(string format, params object[] args) => Lines.Add(string.Format(format, args));
}
