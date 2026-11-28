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

    private Produto() { } // EF Core

    public Produto(string nome, string descricao, decimal preco, int estoque, int categoriaId, string? imagemUrl = null)
    {
        if (estoque < 0)
            throw new DomainException("O estoque inicial não pode ser negativo.");

        DefinirDados(nome, descricao, categoriaId, imagemUrl);
        DefinirPreco(preco);
        Estoque = estoque;
    }

    /// <summary>Edição pelo painel administrativo. O slug acompanha o novo nome.</summary>
    public void Atualizar(string nome, string descricao, int categoriaId, string? imagemUrl) =>
        DefinirDados(nome, descricao, categoriaId, imagemUrl);

    private void DefinirDados(string nome, string descricao, int categoriaId, string? imagemUrl)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150)
            throw new DomainException("O nome do produto é obrigatório (até 150 caracteres).");
        if ((descricao?.Trim().Length ?? 0) > 2000)
            throw new DomainException("A descrição pode ter até 2.000 caracteres.");
        if (categoriaId <= 0)
            throw new DomainException("Escolha uma categoria.");

        var imagem = string.IsNullOrWhiteSpace(imagemUrl) ? null : imagemUrl.Trim();
        // Só aceita https: evita links "javascript:" e imagens servidas sem criptografia.
        if (imagem is not null && (!Uri.TryCreate(imagem, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || imagem.Length > 500))
            throw new DomainException("A imagem precisa ser um endereço https:// válido.");

        Nome = nome.Trim();
        Slug = Comum.Slug.Gerar(Nome);
        Descricao = descricao?.Trim() ?? "";
        CategoriaId = categoriaId;
        ImagemUrl = imagem;
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
