using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Loja.Infrastructure.Persistencia;

/// <summary>
/// O banco pode devolver datas sem a marcação de fuso. Como a aplicação grava tudo em UTC, este conversor
/// marca as datas lidas como UTC; assim o JSON sai com "Z" e o navegador mostra no horário local.
/// </summary>
public class ConversorDataUtc() : ValueConverter<DateTime, DateTime>(
    gravar => gravar.Kind == DateTimeKind.Utc ? gravar : gravar.ToUniversalTime(),
    ler => DateTime.SpecifyKind(ler, DateTimeKind.Utc));
