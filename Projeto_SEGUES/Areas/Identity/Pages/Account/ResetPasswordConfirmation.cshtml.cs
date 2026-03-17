// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model da página de confirmação de sucesso na redefinição de password.
    /// </summary>
    /// <remarks>
    /// Esta página é o destino final do fluxo de "Esqueci-me da senha". 
    /// Informa o utilizador que a sua nova password foi gravada com sucesso e fornece 
    /// o link para regressar à página de autenticação.
    /// </remarks>
    [AllowAnonymous]
    public class ResetPasswordConfirmationModel : PageModel
    {
        /// <summary>
        /// Processa o pedido GET para apresentar a mensagem de confirmação de sucesso.
        /// </summary>
        /// <remarks>
        /// Não contém lógica de processamento, servindo apenas para renderizar a View estática de feedback.
        /// </remarks>
        public void OnGet()
        {
        }
    }
}