using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.ViewModels;

namespace Projeto_SEGUES.Views.Admin
{
    public class CreateInternalAccountModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly AppDbContext _context;

        public CreateInternalAccountModel(UserManager<AppUser> userManager, IEmailSender emailSender, AppDbContext context)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
        }

        [BindProperty]
        public required CreateInternalUserViewModel Input { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            // Como a senha não vem do formulário, removemos a sua validação obrigatória
            ModelState.Remove("Input.Password");

            if (!ModelState.IsValid) return Page();

            // 1. Verificar se o email já existe
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "Este endere�o de email j� est� registado.");
                return Page();
            }

            // 2. Gerar senha aleatória forte
            string temporaryPassword = GenerateSecurePassword();
            
            var category = _context.UserCategories.FirstOrDefault(c => c.Name == "Externo");

            // 3. Mapear para a entidade User
            var user = new AppUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Gender = Input.Gender,
                CreationDate = DateTime.Now,
                EmailConfirmed = true,
                UserCategory = category!
            };

            // 4. Criar utilizador na Base de Dados
            var result = await _userManager.CreateAsync(user, temporaryPassword);

            if (result.Succeeded)
            {
                // Atribuir Role (Admin ou Employee)
                await _userManager.AddToRoleAsync(user, Input.AccountType);

                // Enviar o email com a senha gerada
                await EnviarEmailBoasVindas(Input.Email, Input.FirstName, temporaryPassword, Input.AccountType);

                // Mensagem de Sucesso para o SweetAlert ler
                TempData["Success"] = $"Conta de {Input.FirstName} criada! A senha foi enviada para {Input.Email}.";

                // --- MUDANÇA AQUI ---
                // Limpamos o formulário para poderes criar outro utilizador logo de seguida
                ModelState.Clear();
                Input = new CreateInternalUserViewModel();

                // EM VEZ DE REDIRECIONAR, FICAMOS NA PÁGINA PARA O POP-UP APARECER
                return Page();
            }

            // Caso haja erros de política de password do Identity
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private string GenerateSecurePassword()
        {
            // Gera uma senha com 12 caracteres: Letras, Números e Especial
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@$?_-";
            var random = new Random();
            var password = new StringBuilder();

            for (int i = 0; i < 12; i++)
            {
                password.Append(chars[random.Next(chars.Length)]);
            }

            // Garante que termina com um padrão que satisfaz a maioria das políticas
            return password + "Z9!";
        }

        private async Task EnviarEmailBoasVindas(string email, string name, string password, string role)
        {
            string roleDisplay = role == "Admin" ? "Administrador" : "Funcionário";

            string emailBody = $@"
            <div style='font-family: sans-serif; max-width: 600px; margin: auto; border-top: 6px solid #009697;'>
                <div style='background-color: #009697; padding: 20px; text-align: center; color: white;'>
                    <h2>BEM-VINDO AO SEGUES</h2>
                </div>
                <div style='padding: 30px; color: #333;'>
                    <p>Olá <strong>{name}</strong>,</p>
                    <p>A tua conta de <strong>{roleDisplay}</strong> foi criada com sucesso.</p>
                    <div style='background: #f4f4f4; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Credenciais de Acesso:</strong></p>
                        <p>Email: {email}</p>
                        <p>Senha Temporária: <span style='color: #009697; font-weight: bold;'>{password}</span></p>
                    </div>
                    <p>Por favor, altera a tua senha após o primeiro login.</p>
                    <div style='text-align: center; margin-top: 30px;'>
                        <a href='{Request.Scheme}://{Request.Host}/Identity/Account/Login' 
                           style='background: #009697; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px;'>Aceder ao Portal</a>
                    </div>
                </div>
            </div>";

            await _emailSender.SendEmailAsync(email, "Acesso ao Sistema SEGUES", emailBody);
        }
    }
}