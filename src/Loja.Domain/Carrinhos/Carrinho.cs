using Loja.Domain.Comum;

namespace Loja.Domain.Carrinhos;

/// <summary>
/// Carrinho de um cliente logado. Guarda só produto e quantidade: preço, nome e estoque
/// são lidos do catálogo na hora de exibir, então nunca ficam desatualizados.
/// </summary>
public class Carrinho
{
    public const int QuantidadeMaximaPorItem = 10;

    private readonly List<ItemCarrinho> _itens = [];

    public int Id { get; private set; }
    public string ClienteId { get; private set; } = null!;
    public DateTime AtualizadoEm { get; private set; } = DateTime.UtcNow;
    public IReadOnlyCollection<ItemCarrinho> Itens => _itens.AsReadOnly();

    private Carrinho() { } // EF Core

    public Carrinho(string clienteId)
    {
        if (string.IsNullOrWhiteSpace(clienteId))
            throw new DomainException("O carrinho precisa de um cliente.");
        ClienteId = clienteId;
    }

    /// <summary>Define a quantidade de um produto. Zero remove o item.</summary>
    public void DefinirQuantidade(int produtoId, int quantidade)
    {
        if (quantidade < 0)
            throw new DomainException("A quantidade não pode ser negativa.");
        if (quantidade > QuantidadeMaximaPorItem)
            throw new DomainException($"O limite é de {QuantidadeMaximaPorItem} unidades por produto.");

        var item = _itens.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (quantidade == 0)
        {
            if (item is not null) _itens.Remove(item);
        }
        else if (item is null)
        {
            _itens.Add(new ItemCarrinho(produtoId, quantidade));
        }
        else
        {
            item.AlterarQuantidade(quantidade);
        }
        AtualizadoEm = DateTime.UtcNow;
    }

    public int QuantidadeDe(int produtoId) =>
        _itens.FirstOrDefault(i => i.ProdutoId == produtoId)?.Quantidade ?? 0;

    /// <summary>
    /// Junta o carrinho montado sem login com o da conta: quantidades do mesmo produto
    /// são somadas, respeitando o limite por item.
    /// </summary>
    public void Mesclar(IEnumerable<(int ProdutoId, int Quantidade)> itensVisitante)
    {
        foreach (var (produtoId, quantidade) in itensVisitante.Where(i => i.Quantidade > 0))
        {
            var total = Math.Min(QuantidadeDe(produtoId) + quantidade, QuantidadeMaximaPorItem);
            DefinirQuantidade(produtoId, total);
        }
    }

    public void Esvaziar()
    {
        _itens.Clear();
        AtualizadoEm = DateTime.UtcNow;
    }
}

public class ItemCarrinho
{
    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public int Quantidade { get; private set; }

    private ItemCarrinho() { } // EF Core

    internal ItemCarrinho(int produtoId, int quantidade)
    {
        ProdutoId = produtoId;
        Quantidade = quantidade;
    }

    internal void AlterarQuantidade(int quantidade) => Quantidade = quantidade;
}
