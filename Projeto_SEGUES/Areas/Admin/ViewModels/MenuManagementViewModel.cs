using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Admin.ViewModels
{
    /// <summary>
    /// ViewModel responsável pelo transporte e validação dos links das ementas na área administrativa.
    /// </summary>
    /// <remarks>
    /// Este modelo assegura que os URLs introduzidos para o refeitório e bar cumprem os requisitos 
    /// de formatação necessários para serem apresentados corretamente aos utilizadores finais.
    /// </remarks>
    public class MenuManagementViewModel
    {
        /// <summary>
        /// Obtém ou define o endereço URL da ementa do refeitório (Cantina).
        /// </summary>
        /// <value>Deve ser um URL válido (ex: https://dominio.com/ementa.pdf). Admite valores nulos.</value>
        [Url(ErrorMessage = "Introduza um URL válido (ex: https://...)")]
        [Display(Name = "Link da Ementa do Refeitório")]
        public string? CanteenUrl { get; set; }

        /// <summary>
        /// Obtém ou define o endereço URL da ementa do bar.
        /// </summary>
        /// <value>Deve ser um URL válido (ex: https://dominio.com/bar.pdf). Admite valores nulos.</value>
        [Url(ErrorMessage = "Introduza um URL válido (ex: https://...)")]
        [Display(Name = "Link da Ementa do Bar")]
        public string? BarUrl { get; set; }
    }
}