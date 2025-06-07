using System;
using System.Collections.Generic;
using System.Linq;

namespace ItauChallenge.Domain;

public static class FinancialCalculations
{
    public static decimal CalculateWeightedAveragePrice(IEnumerable<Purchase> purchases)
    {
        if (purchases == null)
        {
            throw new ArgumentNullException(nameof(purchases));
        }

        var purchaseList = purchases.ToList();

        if (!purchaseList.Any())
        {
            throw new ArgumentException("Purchase list cannot be empty.", nameof(purchases));
        }

        // Individual purchase validation (quantity > 0, price > 0) is handled by the Purchase constructor.
        // If an invalid Purchase object were to be created bypassing the constructor (e.g. via reflection or direct field set),
        // additional checks could be added here, but for typical usage, the constructor guards are sufficient.

        decimal totalCost = 0;
        decimal totalQuantity = 0;

        foreach (var purchase in purchaseList)
        {
            // These checks are redundant if Purchase constructor is always used and enforces positivity.
            // However, they provide an additional layer of safety.
            if (purchase.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(purchases), $"Purchase with non-positive quantity ({purchase.Quantity}) found.");
            }
            if (purchase.Price <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(purchases), $"Purchase with non-positive price ({purchase.Price}) found.");
            }

            totalCost += purchase.Quantity * purchase.Price;
            totalQuantity += purchase.Quantity;
        }

        if (totalQuantity == 0)
        {
            // This case should ideally be prevented by the check for an empty list
            // and individual purchase quantity validation.
            throw new InvalidOperationException("Total quantity cannot be zero.");
        }

        return totalCost / totalQuantity;
    }
}
