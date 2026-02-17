using System;

namespace Projeto_SEGUES.Areas.Bar.ViewModels
{
    public class OrderHistoryViewModel
    {
        public string Codigo { get; set; } = string.Empty;
        public DateTime DataCompra { get; set; }
        public string HoraRecolha { get; set; } = string.Empty; // Novo
        public string Estado { get; set; } = string.Empty;
        public int StatusValue { get; set; }
        public DateTime Validade { get; set; } // Novo
        public string? RecolhidoEm { get; set; } // Novo
        public decimal PrecoTotal { get; set; }
    }
}