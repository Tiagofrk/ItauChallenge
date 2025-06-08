using System;

namespace ItauChallenge.Domain
{
    public enum OperationType
    {
        Compra, // Matches ENUM 'Compra'
        Venda   // Matches ENUM 'Venda'
    }

    public class Operation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AssetId { get; set; }
        public OperationType Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime OperationDth { get; set; }
        public DateTime CreatedDth { get; set; }

        // Navigation properties (optional, but good practice)
        // public User User { get; set; }
        // public Asset Asset { get; set; }
    }
}
