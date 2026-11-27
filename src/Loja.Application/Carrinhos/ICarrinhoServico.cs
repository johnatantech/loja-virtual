namespace Loja.Application.Carrinhos;

public interface ICarrinhoServico
{
    /// <summary>Precifica itens sem salvar nada: usado pelo carrinho de visitante.</summary>
    Task<CarrinhoDto> CotarAsync(IReadOnlyList<ItemSolicitadoDto> itens, CancellationToken ct = default);

    Task<CarrinhoDto> ObterAsync(string clienteId, CancellationToken ct = default);
    Task<CarrinhoDto> DefinirQuantidadeAsync(string clienteId, int produtoId, int quantidade, CancellationToken ct = default);
    Task<CarrinhoDto> MesclarAsync(string clienteId, IReadOnlyList<ItemSolicitadoDto> itens, CancellationToken ct = default);
    Task<CarrinhoDto> EsvaziarAsync(string clienteId, CancellationToken ct = default);
}
