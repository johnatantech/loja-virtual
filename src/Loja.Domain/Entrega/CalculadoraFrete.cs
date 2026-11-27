namespace Loja.Domain.Entrega;

public record CotacaoFrete(decimal Valor, int PrazoDias, bool Gratis);

/// <summary>
/// Frete simulado por região, como uma tabela de transportadora simplificada.
/// Compras a partir de R$ 299 têm frete grátis.
/// </summary>
public static class CalculadoraFrete
{
    public const decimal ValorParaFreteGratis = 299m;

    private static readonly Dictionary<string, (decimal Valor, int Prazo)> PorRegiao = new()
    {
        ["SP"] = (15.90m, 2),
        ["Sudeste"] = (22.90m, 4),
        ["Sul"] = (27.90m, 5),
        ["Centro-Oeste"] = (32.90m, 6),
        ["Nordeste"] = (39.90m, 8),
        ["Norte"] = (49.90m, 10),
    };

    public static CotacaoFrete Cotar(Cep cep, decimal subtotal)
    {
        var (valor, prazo) = PorRegiao[Regiao(cep.Uf)];
        return subtotal >= ValorParaFreteGratis
            ? new CotacaoFrete(0m, prazo, Gratis: true)
            : new CotacaoFrete(valor, prazo, Gratis: false);
    }

    public static string Regiao(string uf) => uf switch
    {
        "SP" => "SP",
        "RJ" or "MG" or "ES" => "Sudeste",
        "PR" or "SC" or "RS" => "Sul",
        "DF" or "GO" or "MT" or "MS" => "Centro-Oeste",
        "BA" or "SE" or "AL" or "PE" or "PB" or "RN" or "CE" or "PI" or "MA" => "Nordeste",
        _ => "Norte",
    };
}
