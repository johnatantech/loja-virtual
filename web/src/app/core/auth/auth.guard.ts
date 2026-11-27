import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Páginas que exigem login: sem sessão, vai para /entrar e volta depois para onde estava. */
export const authGuard: CanActivateFn = (_rota, estado) => {
  const auth = inject(AuthService);
  return auth.token() !== null
    ? true
    : inject(Router).createUrlTree(['/entrar'], { queryParams: { retorno: estado.url } });
};
