namespace ItauChallenge.Domain;

public class Purchase
{
    public decimal Quantity { get; }
    public decimal Price { get; }

    public Purchase(decimal quantity, decimal price)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");
        }
        Quantity = quantity;
        Price = price;
    }
}
