namespace ItauChallenge.Api.Dtos;

public class BrokerageEarningsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalEarnings { get; set; }
    public string Currency { get; set; } = "BRL";
}
