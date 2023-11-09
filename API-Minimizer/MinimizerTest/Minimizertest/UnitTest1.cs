using NUnit.Framework;
using MinimizerCommon.Commons;

namespace Minimizertest
{
    public class Tests
    {
        private LifeCheck _lifecheck;
        [SetUp]
        public void Setup()
        {
            _lifecheck = new LifeCheck("BackApi", true);
        }

        [Test]
        public void Test1()
        {
            Assert.IsNotNull(_lifecheck);
        }

        [Test]
        public void Test2()
        {
            if (_lifecheck.Status is false)
                Assert.Fail();
        }

        [Test]
        public void Test3()
        {
            if (string.IsNullOrEmpty(_lifecheck.Name))
                Assert.Fail();
        }
    }
}