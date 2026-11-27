using Loja.Domain.Pedidos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loja.Infrastructure.Persistencia.Configuracoes;

public class PedidoConfiguracao : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.Property(p => p.ClienteId).HasMaxLength(450).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(p => p.ClienteId);
        builder.Ignore(p => p.Total);

        builder.OwnsMany(p => p.Itens, item =>
        {
            item.ToTable("ItensPedido");
            item.WithOwner().HasForeignKey("PedidoId");
            item.HasKey(i => i.Id);
            item.Property(i => i.NomeProduto).HasMaxLength(150).IsRequired();
            item.Property(i => i.PrecoUnitario).HasPrecision(10, 2);
            item.Ignore(i => i.Subtotal);
        });
        builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
