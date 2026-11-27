using Loja.Domain.Comum;

namespace Loja.Domain.Catalogo;

public class Produto
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Descricao { get; private set; } = "";
    public decimal Preco { get; private set; }
    public int Estoque { get; private set; }
    public string? ImagemUrl { get; private set; }
    public bool Ativo { get; private set; } = true;
    public int CategoriaId { get; private set; }
    public Categoria Categoria { get; private set; } = null!;

    /// <summary>
    /// Controle de concorrência otimista: se duas compras alterarem o mesmo produto ao mesmo
    /// tempo, a segunda falha ao salvar em vez de vender um estoque que já não existe.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private Produto() { } // EF Core

    public Produto(string nome, string descricao, decimal preco, int estoque, int categoriaId, string? imagemUrl = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do produto é obrigatório.");
        if (estoque < 0)
            throw new DomainException("O estoque inicial não pode ser negativo.");

        Nome = nome.Trim();
        Slug = Comum.Slug.Gerar(Nome);
        Descricao = descricao.Trim();
        DefinirPreco(preco);
        Estoque = estoque;
        CategoriaId = categoriaId;
        ImagemUrl = imagemUrl;
    }

    public void DefinirPreco(decimal preco)
    {
        if (preco <= 0)
            throw new DomainException("O preço deve ser maior que zero.");
        Preco = decimal.Round(preco, 2);
    }

    public void ReporEstoque(int quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("A quantidade reposta deve ser maior que zero.");
        Estoque += quantidade;
    }

    public void BaixarEstoque(int quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");
        if (quantidade > Estoque)
            throw new DomainException($"Estoque insuficiente para \"{Nome}\": restam {Estoque} unidade(s).");
        Estoque -= quantidade;
    }

    public void Desativar() => Ativo = false;
    public void Ativar() => Ativo = true;
}
