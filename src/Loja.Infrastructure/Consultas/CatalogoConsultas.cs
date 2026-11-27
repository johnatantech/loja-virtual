using Loja.Application.Catalogo;
using Loja.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Loja.Infrastructure.Consultas;

public class CatalogoConsultas(LojaDbContext db) : ICatalogoConsultas
{
    public async Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct = default) =>
        await db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaDto(
                c.Id, c.Nome, c.Slug,
                db.Produtos.Count(p => p.CategoriaId == c.Id && p.Ativo)))
            .ToListAsync(ct);

    public async Task<Pagina<ProdutoResumoDto>> ListarProdutosAsync(FiltroProdutos filtro, CancellationToken ct = default)
    {
        var consulta = db.Produtos.AsNoTracking().Where(p => p.Ativo);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            // A collation padrão do SQL Server ignora maiúsculas e minúsculas.
            var termo = filtro.Busca.Trim();
            consulta = consulta.Where(p => p.Nome.Contains(termo) || p.Descricao.Contains(termo));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
            consulta = consulta.Where(p => p.Categoria.Slug == filtro.Categoria);
        if (filtro.PrecoMin is not null)
            consulta = consulta.Where(p => p.Preco >= filtro.PrecoMin);
        if (filtro.PrecoMax is not null)
            consulta = consulta.Where(p => p.Preco <= filtro.PrecoMax);

        var total = await consulta.CountAsync(ct);

        consulta = filtro.Ordenacao switch
        {
            "menor-preco" => consulta.OrderBy(p => p.Preco).ThenBy(p => p.Id),
            "maior-preco" => consulta.OrderByDescending(p => p.Preco).ThenBy(p => p.Id),
            _ => consulta.OrderBy(p => p.Nome).ThenBy(p => p.Id),
        };

        var itens = await consulta
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .Select(p => new ProdutoResumoDto(
                p.Id, p.Nome, p.Slug, p.Preco, p.ImagemUrl,
                p.Categoria.Nome, p.Categoria.Slug, p.Estoque > 0))
            .ToListAsync(ct);

        return new Pagina<ProdutoResumoDto>(itens, filtro.Pagina, filtro.TamanhoPagina, total);
    }

    public Task<ProdutoDetalheDto?> ObterProdutoAsync(string slug, CancellationToken ct = default) =>
        db.Produtos
            .AsNoTracking()
            .Where(p => p.Slug == slug && p.Ativo)
            .Select(p => new ProdutoDetalheDto(
                p.Id, p.Nome, p.Slug, p.Descricao, p.Preco, p.ImagemUrl,
                p.Categoria.Nome, p.Categoria.Slug, p.Estoque))
            .FirstOrDefaultAsync(ct);
}
