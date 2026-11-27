namespace Loja.Application.Carrinhos;

public record ItemSolicitadoDto(int ProdutoId, int Quantidade);

public record ItemCarrinhoDto(
    int ProdutoId, string Nome, string Slug, string CategoriaSlug, string? ImagemUrl,
    decimal PrecoUnitario, int Quantidade, int EstoqueDisponivel, decimal Subtotal, string? Aviso);

/// <summary>
/// Carrinho já precificado com os dados atuais do catálogo. Avisos explicam ajustes,
/// como um produto que esgotou ou ficou com menos unidades do que o pedido.
/// </summary>
public record CarrinhoDto(IReadOnlyList<ItemCarrinhoDto> Itens, int QuantidadeTotal, decimal Total)
{
    public bool PodeFinalizar => Itens.Count > 0 && Itens.All(i => i.Aviso is null);
}
