import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { destinoSeguro } from '../../contas/entrar/entrar';
import { Sessao } from './auth.models';
import { authGuard } from './auth.guard';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

const sessao = (minutos: number): Sessao => ({
  token: 'token-de-teste',
  expiraEm: new Date(Date.now() + minutos * 60_000).toISOString(),
  usuario: { id: '1', nome: 'Maria Souza', email: 'maria@exemplo.com', perfis: ['Cliente'] },
});

describe('Autenticação', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'entrar', children: [] }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
  });

  it('salva a sessão ao entrar', async () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    const login = auth.entrar('maria@exemplo.com', 'Senha123');
    http.expectOne('/api/contas/login').flush(sessao(60));
    await login;

    expect(auth.logado()).toBe(true);
    expect(auth.usuario()?.nome).toBe('Maria Souza');
    expect(localStorage.getItem('loja.sessao')).toContain('token-de-teste');
  });

  it('descarta uma sessão salva que já expirou', () => {
    localStorage.setItem('loja.sessao', JSON.stringify(sessao(-1)));
    expect(TestBed.inject(AuthService).logado()).toBe(false);
  });

  it('envia o token só para a API', () => {
    localStorage.setItem('loja.sessao', JSON.stringify(sessao(60)));
    const cliente = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);

    cliente.get('/api/carrinho').subscribe();
    cliente.get('https://outro-site.com/dados').subscribe();

    expect(http.expectOne('/api/carrinho').request.headers.get('Authorization')).toBe('Bearer token-de-teste');
    expect(http.expectOne('https://outro-site.com/dados').request.headers.has('Authorization')).toBe(false);
  });

  it('encerra a sessão quando a API recusa o token', () => {
    localStorage.setItem('loja.sessao', JSON.stringify(sessao(60)));
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    TestBed.inject(HttpClient).get('/api/carrinho').subscribe({ error: () => {} });
    http.expectOne('/api/carrinho').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logado()).toBe(false);
  });

  it('o guard manda para o login lembrando de onde o usuário veio', () => {
    const resultado = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/minha-conta' } as never),
    );
    expect(TestBed.inject(Router).serializeUrl(resultado as never)).toBe('/entrar?retorno=%2Fminha-conta');
  });

  it('só redireciona para caminhos internos depois do login', () => {
    expect(destinoSeguro('/carrinho')).toBe('/carrinho');
    expect(destinoSeguro('https://site-malicioso.com')).toBe('/');
    expect(destinoSeguro('//site-malicioso.com')).toBe('/');
    expect(destinoSeguro(undefined)).toBe('/');
  });
});
