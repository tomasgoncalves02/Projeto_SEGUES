using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Inventory.ViewModels;

/// <summary>
/// ViewModel utilizado para a criação, edição e visualização de produtos no inventário.
/// </summary>
/// <remarks>
/// Este modelo transporta os dados de um produto entre a interface de utilizador e os serviços de inventário,
/// garantindo que as regras de stock e preços sejam validadas antes da persistência.
/// </remarks>
public class CreateProductViewModel
{
    /// <summary>
    /// Identificador único do produto.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome descritivo do produto.
    /// </summary>
    /// <value>Obrigatório, máximo de 100 caracteres.</value>
    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome do produto não pode exceder {1} caracteres.")]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    /// <summary>
    /// Explicação detalhada sobre o produto.
    /// </summary>
    /// <value>Obrigatório, máximo de 250 caracteres.</value>
    [Required(ErrorMessage = "A descrição do produto é obrigatória.")]
    [MaxLength(250, ErrorMessage = "A descrição do produto não pode exceder {1} caracteres.")]
    [Display(Name = "Descrição")]
    public required string Description { get; set; }

    /// <summary>
    /// Identificador da categoria à qual o produto pertence.
    /// </summary>
    /// <value>Chave estrangeira (FK) obrigatória.</value>
    [Required(ErrorMessage = "A categoria do produto é obrigatória.")]
    [Display(Name = "Categoria")]
    public required int CategoryId { get; set; } // FK

    /// <summary>
    /// Valor monetário unitário do produto.
    /// </summary>
    /// <value>Obrigatório, deve ser um valor positivo.</value>
    [Required(ErrorMessage = "O preço do produto é obrigatório.")]
    [Range(0, double.MaxValue)]
    [Display(Name = "Preço (€)")]
    public required decimal Price { get; set; }

    /// <summary>
    /// Quantidade atual disponível em armazém.
    /// </summary>
    /// <value>Obrigatório, valor inteiro não negativo.</value>
    [Required(ErrorMessage = "O stock do produto é obrigatório.")]
    [Range(0, int.MaxValue)]
    [Display(Name = "Stock")]
    public required int Stock { get; set; }

    /// <summary>
    /// Limite de segurança para alerta de reposição de stock.
    /// </summary>
    /// <value>Obrigatório, utilizado para monitorização de rutura de stock.</value>
    [Required(ErrorMessage = "O stock mínimo do produto é obrigatório.")]
    [Range(0, int.MaxValue)]
    [Display(Name = "Stock mínimo")]
    public required int MinimumStock { get; set; }

    /// <summary>
    /// Define se o produto está disponível para venda ou uso no sistema.
    /// </summary>
    /// <value>Padrão é <c>true</c>.</value>
    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;
}