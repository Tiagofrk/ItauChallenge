namespace ItauChallenge.Contracts.Dtos; // Updated namespace

public class LatestQuoteDto
{
    public string AssetId { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } // e.g., "Live", "Fallback", "Cache"
}
