using System.Security.Cryptography;
using System.Text;

namespace Loja.Infrastructure.Pagamentos;

/// <summary>
/// Confere se uma notificação veio mesmo do Mercado Pago. O cabeçalho x-signature traz
/// "ts=...,v1=...", onde v1 é um HMAC-SHA256 do texto
/// "id:[data.id];request-id:[x-request-id];ts:[ts];" com a assinatura secreta da aplicação.
/// </summary>
public static class AssinaturaWebhookMercadoPago
{
    public static bool Valida(string? xSignature, string? xRequestId, string? dataId, string segredo)
    {
        if (string.IsNullOrWhiteSpace(xSignature) || string.IsNullOrWhiteSpace(segredo))
            return false;

        string? ts = null, v1 = null;
        foreach (var parte in xSignature.Split(','))
        {
            var chaveValor = parte.Split('=', 2, StringSplitOptions.TrimEntries);
            if (chaveValor.Length != 2) continue;
            if (chaveValor[0] == "ts") ts = chaveValor[1];
            if (chaveValor[0] == "v1") v1 = chaveValor[1];
        }
        if (ts is null || v1 is null)
            return false;

        var esperado = Calcular(ts, xRequestId, dataId, segredo);
        // Comparação em tempo constante: não vaza, pelo tempo de resposta, quantos caracteres batem.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(esperado), Encoding.ASCII.GetBytes(v1.ToLowerInvariant()));
    }

    /// <summary>Monta o texto assinado (omitindo partes ausentes, como manda a documentação) e calcula o HMAC.</summary>
    public static string Calcular(string ts, string? xRequestId, string? dataId, string segredo)
    {
        var manifesto = new StringBuilder();
        if (!string.IsNullOrEmpty(dataId)) manifesto.Append($"id:{dataId.ToLowerInvariant()};");
        if (!string.IsNullOrEmpty(xRequestId)) manifesto.Append($"request-id:{xRequestId};");
        manifesto.Append($"ts:{ts};");

        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), Encoding.UTF8.GetBytes(manifesto.ToString()));
        return Convert.ToHexStringLower(hash);
    }
}
