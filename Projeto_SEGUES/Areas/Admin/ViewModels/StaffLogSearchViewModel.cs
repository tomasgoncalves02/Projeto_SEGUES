using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

public class StaffLogSearchViewModel
{
    [Display(Name = "Pesquisar")]
    public string? SearchString { get; set; }
    
    [Display(Name = "Ação")]
    public UserAction? ActionFilter { get; set; }
    
    [Display(Name = "Data")]
    [DataType(DataType.Date, ErrorMessage = "Insira uma data válida.")]
    public DateTime? DateFilter { get; set; }
    
    [ValidateNever]
    public List<StaffLogDto> Results { get; set; } = [];
}