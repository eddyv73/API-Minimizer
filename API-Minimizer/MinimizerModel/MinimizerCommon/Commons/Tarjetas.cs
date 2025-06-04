using System;
using System.Linq;

namespace MinimizerCommon.Commons
{
    /// <summary>
    /// Represents a simple credit or debit card entity.
    /// </summary>
    public class Tarjetas
    {
        public Tarjetas(int id, string name, string description, bool status, string cardNumber, string ccv)
        {
            if (string.IsNullOrWhiteSpace(ccv) || ccv.Length != 3 || !ccv.All(char.IsDigit))
            {
                throw new ArgumentException("CCV must be exactly 3 digits.");
            }

            Id = id;
            Name = name;
            Description = description;
            Status = status;
            CardNumber = cardNumber;
            CCV = ccv;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Status { get; set; }
        public string CardNumber { get; set; }
        public string CCV { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
