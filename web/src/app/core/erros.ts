import { HttpErrorResponse } from '@angular/common/http';

/** Lê a mensagem e os erros por campo de um ProblemDetails/ValidationProblem da API. */
export function lerErro(erro: unknown, padrao = 'Algo deu errado. Tente novamente.'): {
  mensagem: string;
  campos: Record<string, string[]>;
} {
  if (erro instanceof HttpErrorResponse) {
    if (erro.status === 0) return { mensagem: 'Sem conexão com o servidor.', campos: {} };
    if (erro.status === 429) return { mensagem: 'Muitas tentativas. Aguarde um minuto e tente de novo.', campos: {} };
    const corpo = erro.error as { title?: string; errors?: Record<string, string[]> } | null;
    const campos = corpo?.errors ?? {};
    const temCampos = Object.keys(campos).length > 0;
    return { mensagem: temCampos ? 'Revise os campos destacados.' : (corpo?.title ?? padrao), campos };
  }
  return { mensagem: padrao, campos: {} };
}
