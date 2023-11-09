using MinimizerCommon.Commons;

namespace Minimizertest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var lifecheck = new LifeCheck("BackApi", true);

            if (lifecheck == null)
            {
                Assert.Fail();
            }
            if (lifecheck.Status is false)
                Assert.Fail();
            if (String.IsNullOrEmpty(lifecheck.Name))
                Assert.Fail();

            Assert.Pass();
        }
        [Test]
        public void Test2()
        {
            var lifecheck = new LifeCheck("BackApi", true);


            if (lifecheck.Status is false)
                Assert.Fail();

            Assert.Pass();
        }
        [Test]
        public void Test3()
        {
            var lifecheck = new LifeCheck("BackApi", true);

            if (String.IsNullOrEmpty(lifecheck.Name))
                Assert.Fail();

            Assert.Pass();
        }
    }
}