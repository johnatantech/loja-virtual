using Loja.Domain.Comum;

namespace Loja.Domain.Catalogo;

public class Categoria
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Slug { get; private set; } = null!;

    private Categoria() { } // EF Core

    public Categoria(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da categoria é obrigatório.");
        Nome = nome.Trim();
        Slug = Comum.Slug.Gerar(Nome);
    }
}
