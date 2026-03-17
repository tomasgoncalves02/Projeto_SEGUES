using Projeto_SEGUES.Models.Ticket;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_SEGUES.Models.User;

public class UserCategory
{
    public int Id { get; init; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    public ICollection<TicketPrice> TicketPrices { get; set; } = new List<TicketPrice>();

    [NotMapped]
    public TicketPrice LatestPrice => TicketPrices?.OrderByDescending(x => x.InitialDatePrice).FirstOrDefault()!;
}
