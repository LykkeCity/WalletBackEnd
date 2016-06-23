using NUnit.Framework;

namespace Lykkex.WalletBackend.Tests
{
    public class GenerateIssuerOutputsModel : GenerateMassOutputsModel
    {
        public string AssetName
        {
            get;
            set;
        }
    }

    [TestFixture]
    public class GenerateIssuerOutputsTests : TaskTestsCommon
    {
        
    }
}
