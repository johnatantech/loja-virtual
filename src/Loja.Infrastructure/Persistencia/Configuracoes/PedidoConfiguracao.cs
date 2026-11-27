using Loja.Domain.Pedidos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loja.Infrastructure.Persistencia.Configuracoes;

public class PedidoConfiguracao : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos", t => t.HasCheckConstraint("CK_Pedidos_ValorFrete", "[ValorFrete] >= 0"));
        builder.Property(p => p.ClienteId).HasMaxLength(450).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.ValorFrete).HasPrecision(10, 2);
        builder.Property(p => p.LinkPagamento).HasMaxLength(500);
        builder.Property(p => p.PagamentoExternoId).HasMaxLength(100);
        builder.HasIndex(p => p.ClienteId);
        // Um pagamento do gateway nunca confirma dois pedidos.
        builder.HasIndex(p => p.PagamentoExternoId).IsUnique().HasFilter("[PagamentoExternoId] IS NOT NULL");
        builder.Ignore(p => p.Subtotal);
        builder.Ignore(p => p.Total);

        builder.OwnsOne(p => p.Endereco, e =>
        {
            e.Property(x => x.Destinatario).HasColumnName("EntregaDestinatario").HasMaxLength(100);
            e.Property(x => x.Cep).HasColumnName("EntregaCep").HasMaxLength(8);
            e.Property(x => x.Logradouro).HasColumnName("EntregaLogradouro").HasMaxLength(150);
            e.Property(x => x.Numero).HasColumnName("EntregaNumero").HasMaxLength(20);
            e.Property(x => x.Complemento).HasColumnName("EntregaComplemento").HasMaxLength(60);
            e.Property(x => x.Bairro).HasColumnName("EntregaBairro").HasMaxLength(80);
            e.Property(x => x.Cidade).HasColumnName("EntregaCidade").HasMaxLength(80);
            e.Property(x => x.Uf).HasColumnName("EntregaUf").HasMaxLength(2);
        });

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
