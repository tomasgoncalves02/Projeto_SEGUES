using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Models
{
    public class TicketPrice
    {
        public int Id { get; set; }
        public TicketType TicketType { get; set; } 
        public decimal Price { get; set; }

        // Define o intervalo de tempo em que este preço é válido
        public DateTime InitialDatePrice { get; set; }
        public DateTime EndDatePrice { get; set; }
    }
}
