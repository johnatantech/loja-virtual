using Loja.Domain.Catalogo;
using Loja.Domain.Comum;
using Loja.Domain.Entrega;

namespace Loja.Domain.Pedidos;

public class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string ClienteId { get; private set; } = null!;
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;
    public StatusPedido Status { get; private set; } = StatusPedido.AguardandoPagamento;
    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    public EnderecoEntrega? Endereco { get; private set; }
    public decimal ValorFrete { get; private set; }
    public int PrazoEntregaDias { get; private set; }

    /// <summary>Link do gateway onde o cliente paga este pedido.</summary>
    public string? LinkPagamento { get; private set; }
    /// <summary>Id do pagamento no gateway; também garante que um pagamento só confirme um pedido.</summary>
    public string? PagamentoExternoId { get; private set; }
    public DateTime? PagoEm { get; private set; }

    public decimal Subtotal => _itens.Sum(i => i.Subtotal);
    public decimal Total => Subtotal + ValorFrete;

    private Pedido() { } // EF Core

    public Pedido(string clienteId)
    {
        if (string.IsNullOrWhiteSpace(clienteId))
            throw new DomainException("O pedido precisa de um cliente.");
        ClienteId = clienteId;
    }

    public void AdicionarItem(Produto produto, int quantidade)
    {
        GarantirStatus(StatusPedido.AguardandoPagamento, "alterar os itens");
        if (!produto.Ativo)
            throw new DomainException($"\"{produto.Nome}\" não está disponível para venda.");
        if (quantidade <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");

        var existente = _itens.FirstOrDefault(i => i.ProdutoId == produto.Id);
        if (existente is null)
            _itens.Add(new ItemPedido(produto.Id, produto.Nome, produto.Preco, quantidade));
        else
            existente.Somar(quantidade);
    }

    public void DefinirEntrega(EnderecoEntrega endereco, CotacaoFrete frete)
    {
        GarantirStatus(StatusPedido.AguardandoPagamento, "alterar a entrega");
        Endereco = endereco;
        ValorFrete = frete.Valor;
        PrazoEntregaDias = frete.PrazoDias;
    }

    public void DefinirLinkPagamento(string link)
    {
        GarantirStatus(StatusPedido.AguardandoPagamento, "gerar o pagamento");
        LinkPagamento = link;
    }

    /// <summary>
    /// Registra um pagamento aprovado pelo gateway. É idempotente: a mesma notificação
    /// chegando duas vezes não confirma o pedido em dobro. Retorna false se nada mudou.
    /// </summary>
    public bool RegistrarPagamentoAprovado(string pagamentoId, decimal valorPago, DateTime quando)
    {
        if (Status == StatusPedido.Pago && PagamentoExternoId == pagamentoId)
            return false;
        if (valorPago != Total)
            throw new DomainException($"Valor pago ({valorPago:N2}) diferente do total do pedido ({Total:N2}).");

        ConfirmarPagamento();
        PagamentoExternoId = pagamentoId;
        PagoEm = quando;
        return true;
    }

    // Máquina de estados: cada transição só é válida a partir de um status específico.
    public void ConfirmarPagamento()
    {
        GarantirStatus(StatusPedido.AguardandoPagamento, "confirmar o pagamento");
        if (_itens.Count == 0)
            throw new DomainException("Não é possível pagar um pedido sem itens.");
        Status = StatusPedido.Pago;
    }

    public void Enviar()
    {
        GarantirStatus(StatusPedido.Pago, "enviar");
        Status = StatusPedido.Enviado;
    }

    public void ConfirmarEntrega()
    {
        GarantirStatus(StatusPedido.Enviado, "confirmar a entrega");
        Status = StatusPedido.Entregue;
    }

    public void Cancelar()
    {
        if (Status is StatusPedido.Enviado or StatusPedido.Entregue or StatusPedido.Cancelado)
            throw new DomainException($"Um pedido com status {Status} não pode ser cancelado.");
        Status = StatusPedido.Cancelado;
    }

    private void GarantirStatus(StatusPedido esperado, string acao)
    {
        if (Status != esperado)
            throw new DomainException($"Não é possível {acao} de um pedido com status {Status}.");
    }
}
