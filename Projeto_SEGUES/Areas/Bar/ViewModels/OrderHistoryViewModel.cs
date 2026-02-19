using System;

namespace Projeto_SEGUES.Areas.Bar.ViewModels
{
    public class OrderHistoryViewModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public DateTime DataCompra { get; set; }
        public TimeSpan HoraRecolha { get; set; }  
        public string Estado { get; set; } = string.Empty;
        public int StatusValue { get; set; }
        public DateOnly Validade { get; set; }      
        public DateOnly Recolhido { get; set; }   
        public decimal PrecoTotal { get; set; }
    }
}