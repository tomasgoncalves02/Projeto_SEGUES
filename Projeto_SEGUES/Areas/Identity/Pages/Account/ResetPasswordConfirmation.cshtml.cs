// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model for the confirmation page indicating a successful password reset.
    /// </summary>
    /// <remarks>
    /// This page is the final destination of the "Forgot Password" workflow. 
    /// It informs the user that their new password has been successfully saved and 
    /// provides a link to return to the authentication page.
    /// </remarks>
    [AllowAnonymous]
    public class ResetPasswordConfirmationModel : PageModel
    {
        /// <summary>
        /// Processes the GET request to display the success confirmation message.
        /// </summary>
        /// <remarks>
        /// Contains no processing logic, serving only to render the static feedback View.
        /// </remarks>
        public void OnGet()
        {
        }
    }
}