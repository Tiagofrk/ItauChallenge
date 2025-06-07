namespace ItauChallenge.Api.Dtos;

public class AveragePriceDto
{
    public string UserId { get; set; }
    public string AssetId { get; set; }
    public decimal AveragePrice { get; set; }
    public int TotalQuantity { get; set; }
    public DateTime CalculationDate { get; set; }
}
