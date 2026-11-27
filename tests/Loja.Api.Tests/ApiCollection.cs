namespace Loja.Api.Tests;

/// <summary>
/// Todas as classes de teste da coleção "Api" compartilham o mesmo SQL Server,
/// em vez de subir um contêiner por classe (cada um leva cerca de 20 segundos).
/// Os testes são só de leitura, então compartilhar o banco é seguro.
/// </summary>
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFactory>;
