using Loja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loja.Infrastructure.Persistencia.Configuracoes;

public class ProdutoConfiguracao : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos", t =>
        {
            // Regras repetidas no banco: protegem mesmo contra escritas fora da aplicação.
            t.HasCheckConstraint("CK_Produtos_Preco", "[Preco] > 0");
            t.HasCheckConstraint("CK_Produtos_Estoque", "[Estoque] >= 0");
        });
        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(180).IsRequired();
        builder.Property(p => p.Descricao).HasMaxLength(2000);
        builder.Property(p => p.Preco).HasPrecision(10, 2);
        builder.Property(p => p.ImagemUrl).HasMaxLength(500);
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasOne(p => p.Categoria).WithMany().HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
