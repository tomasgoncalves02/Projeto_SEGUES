using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Models.Bar
{
    public class CartItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } // ID do AppUser
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Propriedades de Navegação
        public virtual AppUser User { get; set; }
        public virtual Product Product { get; set; }
    }
}