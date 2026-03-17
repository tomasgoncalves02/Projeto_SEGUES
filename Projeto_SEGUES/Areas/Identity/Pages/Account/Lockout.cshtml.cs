using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model da página de bloqueio de conta (Lockout).
    /// </summary>
    /// <remarks>
    /// Esta página é apresentada automaticamente pelo sistema de Identity quando um utilizador 
    /// excede o número máximo de tentativas de login falhadas (configurado no Program.cs).
    /// O bloqueio é temporário e serve para proteger a conta contra ataques de força bruta.
    /// </remarks>
    [AllowAnonymous]
    public class Lockout : PageModel
    {
        /// <summary>
        /// Processa o pedido GET da página de bloqueio.
        /// </summary>
        /// <remarks>
        /// Informa o utilizador que a sua conta está temporariamente suspensa por razões de segurança.
        /// </remarks>
        public void OnGet()
        {
        }
    }
}