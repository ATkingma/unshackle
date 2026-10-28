using Xunit;

namespace Tests
{
    public class SampleAttribute : FactAttribute
    {
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
