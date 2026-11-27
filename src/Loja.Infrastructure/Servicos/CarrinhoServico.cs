using Loja.Application.Carrinhos;
using Loja.Domain.Carrinhos;
using Loja.Domain.Comum;
using Loja.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Loja.Infrastructure.Servicos;

public class CarrinhoServico(LojaDbContext db) : ICarrinhoServico
{
    public Task<CarrinhoDto> CotarAsync(IReadOnlyList<ItemSolicitadoDto> itens, CancellationToken ct = default) =>
        PrecificarAsync(Agrupar(itens), ct);

    public async Task<CarrinhoDto> ObterAsync(string clienteId, CancellationToken ct = default)
    {
        var carrinho = await db.Carrinhos.AsNoTracking().FirstOrDefaultAsync(c => c.ClienteId == clienteId, ct);
        return await PrecificarAsync(Itens(carrinho), ct);
    }

    public async Task<CarrinhoDto> DefinirQuantidadeAsync(
        string clienteId, int produtoId, int quantidade, CancellationToken ct = default)
    {
        if (quantidade > 0)
        {
            var produto = await db.Produtos.AsNoTracking()
                .Where(p => p.Id == produtoId && p.Ativo)
                .Select(p => new { p.Nome, p.Estoque })
                .FirstOrDefaultAsync(ct)
                ?? throw new DomainException("Produto não encontrado ou indisponível.");

            if (quantidade > produto.Estoque)
                throw new DomainException(produto.Estoque == 0
                    ? $"\"{produto.Nome}\" está esgotado."
                    : $"Só restam {produto.Estoque} unidade(s) de \"{produto.Nome}\".");
        }

        var carrinho = await CarregarOuCriarAsync(clienteId, ct);
        carrinho.DefinirQuantidade(produtoId, quantidade);
        await db.SaveChangesAsync(ct);
        return await PrecificarAsync(Itens(carrinho), ct);
    }

    public async Task<CarrinhoDto> MesclarAsync(
        string clienteId, IReadOnlyList<ItemSolicitadoDto> itens, CancellationToken ct = default)
    {
        var carrinho = await CarregarOuCriarAsync(clienteId, ct);

        // Só entram produtos que existem e estão ativos; a quantidade é limitada ao estoque.
        var solicitados = Agrupar(itens);
        var estoques = await db.Produtos.AsNoTracking()
            .Where(p => solicitados.Keys.Contains(p.Id) && p.Ativo)
            .ToDictionaryAsync(p => p.Id, p => p.Estoque, ct);

        carrinho.Mesclar(solicitados
            .Where(s => estoques.ContainsKey(s.Key))
            .Select(s => (s.Key, Math.Min(s.Value, estoques[s.Key] - carrinho.QuantidadeDe(s.Key)))));

        await db.SaveChangesAsync(ct);
        return await PrecificarAsync(Itens(carrinho), ct);
    }

    public async Task<CarrinhoDto> EsvaziarAsync(string clienteId, CancellationToken ct = default)
    {
        var carrinho = await db.Carrinhos.FirstOrDefaultAsync(c => c.ClienteId == clienteId, ct);
        if (carrinho is not null)
        {
            carrinho.Esvaziar();
            await db.SaveChangesAsync(ct);
        }
        return new CarrinhoDto([], 0, 0);
    }

    private async Task<Carrinho> CarregarOuCriarAsync(string clienteId, CancellationToken ct)
    {
        var carrinho = await db.Carrinhos.FirstOrDefaultAsync(c => c.ClienteId == clienteId, ct);
        if (carrinho is null)
        {
            carrinho = new Carrinho(clienteId);
            db.Carrinhos.Add(carrinho);
        }
        return carrinho;
    }

    private static Dictionary<int, int> Itens(Carrinho? carrinho) =>
        carrinho?.Itens.ToDictionary(i => i.ProdutoId, i => i.Quantidade) ?? [];

    private static Dictionary<int, int> Agrupar(IEnumerable<ItemSolicitadoDto> itens) =>
        itens.Where(i => i.Quantidade > 0)
            .GroupBy(i => i.ProdutoId)
            .ToDictionary(g => g.Key, g => Math.Min(g.Sum(i => i.Quantidade), Carrinho.QuantidadeMaximaPorItem));

    /// <summary>Monta o carrinho com preço, nome e estoque atuais, explicando qualquer ajuste.</summary>
    private async Task<CarrinhoDto> PrecificarAsync(Dictionary<int, int> quantidades, CancellationToken ct)
    {
        if (quantidades.Count == 0)
            return new CarrinhoDto([], 0, 0);

        var produtos = await db.Produtos.AsNoTracking()
            .Where(p => quantidades.Keys.Contains(p.Id))
            .Select(p => new { p.Id, p.Nome, p.Slug, CategoriaSlug = p.Categoria.Slug, p.ImagemUrl, p.Preco, p.Estoque, p.Ativo })
            .ToListAsync(ct);

        var itens = produtos
            .OrderBy(p => p.Nome)
            .Select(p =>
            {
                var quantidade = quantidades[p.Id];
                var aviso = !p.Ativo || p.Estoque == 0 ? "Produto esgotado ou indisponível."
                    : quantidade > p.Estoque ? $"Só restam {p.Estoque} unidade(s). Ajuste a quantidade."
                    : null;
                return new ItemCarrinhoDto(
                    p.Id, p.Nome, p.Slug, p.CategoriaSlug, p.ImagemUrl, p.Preco,
                    quantidade, p.Estoque, p.Preco * quantidade, aviso);
            })
            .ToList();

        return new CarrinhoDto(itens, itens.Sum(i => i.Quantidade), itens.Sum(i => i.Subtotal));
    }
}
