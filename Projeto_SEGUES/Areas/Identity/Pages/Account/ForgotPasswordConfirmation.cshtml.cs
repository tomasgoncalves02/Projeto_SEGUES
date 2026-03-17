// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model da página de confirmação de envio do pedido de recuperação de senha.
    /// </summary>
    /// <remarks>
    /// Esta página é exibida após o utilizador submeter o seu email no processo de "Esqueci-me da Senha".
    /// Serve como um ponto de feedback genérico para informar o utilizador que deve verificar a sua caixa de entrada,
    /// independentemente de o email existir ou não no sistema, mantendo a privacidade dos dados.
    /// </remarks>
    [AllowAnonymous]
    public class ForgotPasswordConfirmation : PageModel
    {
        /// <summary>
        /// Processa o pedido GET da página de confirmação.
        /// </summary>
        /// <remarks>
        /// Não requer lógica adicional no servidor, pois apenas apresenta uma mensagem estática de sucesso/instrução na View.
        /// </remarks>
        public void OnGet()
        {
        }
    }
}