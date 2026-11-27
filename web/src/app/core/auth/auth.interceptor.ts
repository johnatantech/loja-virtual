import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Envia o token nas chamadas à API e, se a API recusar um token que estava em uso
 * (expirado ou revogado), encerra a sessão e leva o usuário para o login.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = req.url.startsWith('/api/') ? auth.token() : null;
  const requisicao = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(requisicao).pipe(
    catchError((erro: unknown) => {
      if (token && erro instanceof HttpErrorResponse && erro.status === 401) {
        auth.encerrar();
        void router.navigate(['/entrar'], { queryParams: { expirou: 1, retorno: router.url } });
      }
      return throwError(() => erro);
    }),
  );
};
