using System;
using MinimizerCommon.Commons;
using NUnit.Framework;

namespace MinimizerCommon.Commons.Tests
{
    [TestFixture]
    public class LifeCheckTests
    {
        [Test]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            string name = "Test API";
            bool status = true;

            // Act
            var lifeCheck = new LifeCheck(name, status);

            // Assert
            Assert.AreEqual(name, lifeCheck.Name);
            Assert.AreEqual(status, lifeCheck.Status);
            Assert.That((DateTime.Now - lifeCheck.Datetime).TotalSeconds, Is.LessThan(1));
        }

        [Test]
        public void Properties_ShouldAllowModification()
        {
            // Arrange
            var lifeCheck = new LifeCheck("Initial Name", true);

            // Act
            lifeCheck.Name = "Updated Name";
            lifeCheck.Status = false;
            lifeCheck.Datetime = DateTime.Now.AddMinutes(1);

            // Assert
            Assert.AreEqual("Updated Name", lifeCheck.Name);
            Assert.IsFalse(lifeCheck.Status);
            Assert.That((DateTime.Now.AddMinutes(1) - lifeCheck.Datetime).TotalSeconds, Is.LessThan(1));
        }

        [Test]
        public void Constructor_WithFalseStatus_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            string name = "Test API";
            bool status = false;

            // Act
            var lifeCheck = new LifeCheck(name, status);

            // Assert
            Assert.AreEqual(name, lifeCheck.Name);
            Assert.IsFalse(lifeCheck.Status);
            Assert.IsNotNull(lifeCheck.Datetime);
        }
    }
}