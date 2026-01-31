using System.ComponentModel.DataAnnotations.Schema;

namespace Projeto_SEGUES.Models
{
    public class BalanceCharge
    {
        public int Id { get; set; }
        public int Quantity { get; set; } // Pode ser redundante com Value, dependendo da lógica
        public DateTime TransactionDate { get; set; }
        public decimal Value { get; set; }

        // Relação com User (quem carregou o saldo)
        public string UserId { get; set; }
        //[ForeignKey("UserId")]
        public User User { get; set; }
    }
}
