using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model da página de login via códigos de recuperação (Recovery Codes).
    /// </summary>
    /// <remarks>
    /// Esta página serve como método de contingência para utilizadores com 2FA ativo. 
    /// Permite a entrada no sistema utilizando um dos códigos de uso único gerados durante a 
    /// configuração da autenticação de dois fatores, caso o dispositivo principal esteja inacessível.
    /// </remarks>
    [AllowAnonymous]
    public class LoginWithRecoveryCode : PageModel
    {
        /// <summary>
        /// Processa o pedido GET para a página de introdução do código de recuperação.
        /// </summary>
        /// <remarks>
        /// Verifica se o utilizador passou pela primeira fase de autenticação (password) 
        /// e prepara o formulário para receber o código alfanumérico de segurança.
        /// </remarks>
        public void OnGet()
        {
        }
    }
}