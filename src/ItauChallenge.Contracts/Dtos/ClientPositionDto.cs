using System.Collections.Generic;

namespace ItauChallenge.Contracts.Dtos; // Updated namespace

public class AssetPositionDto
{
    public string AssetId { get; set; }
    public decimal Quantity { get; set; }
    public decimal CurrentMarketPrice { get; set; } // Could be fetched live
    public decimal TotalValue { get; set; } // Quantity * CurrentMarketPrice
    public decimal AverageAcquisitionPrice { get; set; } // From domain logic
    public decimal ProfitOrLoss { get; set; } // Added in ClientApplicationService implementation
}

public class ClientPositionDto
{
    public string ClientId { get; set; }
    public List<AssetPositionDto> Assets { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public DateTime AsOfDate { get; set; }
}
