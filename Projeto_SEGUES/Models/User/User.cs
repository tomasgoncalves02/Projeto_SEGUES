using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Models.User
{
    /*
     * Identity user has attributes for email, phone number, password hash, etc. IdentityRole manages roles.
     * https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.identityuser?view=aspnetcore-10.0&viewFallbackFrom=net-8.0
     */
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        [Display(Name = "Primeiro(s) Nome(s)")]
        public required string FirstName { get; set; }
        
        [Required]
        [MaxLength(50)]
        [Display(Name = "Apelido(s)")]
        public required string LastName { get; set; }
        
        [Required]
        public required UserCategory UserCategory { get; set; } // FK

        [Range(0, double.MaxValue)]
        [Display(Name = "Saldo")]
        public decimal Balance { get; set; } // 0 is default
        
        [MaxLength(9)]
        [Display(Name = "NIF")]
        public string? FiscalNumber { get; set; }
        
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data de Nascimento")]
        public DateTime? BirthDate { get; set; }
        
        [Required]
        public required Gender Gender { get; set; }
        
        [Display(Name = "Data de Criação")]
        public DateTime CreationDate { get; set; } = DateTime.Now;
        
        public UserStatus Status { get; set; } = UserStatus.Active;
        
        [MaxLength(250)]
        [Display(Name = "Morada")]
        public string? Address { get; set; }
        
        [MaxLength(50)]
        [Display(Name = "Cidade")]
        public string? City { get; set; }
        
        public PostalCode? PostalCode { get; set; } // FK
    }
}
