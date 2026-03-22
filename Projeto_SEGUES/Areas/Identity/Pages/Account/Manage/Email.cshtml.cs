using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account.Manage;

/// <summary>
/// Classe de modelo para a página de gestão de email do utilizador.
/// </summary>
/// <remarks>
/// Esta classe permite ao utilizador visualizar o seu email atual, verificar o estado de confirmação 
/// e solicitar a alteração do endereço através do envio de tokens de segurança por email.
/// </remarks>
public class EmailModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailModel> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="EmailModel"/>.
    /// </summary>
    /// <param name="userManager">Gestor de utilizadores para operações de conta.</param>
    /// <param name="emailSender">Serviço de envio de emails para notificações e tokens.</param>
    public EmailModel(
        UserManager<AppUser> userManager,
        IEmailSender emailSender,
        ILogger<EmailModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <summary>
    /// Obtém ou define o email atual do utilizador.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Indica se o email atual já foi confirmado pelo utilizador.
    /// </summary>
    public bool IsEmailConfirmed { get; set; }

    /// <summary>
    /// Modelo de entrada de dados para o formulário de alteração de email.
    /// </summary>
    [BindProperty]
    public required InputModel Input { get; set; }

    /// <summary>
    /// Define as propriedades e validações do formulário de entrada para novo email.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// O novo endereço de email pretendido pelo utilizador.
        /// </summary>
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Endereço de email inválido.")]
        [Display(Name = "Novo email")]
        public required string NewEmail { get; init; }
    }

    /// <summary>
    /// Carrega os dados do utilizador para as propriedades do modelo.
    /// </summary>
    /// <param name="user">O utilizador autenticado atual.</param>
    private async Task LoadAsync(AppUser user)
    {
        var email = (await _userManager.GetEmailAsync(user))!;
        Email = email;

        Input = new InputModel
        {
            NewEmail = email,
        };

        IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
    }

    /// <summary>
    /// Processa o pedido GET inicial para a página de gestão de email.
    /// </summary>
    /// <returns>A página Razor com os dados carregados.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            throw new ApplicationException("Não foi possível carregar o utilizador.");
        }

        await LoadAsync(user);
        return Page();
    }

    /// <summary>
    /// Processa o pedido de alteração de email e envia o link de confirmação para o novo endereço.
    /// </summary>
    /// <returns>Redirecionamento para a mesma página com mensagem de sucesso ou erro.</returns>
    public async Task<IActionResult> OnPostChangeEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            throw new ApplicationException("Não foi possível carregar o utilizador.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var currentEmail = await _userManager.GetEmailAsync(user);
        if (Input.NewEmail != currentEmail)
        {
            // Check if the new email is already in use by another account
            var emailExists = await _userManager.FindByEmailAsync(Input.NewEmail);
            if (emailExists != null)
            {
                _logger.LogAppUser($"User {currentEmail} tried to change email to {Input.NewEmail}, but it's already in use.", UserAction.Update);
                ModelState.AddModelError("Input.NewEmail", "Este endereço de email já está registado no sistema.");
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
            var codeEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailChange",
                pageHandler: null,
                values: new { area = "Identity", userId, email = Input.NewEmail, code = codeEncoded },
                protocol: Request.Scheme)!;

            const string title = "Confirmação de Novo Email - SEGUES";
            string content = $"""
                                  <p>Recebemos um pedido para alterar o email associado à sua conta SEGUES para: <strong>{Input.NewEmail}</strong>.</p>
                                  <p>Para concluir este processo e validar o seu novo endereço, clique no botão abaixo:</p>
                                  <div style='text-align: center; margin: 30px 0;'>
                                      <a href='{callbackUrl}' style='background-color: #009697; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Confirmar Novo Email</a>
                                  </div>
                                  <p style='font-size: 0.8em; color: #666;'>Se não solicitou esta alteração, pode ignorar este email com segurança.</p>
                              """;

            var emailSenderService = _emailSender as EmailSender;
            string finalBody = emailSenderService?.GetEmailBody(title, user.FirstName, content) ?? content;

            try
            {
                await _emailSender.SendEmailAsync(Input.NewEmail, title, finalBody);
                _logger.LogAppUser($"User {currentEmail} requested email change to {Input.NewEmail}.", UserAction.Update);
                TempData.SetSwalSuccess("Pedido enviado! Verifique a sua nova caixa de entrada para confirmar a alteração.");
            }
            catch (Exception ex)
            {
                _logger.LogAppError(AppErrors.EmailSenderError, TableName.Identity, AppOperation.Update, ex);
                TempData.SetSwalError("Erro ao enviar email. Por favor, verifique a sua ligação ou tente mais tarde.");
            }

            return RedirectToPage();
        }

        TempData.SetSwalInfo("O endereço de email inserido é o mesmo que já está em uso.");
        return RedirectToPage();
    }

    /// <summary>
    /// Reenvia o email de confirmação para o endereço de email atual, caso este ainda não esteja confirmado.
    /// </summary>
    /// <returns>Redirecionamento para a página atual com feedback visual.</returns>
    public async Task<IActionResult> OnPostSendVerificationEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            throw new ApplicationException("Não foi possível carregar o utilizador.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var email = (await _userManager.GetEmailAsync(user))!;
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId, code },
            protocol: Request.Scheme)!;
        
        const string title = "Verificação de Conta - SEGUES";
        string content = $"""
                              <p>Obrigado por utilizar a plataforma SEGUES.</p>
                              <p>Para garantir a segurança da sua conta, por favor confirme o seu endereço de email clicando no botão abaixo:</p>
                              <div style='text-align: center; margin: 30px 0;'>
                                  <a href='{callbackUrl}' style='background-color: #009697; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Confirmar Email</a>
                              </div>
                          """;
        
        var emailSenderService = _emailSender as EmailSender;
        string finalBody = emailSenderService?.GetEmailBody(title, user.FirstName, content) ?? content;

        try
        {
            await _emailSender.SendEmailAsync(email, title, finalBody);
            _logger.LogAppUser($"User {email} requested to resend the email verification email.", UserAction.Update);
            TempData.SetSwalSuccess("Link de confirmação enviado. Por favor verifique a sua caixa de correio.");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.EmailSenderError, TableName.All, AppOperation.Other, ex);
            TempData.SetSwalError("Erro ao enviar email. Por favor, verifique a sua ligação ou tente mais tarde.");
        }
        
        return RedirectToPage();
    }
}