using Loja.Domain.Comum;

namespace Loja.Domain.Entrega;

/// <summary>
/// CEP com a UF derivada das faixas dos Correios. O frete é calculado a partir daqui,
/// e não da UF digitada no formulário, então não dá para pagar menos informando outro estado.
/// </summary>
public readonly record struct Cep
{
    private static readonly (int Inicio, int Fim, string Uf)[] Faixas =
    [
        (01000, 19999, "SP"), (20000, 28999, "RJ"), (29000, 29999, "ES"), (30000, 39999, "MG"),
        (40000, 48999, "BA"), (49000, 49999, "SE"), (50000, 56999, "PE"), (57000, 57999, "AL"),
        (58000, 58999, "PB"), (59000, 59999, "RN"), (60000, 63999, "CE"), (64000, 64999, "PI"),
        (65000, 65999, "MA"), (66000, 68899, "PA"), (68900, 68999, "AP"), (69000, 69299, "AM"),
        (69300, 69399, "RR"), (69400, 69899, "AM"), (69900, 69999, "AC"), (70000, 72799, "DF"),
        (72800, 72999, "GO"), (73000, 73699, "DF"), (73700, 76799, "GO"), (76800, 76999, "RO"),
        (77000, 77999, "TO"), (78000, 78899, "MT"), (78900, 78999, "RO"), (79000, 79999, "MS"),
        (80000, 87999, "PR"), (88000, 89999, "SC"), (90000, 99999, "RS"),
    ];

    public string Numero { get; }
    public string Uf { get; }

    private Cep(string numero, string uf)
    {
        Numero = numero;
        Uf = uf;
    }

    public static Cep Criar(string? valor)
    {
        var digitos = new string((valor ?? "").Where(char.IsDigit).ToArray());
        if (digitos.Length != 8)
            throw new DomainException("Informe um CEP com 8 dígitos.");

        var prefixo = int.Parse(digitos[..5]);
        var faixa = Faixas.FirstOrDefault(f => prefixo >= f.Inicio && prefixo <= f.Fim);
        if (faixa.Uf is null)
            throw new DomainException("CEP inválido.");

        return new Cep(digitos, faixa.Uf);
    }

    public override string ToString() => $"{Numero[..5]}-{Numero[5..]}";
}
