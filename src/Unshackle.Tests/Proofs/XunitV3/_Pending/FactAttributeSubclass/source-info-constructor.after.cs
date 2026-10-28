using System.Runtime.CompilerServices;
using Xunit;

namespace Tests
{
    public class SampleAttribute : FactAttribute
    {
        public SampleAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
        }

        public string Description { get; set; }
    }

    public class SampleTests
    {
        [Sample(Description = "a sample")]
        public void Runs()
        {
        }
    }
}
