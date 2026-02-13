// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public ConfirmEmailChangeModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGetAsync(string? userId, string? email, string? code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Não foi possível alterar o email. Tente novamente mais tarde ou contacte o suporte.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var currentEmail = (await _userManager.GetEmailAsync(user))!;
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                TempData.SetSwalError("Erro ao alterar email. Tente novamente mais tarde ou contacte o suporte.");
                return Page();
            }
            
            // Update the username to match the new email
            var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
            if (!setUserNameResult.Succeeded)
            {
                // Rollback the email change if setting the username fails
                await _userManager.ChangeEmailAsync(user, currentEmail, code);
                
                TempData.SetSwalError("Erro ao alterar email. Tente novamente mais tarde ou contacte o suporte.");
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData.SetSwalSuccess("Email alterado com sucesso.");
            return Page();
        }
    }
}
