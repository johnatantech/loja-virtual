namespace Loja.Domain.Pedidos;

public enum StatusPedido
{
    AguardandoPagamento = 0,
    Pago = 1,
    Enviado = 2,
    Entregue = 3,
    Cancelado = 4,
}
