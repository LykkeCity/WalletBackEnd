using System;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCommon
{
    public interface IInter1
    {
        
    }

    public class Cl1 : IInter1
    {
        
    }

    public interface IInter2
    {
        
    }

    public class Cl2 : IInter2
    {
        
    }

    public class Cl3 : IInter2
    {
        
    }



    public class Srv
    {
        public IInter1 Param1 { get; set; }
        public IInter2[] Param2 { get; set; }

        public Srv(IInter1 param1, params IInter2[] param2)
        {
            Param1 = param1;
            Param2 = param2;
        }
    }

    [TestClass]
    public class IoCTest
    {
        [TestMethod]
        public void TestArrayParams()
        {
            var ioC = new IoC();

            ioC.Register<IInter1,Cl1>();
            ioC.Register<IInter2, Cl2>();
            ioC.Register<IInter2, Cl3>();

            ioC.Register<Srv>();


            var srv = ioC.GetObject<Srv>();

            Assert.IsTrue(srv.Param1 != null);
            Assert.IsTrue(srv.Param1 is Cl1);

            Assert.AreEqual(2, srv.Param2.Length);




        }
    }
}
