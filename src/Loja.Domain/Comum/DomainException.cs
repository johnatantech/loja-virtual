namespace Loja.Domain.Comum;

/// <summary>Violação de uma regra de negócio. A API traduz para HTTP 400.</summary>
public class DomainException(string mensagem) : Exception(mensagem);
