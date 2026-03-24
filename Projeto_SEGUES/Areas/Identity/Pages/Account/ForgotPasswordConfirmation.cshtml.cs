// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model for the confirmation page displayed after a password recovery request is submitted.
    /// </summary>
    /// <remarks>
    /// This page is shown after the user submits their email in the "Forgot Password" process.
    /// It serves as a generic feedback point to inform the user to check their inbox,
    /// regardless of whether the email exists in the system or not, maintaining data privacy.
    /// </remarks>
    [AllowAnonymous]
    public class ForgotPasswordConfirmation : PageModel
    {
        /// <summary>
        /// Processes the GET request for the confirmation page.
        /// </summary>
        /// <remarks>
        /// Requires no additional server-side logic, as it only presents a static success/instruction message in the View.
        /// </remarks>
        public void OnGet()
        {
        }
    }
}