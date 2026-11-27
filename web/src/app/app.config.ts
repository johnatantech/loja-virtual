import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { apiUrlInterceptor } from './core/api-url';
import { authInterceptor } from './core/auth/auth.interceptor';
import { provideLocalizacaoBrasil } from './core/localizacao';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor, apiUrlInterceptor])),
    provideLocalizacaoBrasil(),
  ],
};
