using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Model for the account lockout page (Lockout).
    /// </summary>
    /// <remarks>
    /// This page is automatically presented by the Identity system when a user 
    /// exceeds the maximum number of failed login attempts (configured in Program.cs).
    /// The lockout is temporary and serves to protect the account against brute-force attacks.
    /// </remarks>
    [AllowAnonymous]
    public class Lockout : PageModel
    {
        /// <summary>
        /// Processes the GET request for the lockout page.
        /// </summary>
        /// <remarks>
        /// Informs the user that their account is temporarily suspended for security reasons.
        /// </remarks>
        public void OnGet()
        {
        }
    }
}