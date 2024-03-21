using NUnit.Framework;
using MinimizerCommon.Commons;
using API_Minimizer_back.Controllers;
/// <summary>
/// Contains unit tests for the <see cref="LifeCheck"/> class.
/// </summary>
namespace Minimizertest
{
    public class Tests
    {
        private LifeCheck _lifecheck;
        /// <summary>
        /// This method is called before each test method is executed to set up the test environment.
        /// </summary>
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
        
        [Test]
        public void Test4()
        {
            if (string.IsNullOrEmpty(_lifecheck.Name))
                Assert.Fail();
        }


    }
}