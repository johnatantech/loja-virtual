namespace Loja.Domain.Pedidos;

/// <summary>
/// Guarda nome e preço do momento da compra: se o produto mudar de preço depois,
/// o pedido continua mostrando o valor que o cliente pagou.
/// </summary>
public class ItemPedido
{
    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public string NomeProduto { get; private set; } = null!;
    public decimal PrecoUnitario { get; private set; }
    public int Quantidade { get; private set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;

    private ItemPedido() { } // EF Core

    internal ItemPedido(int produtoId, string nomeProduto, decimal precoUnitario, int quantidade)
    {
        ProdutoId = produtoId;
        NomeProduto = nomeProduto;
        PrecoUnitario = precoUnitario;
        Quantidade = quantidade;
    }

    internal void Somar(int quantidade) => Quantidade += quantidade;
}
