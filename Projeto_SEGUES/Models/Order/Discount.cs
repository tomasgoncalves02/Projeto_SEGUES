using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Order;

public class Discount
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100, ErrorMessage = "O nome do desconto não pode exceder {1} caracteres.")]
    [Display(Name = "Nome")]
    public required string Name { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Valor")]
    [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
    public decimal Value { get; set; }

    [Required]
    public required DiscountType DiscountType { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Início")]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data de Fim")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Global")]
    public bool IsGlobal { get; set; } = false;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}