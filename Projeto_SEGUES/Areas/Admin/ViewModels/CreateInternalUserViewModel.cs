using Projeto_SEGUES.Attributes;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Validators;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Admin.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para o registo de novos utilizadores internos (Staff/Admin).
    /// </summary>
    /// <remarks>
    /// Contém todas as propriedades necessárias para a criação de conta e as respetivas 
    /// validações de integridade, como limites de caracteres, expressões regulares para nomes e idade mínima.
    /// </remarks>
    public class CreateInternalUserViewModel
    {
        /// <summary>
        /// Obtém ou define o primeiro nome do utilizador.
        /// </summary>
        /// <value>Obrigatório, entre 2 a 50 caracteres, apenas letras.</value>
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter no mínimo {2} letras.")]
        [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O nome não pode conter números nem símbolos.")]
        [Display(Name = "Primeiro Nome")]
        public required string FirstName { get; init; }

        /// <summary>
        /// Obtém ou define o sobrenome do utilizador.
        /// </summary>
        /// <value>Obrigatório, entre 2 a 50 caracteres, apenas letras.</value>
        [Required(ErrorMessage = "O sobrenome é obrigatório.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "O sobrenome deve ter no mínimo {2} letras.")]
        [RegularExpression(@"^[a-zA-Z\u00C0-\u00FF\s]*$", ErrorMessage = "O sobrenome não pode conter números nem símbolos.")]
        [Display(Name = "Sobrenome")]
        public required string LastName { get; init; }

        /// <summary>
        /// Obtém ou define o endereço de correio eletrónico.
        /// </summary>
        /// <value>Deve ser um formato de email válido e é obrigatório.</value>
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public required string Email { get; init; }

        /// <summary>
        /// Obtém ou define o género do utilizador.
        /// </summary>
        /// <value>Valor do Enum <see cref="Gender"/>.</value>
        [Required(ErrorMessage = "Selecione o género.")]
        [Display(Name = "Género")]
        public required Gender Gender { get; init; }

        /// <summary>
        /// Obtém ou define a data de nascimento do utilizador.
        /// </summary>
        /// <value>Data obrigatória. Requer idade mínima de 18 anos e máxima de 120.</value>
        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        [DataType(DataType.Date, ErrorMessage = "Data de nascimento inválida.")]
        [MinimumAge(ErrorMessage = "Deve ter pelo menos 18 anos para se registrar.")]
        [Display(Name = "Data de Nascimento")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
        [MaximumAge(120, ErrorMessage = "{0} inválida. Não pode ter mais de {1} anos.")]
        public required DateTime BirthDate { get; init; }

        /// <summary>
        /// Obtém ou define o tipo de conta (Role) a atribuir ao utilizador interno.
        /// </summary>
        /// <value>Normalmente "Admin" ou "Employee".</value>
        [Required(ErrorMessage = "Selecione o tipo de conta.")]
        [Display(Name = "Tipo de Conta")]
        public required string AccountType { get; init; }
    }
}