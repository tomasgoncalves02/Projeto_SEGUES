namespace Projeto_SEGUES.Models
{
    public class UserLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Mantemos como int simples ou FK se quiser integridade referencial
        public string UserAction { get; set; } // "Create", "Login", etc.
        public DateTime Date { get; set; }

        // Se quiser criar a relação física (opcional para logs):
        // [ForeignKey("UserId")]
        // public User User { get; set; }
    }
}
