using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models
{
    public class PostalCode
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } // Ex: "2900-000"

        
        public ICollection<User> Users { get; set; }
    }
}
