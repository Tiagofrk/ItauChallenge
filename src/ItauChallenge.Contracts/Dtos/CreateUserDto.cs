using System.ComponentModel.DataAnnotations;

namespace ItauChallenge.Contracts.Dtos // Updated namespace
{
    public class CreateUserDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [Range(0, 0.9999)] // Example range for brokerage percent, allowing 0.0000 to 0.9999
        public decimal BrokeragePercent { get; set; }
    }
}
