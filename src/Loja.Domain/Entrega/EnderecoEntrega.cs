using Loja.Domain.Comum;

namespace Loja.Domain.Entrega;

/// <summary>Endereço gravado no pedido: se o cliente mudar de endereço depois, o pedido não muda.</summary>
public record EnderecoEntrega
{
    public string Destinatario { get; init; } = "";
    public string Cep { get; init; } = "";
    public string Logradouro { get; init; } = "";
    public string Numero { get; init; } = "";
    public string? Complemento { get; init; }
    public string Bairro { get; init; } = "";
    public string Cidade { get; init; } = "";
    public string Uf { get; init; } = "";

    private EnderecoEntrega() { } // EF Core

    public static EnderecoEntrega Criar(
        string destinatario, Cep cep, string logradouro, string numero,
        string? complemento, string bairro, string cidade)
    {
        static string Obrigatorio(string? valor, string campo, int maximo)
        {
            var texto = valor?.Trim() ?? "";
            if (texto.Length == 0 || texto.Length > maximo)
                throw new DomainException($"Informe {campo} (até {maximo} caracteres).");
            return texto;
        }

        var comp = complemento?.Trim();
        if (comp?.Length > 60)
            throw new DomainException("O complemento pode ter até 60 caracteres.");

        return new EnderecoEntrega
        {
            Destinatario = Obrigatorio(destinatario, "o nome de quem vai receber", 100),
            Cep = cep.Numero,
            Logradouro = Obrigatorio(logradouro, "a rua", 150),
            Numero = Obrigatorio(numero, "o número", 20),
            Complemento = string.IsNullOrEmpty(comp) ? null : comp,
            Bairro = Obrigatorio(bairro, "o bairro", 80),
            Cidade = Obrigatorio(cidade, "a cidade", 80),
            Uf = cep.Uf, // sempre a UF do CEP, nunca a digitada
        };
    }
}
