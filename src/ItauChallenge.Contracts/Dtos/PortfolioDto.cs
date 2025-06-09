namespace ItauChallenge.Contracts.Dtos
{
    public class PortfolioDto
    {
        public IEnumerable<ClientPositionDto> Positions { get; set; }
        public decimal Consolidated { get; set; }
    }
}
