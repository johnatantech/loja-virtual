using Loja.Domain.Carrinhos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loja.Infrastructure.Persistencia.Configuracoes;

public class CarrinhoConfiguracao : IEntityTypeConfiguration<Carrinho>
{
    public void Configure(EntityTypeBuilder<Carrinho> builder)
    {
        builder.ToTable("Carrinhos");
        builder.Property(c => c.ClienteId).HasMaxLength(450).IsRequired();
        builder.HasIndex(c => c.ClienteId).IsUnique(); // um carrinho por cliente

        builder.OwnsMany(c => c.Itens, item =>
        {
            item.ToTable("ItensCarrinho", t =>
                t.HasCheckConstraint("CK_ItensCarrinho_Quantidade", "\"Quantidade\" > 0"));
            item.WithOwner().HasForeignKey("CarrinhoId");
            item.HasKey(i => i.Id);
            item.HasIndex("CarrinhoId", nameof(ItemCarrinho.ProdutoId)).IsUnique();
        });
        builder.Navigation(c => c.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
