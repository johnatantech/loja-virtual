import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Área administrativa: sem login vai para /entrar; logado sem perfil Admin volta para a loja.
 * É só conveniência de navegação: quem protege os dados de verdade é a API (403 para não admins).
 */
export const adminGuard: CanActivateFn = (_rota, estado) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.token() === null) return router.createUrlTree(['/entrar'], { queryParams: { retorno: estado.url } });
  return auth.ehAdmin() ? true : router.createUrlTree(['/']);
};
