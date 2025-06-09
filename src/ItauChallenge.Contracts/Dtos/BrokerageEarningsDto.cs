namespace ItauChallenge.Contracts.Dtos; // Updated namespace

public class BrokerageEarningsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalEarnings { get; set; }
    public string Currency { get; set; } = "BRL";
    // Note: The interface IBrokerageApplicationService expected a DTO with a 'Details' string.
    // This DTO has 'Currency' instead. This might need reconciliation later.
    // For now, moving the DTO as defined in the Api layer.
}
