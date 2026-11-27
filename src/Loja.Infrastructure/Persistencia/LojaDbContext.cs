using Loja.Domain.Carrinhos;
using Loja.Domain.Catalogo;
using Loja.Domain.Pedidos;
using Loja.Infrastructure.Identidade;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Loja.Infrastructure.Persistencia;

public class LojaDbContext(DbContextOptions<LojaDbContext> options)
    : IdentityDbContext<Usuario>(options), IDataProtectionKeyContext
{
    /// <summary>Chaves internas de criptografia do ASP.NET, guardadas no banco para sobreviver a reinícios e deploys.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<Carrinho> Carrinhos => Set<Carrinho>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuracao)
    {
        configuracao.Properties<DateTime>().HaveConversion<ConversorDataUtc>();
        configuracao.Properties<DateTime?>().HaveConversion<ConversorDataUtc>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // tabelas do Identity
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LojaDbContext).Assembly);
    }
}
