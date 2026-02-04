using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User
{
    public class School
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        [Display(Name = "Nome")]
        public required string Name { get; set; }
        
        [Required]
        [MaxLength(9)]
        [Display(Name = "Código/Sigla")]
        public required string Code { get; set; }
        
        [Required]
        [MaxLength(250)]
        [Display(Name = "Endereço")]
        public required string Address { get; set; }
        
        [Required]
        [MaxLength(50)]
        [Display(Name = "Cidade")]
        public required string City { get; set; }
        
        public PostalCode? PostalCode { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
