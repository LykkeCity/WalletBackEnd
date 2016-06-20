using NBitcoin;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;

namespace Lykkex.WalletBackend.Tests.GenerateIssuerOutputs
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
