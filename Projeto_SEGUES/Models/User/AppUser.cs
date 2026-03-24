using Microsoft.AspNetCore.Identity;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Validators;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.User;

/*
 * Identity user has attributes for email, phone number, password hash, etc. IdentityRole manages roles.
 * https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.identityuser?view=aspnetcore-10.0&viewFallbackFrom=net-8.0
 */
public class AppUser : IdentityUser
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

    [MaxLength(9, ErrorMessage = "O NIF deve ter no máximo {1} caracteres.")]
    [Display(Name = "NIF")]
    public string? FiscalNumber { get; set; }

    [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
    [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos para se registrar.")]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Nascimento")]
    public required DateTime BirthDate { get; set; }

    [Required]
    public required Gender Gender { get; set; }

    [Display(Name = "Data de Criação")]
    public DateTime CreationDate { get; init; } = DateTime.Now;

    public UserStatus Status { get; set; } = UserStatus.Active;

    [MaxLength(250, ErrorMessage = "Morada deve ter no máximo {1} caracteres.")]
    [Display(Name = "Morada")]
    public string? Address { get; set; }

    [MaxLength(50, ErrorMessage = "Cidade deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cidade")]
    public string? City { get; set; }

    public PostalCode? PostalCode { get; set; } // FK
}