namespace Loja.Application.Admin;

public record DashboardDto(
    decimal FaturamentoTotal, decimal FaturamentoUltimos30Dias, decimal TicketMedio, int PedidosAguardandoEnvio,
    IReadOnlyDictionary<string, int> PedidosPorStatus,
    IReadOnlyList<ProdutoEstoqueDto> EstoqueBaixo,
    IReadOnlyList<MaisVendidoDto> MaisVendidos);

public record ProdutoEstoqueDto(int Id, string Nome, int Estoque);
public record MaisVendidoDto(int ProdutoId, string Nome, int Quantidade, decimal Receita);

public record ProdutoAdminDto(
    int Id, string Nome, string Slug, string Descricao, decimal Preco, int Estoque, bool Ativo,
    int CategoriaId, string Categoria, string? ImagemUrl);

public record SalvarProdutoDto(
    string Nome, string? Descricao, decimal Preco, int CategoriaId, string? ImagemUrl, int EstoqueInicial = 0);

public record PedidoAdminResumoDto(
    Guid Id, DateTime CriadoEm, string Status, string ClienteNome, string ClienteEmail, int QuantidadeItens, decimal Total);

public record ClienteAdminDto(
    string Id, string Nome, string Email, DateTime CriadoEm, IReadOnlyList<string> Perfis,
    DateTimeOffset? BloqueadoAte, int TentativasFalhas, int QuantidadePedidos);
