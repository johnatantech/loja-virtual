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
}
