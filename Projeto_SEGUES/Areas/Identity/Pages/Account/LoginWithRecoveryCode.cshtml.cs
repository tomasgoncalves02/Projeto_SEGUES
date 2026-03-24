using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model for the login page using recovery codes (Recovery Codes).
    /// </summary>
    /// <remarks>
    /// This page serves as a contingency method for users with 2FA active. 
    /// It allows access to the system using one of the single-use codes generated during 
    /// the two-factor authentication setup, in case the primary device is inaccessible.
    /// </remarks>
    [AllowAnonymous]
    public class LoginWithRecoveryCode : PageModel
    {
        /// <summary>
        /// Processes the GET request for the recovery code entry page.
        /// </summary>
        /// <remarks>
        /// Verifies if the user has passed the first stage of authentication (password) 
        /// and prepares the form to receive the alphanumeric security code.
        /// </remarks>
        public void OnGet()
        {
        }
    }
}