using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Identity.ViewModels;

/// <summary>
/// ViewModel responsável pela recolha e validação dos dados de registo de um novo utilizador.
/// </summary>
/// <remarks>
/// Este modelo impõe regras estritas de segurança, incluindo complexidade de password, 
/// verificação de idade mínima/máxima e consistência na confirmação de dados.
/// </remarks>
public class RegisterDataViewModel
{
    /// <summary>
    /// Primeiro nome do utilizador.
    /// </summary>
    /// <value>Obrigatório, entre 2 a 50 caracteres, sem números ou símbolos.</value>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
    [Display(Name = "Primeiro Nome")]
    public required string FirstName { get; init; }

    /// <summary>
    /// Sobrenome do utilizador.
    /// </summary>
    /// <value>Obrigatório, entre 2 a 50 caracteres, sem números ou símbolos.</value>
    [Required(ErrorMessage = "O sobrenome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
    [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
    [Display(Name = "Sobrenome")]
    public required string LastName { get; init; }

    /// <summary>
    /// Género biológico ou identidade de género do utilizador.
    /// </summary>
    /// <value>Baseado no enumerado <see cref="Gender"/>.</value>
    [Required(ErrorMessage = "O género é obrigatório.")]
    [Display(Name = "Género")]
    public required Gender Gender { get; init; }

    /// <summary>
    /// Endereço de email principal para a conta.
    /// </summary>
    /// <value>Deve ser um formato de email válido (ex: utilizador@dominio.com).</value>
    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Endereço de email inválido.")]
    [Display(Name = "Email")]
    public required string Email { get; init; }

    /// <summary>
    /// Data de nascimento para verificação de elegibilidade.
    /// </summary>
    /// <value>Obrigatório. Requer idade mínima de 18 anos.</value>
    [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
    [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
    [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos para se registrar.")]
    [Display(Name = "Data de Nascimento")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [MaximumAge(120, ErrorMessage = "{0} inválida. Não pode ter mais de {1} anos.")]
    public required DateTime BirthDate { get; init; }

    /// <summary>
    /// Senha de acesso à conta.
    /// </summary>
    /// <value>Mínimo 12 caracteres, incluindo maiúsculas, minúsculas, números e símbolos.</value>
    [Required(ErrorMessage = "A password é obrigatória.")]
    [StringLength(100, ErrorMessage = "A password deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 12)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$",
        ErrorMessage = "A password deve ter pelo menos: 1 Minúscula, 1 Maiúscula, 1 Número e 1 Símbolo. E no mínimo 12 caracteres.")]
    public required string Password { get; init; }

    /// <summary>
    /// Campo de verificação para garantir que a senha foi digitada corretamente.
    /// </summary>
    /// <value>Deve ser idêntico ao campo Password.</value>
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar password")]
    [Compare("Password", ErrorMessage = "A password e a confirmação não coincidem.")]
    public required string ConfirmPassword { get; init; }

    /// <summary>
    /// Código de ativação ou convite associado ao registo.
    /// </summary>
    public required string Code { get; set; } = "";

    /// <summary>
    /// Data e hora de expiração da sessão ou do código de registo.
    /// </summary>
    public required DateTime ExpiryTime { get; set; } = DateTime.Now;
}