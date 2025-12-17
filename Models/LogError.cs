namespace Projeto_SEGUES.Models
{
    public class LogError
    {
        public int Id { get; set; }
        public string Operation { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public string Table { get; set; } // Em que tabela ocorreu o erro
    }
}
