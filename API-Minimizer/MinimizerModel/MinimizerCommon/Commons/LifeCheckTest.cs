using System;
using MinimizerCommon.Commons;
using Xunit;

namespace MinimizerCommon.Commons.Tests
{
    public class TarjetasTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            int id = 1;
            string name = "Test Card";
            string description = "This is a test card.";
            bool status = true;
            string cardNumber = "1234567890123456";
            string ccv = "123";

            // Act
            var tarjeta = new Tarjetas(id, name, description, status, cardNumber, ccv);

            // Assert
            Assert.Equal(id, tarjeta.Id);
            Assert.Equal(name, tarjeta.Name);
            Assert.Equal(description, tarjeta.Description);
            Assert.Equal(status, tarjeta.Status);
            Assert.Equal(cardNumber, tarjeta.CardNumber);
            Assert.Equal(ccv, tarjeta.CCV);
            Assert.True((DateTime.Now - tarjeta.CreatedAt).TotalSeconds < 1);
            Assert.True((DateTime.Now - tarjeta.UpdatedAt).TotalSeconds < 1);
        }

        [Fact]
        public void Properties_ShouldAllowModification()
        {
            // Arrange
            var tarjeta = new Tarjetas(1, "Test Card", "Description", true, "1234567890123456", "123");

            // Act
            tarjeta.Name = "Updated Name";
            tarjeta.Description = "Updated Description";
            tarjeta.Status = false;
            tarjeta.UpdatedAt = DateTime.Now.AddMinutes(1);

            // Assert
            Assert.Equal("Updated Name", tarjeta.Name);
            Assert.Equal("Updated Description", tarjeta.Description);
            Assert.False(tarjeta.Status);
            Assert.True((DateTime.Now.AddMinutes(1) - tarjeta.UpdatedAt).TotalSeconds < 1);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenCCVIsInvalid()
        {
            // Arrange
            int id = 1;
            string name = "Test Card";
            string description = "This is a test card.";
            bool status = true;
            string cardNumber = "1234567890123456";
            string invalidCCV = "1234"; // Invalid CCV with more than 3 digits

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Tarjetas(id, name, description, status, cardNumber, invalidCCV));
            Assert.Equal("CCV must be exactly 3 digits.", exception.Message);
        }
    }
}