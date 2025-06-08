using System;

namespace ItauChallenge.Domain
{
    public class Position
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AssetId { get; set; }
        public int Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public DateTime UpdatedDth { get; set; }
        public DateTime CreatedDth { get; set; }

        // Navigation properties (optional)
        // public User User { get; set; }
        // public Asset Asset { get; set; }
    }
}
