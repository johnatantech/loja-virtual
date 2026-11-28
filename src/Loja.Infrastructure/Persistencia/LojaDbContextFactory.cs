using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Loja.Infrastructure.Persistencia;

/// <summary>
/// Usada só pelo "dotnet ef" para gerar migrations. Não conecta ao banco,
/// por isso não precisa de credenciais.
/// </summary>
public class LojaDbContextFactory : IDesignTimeDbContextFactory<LojaDbContext>
{
    public LojaDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LojaDbContext>()
            .UseNpgsql("Host=design-time;Database=loja")
            .Options);
}
