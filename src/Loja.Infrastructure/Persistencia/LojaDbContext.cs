using Loja.Domain.Carrinhos;
using Loja.Domain.Catalogo;
using Loja.Domain.Pedidos;
using Loja.Infrastructure.Identidade;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Loja.Infrastructure.Persistencia;

public class LojaDbContext(DbContextOptions<LojaDbContext> options) : IdentityDbContext<Usuario>(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<Carrinho> Carrinhos => Set<Carrinho>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // tabelas do Identity
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LojaDbContext).Assembly);
    }
}
