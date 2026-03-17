using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Order.ViewModels;

/// <summary>
/// ViewModel que representa o resumo financeiro e quantitativo de uma encomenda ou carrinho.
/// </summary>
/// <remarks>
/// Este modelo é frequentemente utilizado em respostas JSON para atualizar elementos da interface 
/// em tempo real, como o contador de itens no cabeçalho e o valor total acumulado.
/// </remarks>
public class OrderTotalViewModel
{
    /// <summary>
    /// Soma total de todos os itens individuais presentes na encomenda ou carrinho.
    /// </summary>
    /// <value>Valor inteiro não negativo.</value>
    [Range(0, int.MaxValue)]
    [Display(Name = "Quantidade Total")]
    public int TotalQuantity { get; set; } = 0;

    /// <summary>
    /// Valor monetário total da encomenda, somando o preço unitário multiplicado pela quantidade de cada produto.
    /// </summary>
    /// <value>Valor decimal não negativo.</value>
    [Range(0, double.MaxValue)]
    [Display(Name = "Valor Total")]
    public decimal TotalValue { get; set; } = 0m;
}