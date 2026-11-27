import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Endereço da API na publicação, definido no build:
 *   ng build --define API_URL="'https://minha-api.azurewebsites.net'"
 * Em desenvolvimento fica vazio, e o proxy do ng serve encaminha /api para a API local.
 */
declare const API_URL: string | undefined;
export const URL_API: string = typeof API_URL === 'string' ? API_URL.replace(/\/$/, '') : '';

/** Demo publicada: usada para avisar que o primeiro acesso pode demorar (planos gratuitos dormem). */
export const EH_DEMO_PUBLICADA = URL_API !== '';

export function comBaseDaApi(url: string, base = URL_API): string {
  return base && url.startsWith('/api/') ? base + url : url;
}

/** Prefixa as chamadas /api com o endereço da API publicada. */
export const apiUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const url = comBaseDaApi(req.url);
  return next(url === req.url ? req : req.clone({ url }));
};
