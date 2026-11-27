using Loja.Domain.Catalogo;
using Loja.Domain.Pedidos;
using Microsoft.EntityFrameworkCore;

namespace Loja.Infrastructure.Persistencia;

public class LojaDbContext(DbContextOptions<LojaDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LojaDbContext).Assembly);
}
