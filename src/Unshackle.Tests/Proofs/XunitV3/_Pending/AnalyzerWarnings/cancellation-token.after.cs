using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class Validator
    {
        public Task<bool> ValidateAsync(string value, CancellationToken cancellationToken = default) => Task.FromResult(value.Length > 0);
    }

    public class ValidatorTests
    {
        [Fact]
        public async Task Validates()
        {
            bool valid = await new Validator().ValidateAsync("x", TestContext.Current.CancellationToken);
            Assert.True(valid);
        }
    }
}
