using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }      
        public UserStatus Status { get; set; }
        public decimal Balance { get; set; }
        public UserRole Role { get; set; }
        public string Nif { get; set; }
        public DateTime BirthDate { get; set; }
        public Gender Gender { get; set; }
        public DateTime CreationDate { get; set; }

        // Foreign Key para PostalCode
        public int PostalCodeId { get; set; }
        [ForeignKey("PostalCodeId")]
        public PostalCode? PostalCode { get; set; }
    }
}
