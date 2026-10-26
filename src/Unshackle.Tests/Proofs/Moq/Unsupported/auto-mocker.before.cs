using Moq.AutoMock;

namespace Moq.AutoMock
{
    public class AutoMocker
    {
    }
}

namespace Proofs
{
    public class UnsupportedProof
    {
        public void Test()
        {
            var mocker = new AutoMocker();
        }
    }
}
