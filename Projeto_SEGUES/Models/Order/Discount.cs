using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Order;

/// <summary>
/// Entity representing a promotional rule that modifies the unit price of one or more products.
/// </summary>
/// <remarks>
/// This model supports both relative (%) and absolute (€) discounts. It includes temporal 
/// constraints (<see cref="StartDate"/> and <see cref="EndDate"/>) and a scope toggle (<see cref="IsGlobal"/>).
/// </remarks>
public class Discount
{
    /// <summary>Unique identifier for the discount rule.</summary>
    public int Id { get; set; }

    /// <summary>The internal name of the promotion (e.g., "Promoção de Natal").</summary>
    [Required]
    [MaxLength(100, ErrorMessage = "O nome do desconto não pode exceder {1} caracteres.")]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    /// <summary>The numeric value of the discount (either a percentage or a fixed amount).</summary>
    [Range(0, double.MaxValue)]
    [Display(Name = "Valor")]
    [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
    public decimal Value { get; set; }

    /// <summary>Determines the mathematical logic applied to the calculation (Percentage or Fixed).</summary>
    [Required]
    public required DiscountType DiscountType { get; set; }

    /// <summary>The date when the promotion becomes effective.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Início")]
    public DateTime StartDate { get; set; }

    /// <summary>The date when the promotion automatically expires.</summary>
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Fim")]
    public DateTime EndDate { get; set; }

    /// <summary>Manual override to enable or disable the promotion regardless of the date range.</summary>
    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    /// <summary>If true, the discount applies to every product in the system; otherwise, it only applies to <see cref="Products"/>.</summary>
    [Display(Name = "Global")]
    public bool IsGlobal { get; set; } = false;

    /// <summary>The collection of specific products eligible for this discount (if not global).</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}