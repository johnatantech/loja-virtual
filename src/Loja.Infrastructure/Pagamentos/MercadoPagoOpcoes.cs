namespace Loja.Infrastructure.Pagamentos;

public class MercadoPagoOpcoes
{
    public const string Secao = "MercadoPago";

    /// <summary>Access Token (de teste, começa com TEST-). Vem de user-secrets.</summary>
    public string AccessToken { get; set; } = "";

    /// <summary>Assinatura secreta dos webhooks, gerada no painel do Mercado Pago. Vem de user-secrets.</summary>
    public string SegredoWebhook { get; set; } = "";

    /// <summary>Endereço público da API, para onde o Mercado Pago envia as notificações.</summary>
    public string? UrlPublicaApi { get; set; }

    /// <summary>Endereço da loja, para onde o cliente volta depois de pagar.</summary>
    public string UrlLoja { get; set; } = "http://localhost:4200";
}
