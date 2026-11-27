using Loja.Application.Admin;
using Loja.Application.Catalogo;
using Loja.Application.Pedidos;
using Loja.Domain.Catalogo;
using Loja.Domain.Comum;
using Loja.Domain.Pedidos;
using Loja.Infrastructure.Identidade;
using Loja.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Loja.Infrastructure.Servicos;

public class AdminServico(
    LojaDbContext db, UserManager<Usuario> usuarios, TimeProvider relogio, ILogger<AdminServico> log) : IAdminServico
{
    private const int TamanhoPagina = 20;
    private const int LimiteEstoqueBaixo = 5;
    private static readonly StatusPedido[] StatusPagos = [StatusPedido.Pago, StatusPedido.Enviado, StatusPedido.Entregue];

    // ---------- Dashboard ----------

    public async Task<DashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var desde = relogio.GetUtcNow().UtcDateTime.AddDays(-30);

        // Total = frete + soma dos itens, calculado no banco (o Total da entidade não é coluna).
        var pagos = await db.Pedidos.AsNoTracking()
            .Where(p => StatusPagos.Contains(p.Status))
            .Select(p => new { p.CriadoEm, Total = p.ValorFrete + p.Itens.Sum(i => i.PrecoUnitario * i.Quantidade) })
            .ToListAsync(ct);

        var porStatus = await db.Pedidos.AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, Quantidade = g.Count() })
            .ToListAsync(ct);

        var estoqueBaixo = await db.Produtos.AsNoTracking()
            .Where(p => p.Ativo && p.Estoque <= LimiteEstoqueBaixo)
            .OrderBy(p => p.Estoque).ThenBy(p => p.Nome)
            .Select(p => new ProdutoEstoqueDto(p.Id, p.Nome, p.Estoque))
            .ToListAsync(ct);

        // Agrupa e ordena em tipos simples (traduzíveis para SQL) e só depois monta o DTO.
        var maisVendidos = (await db.Pedidos.AsNoTracking()
                .Where(p => StatusPagos.Contains(p.Status))
                .SelectMany(p => p.Itens)
                .GroupBy(i => i.ProdutoId)
                .Select(g => new
                {
                    ProdutoId = g.Key,
                    Nome = g.Max(i => i.NomeProduto),
                    Quantidade = g.Sum(i => i.Quantidade),
                    Receita = g.Sum(i => i.PrecoUnitario * i.Quantidade),
                })
                .OrderByDescending(m => m.Quantidade).ThenByDescending(m => m.Receita)
                .Take(5)
                .ToListAsync(ct))
            .Select(m => new MaisVendidoDto(m.ProdutoId, m.Nome ?? "", m.Quantidade, m.Receita))
            .ToList();

        var faturamento = pagos.Sum(p => p.Total);
        return new DashboardDto(
            faturamento,
            pagos.Where(p => p.CriadoEm >= desde).Sum(p => p.Total),
            pagos.Count == 0 ? 0 : decimal.Round(faturamento / pagos.Count, 2),
            porStatus.FirstOrDefault(s => s.Key == StatusPedido.Pago)?.Quantidade ?? 0,
            Enum.GetValues<StatusPedido>().ToDictionary(s => s.ToString(), s => porStatus.FirstOrDefault(x => x.Key == s)?.Quantidade ?? 0),
            estoqueBaixo,
            maisVendidos);
    }

    // ---------- Produtos ----------

    public async Task<Pagina<ProdutoAdminDto>> ListarProdutosAsync(string? busca, int pagina, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        var consulta = db.Produtos.AsNoTracking(); // inclui inativos, ao contrário da vitrine
        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(p => p.Nome.Contains(busca.Trim()));

        var total = await consulta.CountAsync(ct);
        var itens = await consulta.OrderBy(p => p.Nome).ThenBy(p => p.Id)
            .Skip((pagina - 1) * TamanhoPagina).Take(TamanhoPagina)
            .Select(p => new ProdutoAdminDto(p.Id, p.Nome, p.Slug, p.Descricao, p.Preco, p.Estoque, p.Ativo, p.CategoriaId, p.Categoria.Nome, p.ImagemUrl))
            .ToListAsync(ct);
        return new Pagina<ProdutoAdminDto>(itens, pagina, TamanhoPagina, total);
    }

    public Task<ProdutoAdminDto?> ObterProdutoAsync(int id, CancellationToken ct = default) =>
        db.Produtos.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new ProdutoAdminDto(p.Id, p.Nome, p.Slug, p.Descricao, p.Preco, p.Estoque, p.Ativo, p.CategoriaId, p.Categoria.Nome, p.ImagemUrl))
            .FirstOrDefaultAsync(ct);

    public async Task<ProdutoAdminDto> CriarProdutoAsync(SalvarProdutoDto dados, CancellationToken ct = default)
    {
        await GarantirCategoriaAsync(dados.CategoriaId, ct);
        var produto = new Produto(dados.Nome, dados.Descricao ?? "", dados.Preco, dados.EstoqueInicial, dados.CategoriaId, dados.ImagemUrl);
        await GarantirSlugLivreAsync(produto.Slug, null, ct);

        db.Produtos.Add(produto);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Produto {ProdutoId} ({Nome}) criado pelo painel.", produto.Id, produto.Nome);
        return (await ObterProdutoAsync(produto.Id, ct))!;
    }

    public async Task<ProdutoAdminDto?> AtualizarProdutoAsync(int id, SalvarProdutoDto dados, CancellationToken ct = default)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (produto is null) return null;

        await GarantirCategoriaAsync(dados.CategoriaId, ct);
        produto.Atualizar(dados.Nome, dados.Descricao ?? "", dados.CategoriaId, dados.ImagemUrl);
        produto.DefinirPreco(dados.Preco); // pedidos antigos guardam o preço da época, então não mudam
        await GarantirSlugLivreAsync(produto.Slug, id, ct);

        await db.SaveChangesAsync(ct);
        return await ObterProdutoAsync(id, ct);
    }

    public async Task<ProdutoAdminDto?> ReporEstoqueAsync(int id, int quantidade, CancellationToken ct = default)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (produto is null) return null;

        produto.ReporEstoque(quantidade);
        await db.SaveChangesAsync(ct); // se uma compra mudou o estoque ao mesmo tempo, o RowVersion gera 409
        return await ObterProdutoAsync(id, ct);
    }

    public async Task<ProdutoAdminDto?> DefinirAtivoAsync(int id, bool ativo, CancellationToken ct = default)
    {
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (produto is null) return null;

        if (ativo) produto.Ativar(); else produto.Desativar();
        await db.SaveChangesAsync(ct);
        return await ObterProdutoAsync(id, ct);
    }

    private async Task GarantirCategoriaAsync(int categoriaId, CancellationToken ct)
    {
        if (!await db.Categorias.AnyAsync(c => c.Id == categoriaId, ct))
            throw new DomainException("Categoria não encontrada.");
    }

    private async Task GarantirSlugLivreAsync(string slug, int? idAtual, CancellationToken ct)
    {
        if (await db.Produtos.AnyAsync(p => p.Slug == slug && p.Id != idAtual, ct))
            throw new DomainException("Já existe um produto com esse nome.");
    }

    // ---------- Pedidos ----------

    public async Task<Pagina<PedidoAdminResumoDto>> ListarPedidosAsync(string? status, int pagina, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        var consulta = db.Pedidos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusPedido>(status, ignoreCase: true, out var filtro))
                throw new DomainException("Status de pedido inválido.");
            consulta = consulta.Where(p => p.Status == filtro);
        }

        var total = await consulta.CountAsync(ct);
        var itens = await (
                from p in consulta
                join u in db.Users on p.ClienteId equals u.Id
                orderby p.CriadoEm descending
                select new PedidoAdminResumoDto(
                    p.Id, p.CriadoEm, p.Status.ToString(), u.Nome, u.Email!,
                    p.Itens.Sum(i => i.Quantidade),
                    p.ValorFrete + p.Itens.Sum(i => i.PrecoUnitario * i.Quantidade)))
            .Skip((pagina - 1) * TamanhoPagina).Take(TamanhoPagina)
            .ToListAsync(ct);
        return new Pagina<PedidoAdminResumoDto>(itens, pagina, TamanhoPagina, total);
    }

    public async Task<PedidoDetalheDto?> ObterPedidoAsync(Guid id, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return pedido is null ? null : PedidoMapeamento.ParaDetalhe(pedido);
    }

    /// <summary>Avança o pedido pela máquina de estados do domínio: enviar, entregar ou cancelar.</summary>
    public async Task<PedidoDetalheDto?> AvancarPedidoAsync(Guid id, string acao, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return null;

        switch (acao)
        {
            case "enviar": pedido.Enviar(); break;
            case "entregar": pedido.ConfirmarEntrega(); break;
            case "cancelar":
                var estavaPago = pedido.Status == StatusPedido.Pago;
                pedido.Cancelar(); // o domínio só permite antes do envio
                await DevolverEstoqueAsync(pedido, ct);
                if (estavaPago)
                    log.LogWarning("Pedido pago {PedidoId} cancelado pelo painel: estorno do pagamento {PagamentoId} deve ser feito no gateway.",
                        pedido.Id, pedido.PagamentoExternoId);
                break;
            default:
                throw new DomainException("Ação inválida. Use enviar, entregar ou cancelar.");
        }

        await db.SaveChangesAsync(ct);
        return PedidoMapeamento.ParaDetalhe(pedido);
    }

    private async Task DevolverEstoqueAsync(Pedido pedido, CancellationToken ct)
    {
        var ids = pedido.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await db.Produtos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        foreach (var item in pedido.Itens)
            if (produtos.TryGetValue(item.ProdutoId, out var produto))
                produto.ReporEstoque(item.Quantidade);
    }

    // ---------- Clientes ----------

    public async Task<Pagina<ClienteAdminDto>> ListarClientesAsync(string? busca, int pagina, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        var consulta = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            consulta = consulta.Where(u => u.Nome.Contains(termo) || u.Email!.Contains(termo));
        }

        var total = await consulta.CountAsync(ct);
        var usuariosPagina = await consulta.OrderByDescending(u => u.CriadoEm)
            .Skip((pagina - 1) * TamanhoPagina).Take(TamanhoPagina)
            .ToListAsync(ct);

        var ids = usuariosPagina.Select(u => u.Id).ToList();
        var perfis = await (from ur in db.UserRoles
                            join r in db.Roles on ur.RoleId equals r.Id
                            where ids.Contains(ur.UserId)
                            select new { ur.UserId, r.Name }).ToListAsync(ct);
        var pedidos = await db.Pedidos.AsNoTracking()
            .Where(p => ids.Contains(p.ClienteId))
            .GroupBy(p => p.ClienteId)
            .Select(g => new { g.Key, Quantidade = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Quantidade, ct);

        var agora = relogio.GetUtcNow();
        var itens = usuariosPagina.Select(u => new ClienteAdminDto(
                u.Id, u.Nome, u.Email!, u.CriadoEm,
                perfis.Where(p => p.UserId == u.Id).Select(p => p.Name!).ToList(),
                u.LockoutEnd > agora ? u.LockoutEnd : null, // só mostra bloqueios ainda em vigor
                u.AccessFailedCount,
                pedidos.GetValueOrDefault(u.Id)))
            .ToList();
        return new Pagina<ClienteAdminDto>(itens, pagina, TamanhoPagina, total);
    }

    public async Task<bool> DesbloquearClienteAsync(string id, CancellationToken ct = default)
    {
        var usuario = await usuarios.FindByIdAsync(id);
        if (usuario is null) return false;

        await usuarios.SetLockoutEndDateAsync(usuario, null);
        await usuarios.ResetAccessFailedCountAsync(usuario);
        log.LogInformation("Conta {UsuarioId} desbloqueada pelo painel.", id);
        return true;
    }
}
