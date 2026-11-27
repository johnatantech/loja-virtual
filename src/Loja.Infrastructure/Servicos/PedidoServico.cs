using Loja.Application.Pedidos;
using Loja.Domain.Comum;
using Loja.Domain.Entrega;
using Loja.Domain.Pedidos;
using Loja.Infrastructure.Pagamentos;
using Loja.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Loja.Infrastructure.Servicos;

public class PedidoServico(
    LojaDbContext db, IGatewayPagamento gateway, TimeProvider relogio, ILogger<PedidoServico> log) : IPedidoServico
{
    private const int TentativasPorConflito = 5;

    public async Task<CotacaoFreteDto> CotarFreteAsync(string clienteId, string cep, CancellationToken ct = default)
    {
        var destino = Cep.Criar(cep);
        var subtotal = await SubtotalDoCarrinhoAsync(clienteId, ct);
        var frete = CalculadoraFrete.Cotar(destino, subtotal);
        return new CotacaoFreteDto(destino.Uf, subtotal, frete.Valor, frete.PrazoDias, frete.Gratis, subtotal + frete.Valor);
    }

    public async Task<PedidoCriadoDto> CriarAsync(string clienteId, EnderecoDto dados, CancellationToken ct = default)
    {
        var cep = Cep.Criar(dados.Cep);
        var endereco = EnderecoEntrega.Criar(
            dados.Destinatario, cep, dados.Logradouro, dados.Numero, dados.Complemento, dados.Bairro, dados.Cidade);

        var pedido = await ComRepeticaoEmConflitoAsync(async () =>
        {
            var carrinho = await db.Carrinhos.FirstOrDefaultAsync(c => c.ClienteId == clienteId, ct);
            if (carrinho is null || carrinho.Itens.Count == 0)
                throw new DomainException("Seu carrinho está vazio.");

            var ids = carrinho.Itens.Select(i => i.ProdutoId).ToList();
            var produtos = await db.Produtos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

            var novo = new Pedido(clienteId);
            foreach (var item in carrinho.Itens)
            {
                if (!produtos.TryGetValue(item.ProdutoId, out var produto))
                    throw new DomainException("Um produto do carrinho não existe mais. Revise o carrinho.");
                novo.AdicionarItem(produto, item.Quantidade); // valida produto ativo
                produto.BaixarEstoque(item.Quantidade);    // valida estoque disponível
            }
            novo.DefinirEntrega(endereco, CalculadoraFrete.Cotar(cep, novo.Subtotal));

            db.Pedidos.Add(novo);
            carrinho.Esvaziar();
            // Pedido, baixa de estoque e carrinho vazio são gravados juntos ou nada é gravado.
            // Se outra compra alterou o mesmo produto nesse meio-tempo, o RowVersion detecta
            // o conflito e a operação é refeita com o estoque atualizado.
            await db.SaveChangesAsync(ct);
            return novo;
        }, ct);

        await TentarGerarCobrancaAsync(pedido, ct);
        return new PedidoCriadoDto(pedido.Id, pedido.LinkPagamento);
    }

    public async Task<IReadOnlyList<PedidoResumoDto>> ListarAsync(string clienteId, CancellationToken ct = default)
    {
        var pedidos = await db.Pedidos.AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync(ct);
        return pedidos
            .Select(p => new PedidoResumoDto(p.Id, p.CriadoEm, p.Status.ToString(), p.Itens.Sum(i => i.Quantidade), p.Total))
            .ToList();
    }

    public async Task<PedidoDetalheDto?> ObterAsync(string clienteId, Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pedidoId && p.ClienteId == clienteId, ct);
        return pedido is null ? null : Detalhe(pedido);
    }

    public async Task<PedidoDetalheDto?> CancelarAsync(string clienteId, Guid pedidoId, CancellationToken ct = default)
    {
        return await ComRepeticaoEmConflitoAsync(async () =>
        {
            var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId && p.ClienteId == clienteId, ct);
            if (pedido is null)
                return null;
            if (pedido.Status != StatusPedido.AguardandoPagamento)
                throw new DomainException("Só é possível cancelar pedidos que ainda aguardam pagamento.");

            pedido.Cancelar();
            var ids = pedido.Itens.Select(i => i.ProdutoId).ToList();
            var produtos = await db.Produtos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
            foreach (var item in pedido.Itens)
                if (produtos.TryGetValue(item.ProdutoId, out var produto))
                    produto.ReporEstoque(item.Quantidade); // devolve ao estoque

            await db.SaveChangesAsync(ct);
            return Detalhe(pedido);
        }, ct);
    }

    public async Task<PedidoCriadoDto?> GerarPagamentoAsync(string clienteId, Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId && p.ClienteId == clienteId, ct);
        if (pedido is null)
            return null;
        if (pedido.Status != StatusPedido.AguardandoPagamento)
            throw new DomainException("Este pedido não aguarda pagamento.");

        await TentarGerarCobrancaAsync(pedido, ct);
        if (pedido.LinkPagamento is null)
            throw new DomainException("O serviço de pagamento está indisponível. Tente novamente em instantes.");
        return new PedidoCriadoDto(pedido.Id, pedido.LinkPagamento);
    }

    public async Task<bool> ProcessarPagamentoAsync(string pagamentoId, CancellationToken ct = default)
    {
        // Nunca confia no conteúdo da notificação: busca o pagamento direto no gateway.
        var pagamento = await gateway.ConsultarPagamentoAsync(pagamentoId, ct);
        if (pagamento is null || pagamento.PedidoId is null)
        {
            log.LogWarning("Pagamento {PagamentoId} não encontrado ou sem pedido vinculado.", pagamentoId);
            return false;
        }
        return await AplicarPagamentoAsync(pagamento, ct);
    }

    public async Task<PedidoDetalheDto?> SimularPagamentoAsync(
        string clienteId, Guid pedidoId, bool aprovar, CancellationToken ct = default)
    {
        if (!gateway.EhSimulado)
            return null;

        var pedido = await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pedidoId && p.ClienteId == clienteId, ct);
        if (pedido is null)
            return null;

        if (aprovar)
        {
            await AplicarPagamentoAsync(new PagamentoGateway(
                GatewaySimulado.IdPagamento(pedido.Id), SituacaoPagamento.Aprovado, pedido.Id, pedido.Total), ct);
        }
        return await ObterAsync(clienteId, pedidoId, ct);
    }

    private async Task<bool> AplicarPagamentoAsync(PagamentoGateway pagamento, CancellationToken ct)
    {
        if (pagamento.Situacao != SituacaoPagamento.Aprovado)
        {
            log.LogInformation("Pagamento {PagamentoId} com situação {Situacao}; pedido segue aguardando.",
                pagamento.Id, pagamento.Situacao);
            return false;
        }

        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == pagamento.PedidoId, ct);
        if (pedido is null)
        {
            log.LogWarning("Pagamento {PagamentoId} aponta para o pedido {PedidoId}, que não existe.", pagamento.Id, pagamento.PedidoId);
            return false;
        }

        try
        {
            var mudou = pedido.RegistrarPagamentoAprovado(pagamento.Id, pagamento.Valor, relogio.GetUtcNow().UtcDateTime);
            if (!mudou)
            {
                log.LogInformation("Pagamento {PagamentoId} já processado; notificação repetida ignorada.", pagamento.Id);
                return false;
            }
            await db.SaveChangesAsync(ct);
            log.LogInformation("Pedido {PedidoId} pago (pagamento {PagamentoId}).", pedido.Id, pagamento.Id);
            return true;
        }
        catch (DomainException ex)
        {
            // Ex.: pagamento aprovado de um pedido já cancelado. Exige estorno manual, então fica registrado.
            log.LogError("Pagamento {PagamentoId} do pedido {PedidoId} não pôde ser aplicado: {Motivo}",
                pagamento.Id, pedido.Id, ex.Message);
            return false;
        }
    }

    private async Task TentarGerarCobrancaAsync(Pedido pedido, CancellationToken ct)
    {
        try
        {
            pedido.DefinirLinkPagamento(await gateway.CriarCobrancaAsync(pedido, ct));
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            // O pedido já está salvo e o estoque reservado; o cliente pode gerar o pagamento depois.
            log.LogError(ex, "Falha ao gerar a cobrança do pedido {PedidoId}.", pedido.Id);
        }
    }

    private async Task<decimal> SubtotalDoCarrinhoAsync(string clienteId, CancellationToken ct)
    {
        var carrinho = await db.Carrinhos.AsNoTracking().FirstOrDefaultAsync(c => c.ClienteId == clienteId, ct);
        if (carrinho is null || carrinho.Itens.Count == 0)
            return 0;
        var quantidades = carrinho.Itens.ToDictionary(i => i.ProdutoId, i => i.Quantidade);
        var precos = await db.Produtos.AsNoTracking()
            .Where(p => quantidades.Keys.Contains(p.Id))
            .Select(p => new { p.Id, p.Preco })
            .ToListAsync(ct);
        return precos.Sum(p => p.Preco * quantidades[p.Id]);
    }

    /// <summary>Refaz a operação do zero se outra transação alterou os mesmos registros (concorrência otimista).</summary>
    private async Task<T> ComRepeticaoEmConflitoAsync<T>(Func<Task<T>> operacao, CancellationToken ct)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                return await operacao();
            }
            catch (DbUpdateConcurrencyException) when (tentativa < TentativasPorConflito)
            {
                log.LogInformation("Conflito de concorrência no estoque; tentativa {Tentativa} de {Total}.",
                    tentativa + 1, TentativasPorConflito);
                db.ChangeTracker.Clear();
                // Espera curta e aleatória, para as requisições concorrentes não colidirem de novo em sincronia.
                await Task.Delay(Random.Shared.Next(10, 60) * tentativa, ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DomainException("Muitas compras simultâneas deste produto. Tente novamente em instantes.");
            }
        }
    }

    private static PedidoDetalheDto Detalhe(Pedido p) => PedidoMapeamento.ParaDetalhe(p);
}
