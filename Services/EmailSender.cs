using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace Projeto_SEGUES.Services
{
    // Esta classe finge que envia o email, mas não faz nada.
    // Serve apenas para o site não dar erro.
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Aqui futuramente podes meter lógica real.
            // Por agora, retorna "Tarefa Concluída" para enganar o sistema.
            return Task.CompletedTask;
        }
    }
}