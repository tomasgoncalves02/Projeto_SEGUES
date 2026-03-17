using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Admin.ViewModels
{
    public class MenuManagementViewModel
    {
        [Url(ErrorMessage = "Introduza um URL válido (ex: https://...)")]
        [Display(Name = "Link da Ementa do Refeitório")]
        public string? RefeitorioUrl { get; set; }

        [Url(ErrorMessage = "Introduza um URL válido (ex: https://...)")]
        [Display(Name = "Link da Ementa do Bar")]
        public string? BarUrl { get; set; }
    }
}