using System;

namespace ItauChallenge.Domain
{
    public class Quote
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public decimal Price { get; set; }
        public DateTime QuoteDth { get; set; }
        public DateTime CreatedDth { get; set; }

        // Navigation property (optional)
        // public Asset Asset { get; set; }
    }
}
