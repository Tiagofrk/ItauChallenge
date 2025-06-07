namespace ItauChallenge.Api.Dtos;

public class ClientRankingInfoDto
{
    public string ClientId { get; set; }
    public string ClientName { get; set; } // Assuming name is available
    public decimal Value { get; set; } // Could be total position value or total brokerage paid
}

public class TopClientsDto
{
    public string Criteria { get; set; } // e.g., "ByPosition", "ByBrokerage"
    public int Count { get; set; }
    public List<ClientRankingInfoDto> Clients { get; set; }
    public DateTime AsOfDate { get; set; }
}
