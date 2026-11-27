import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Sessao } from './auth.models';

const CHAVE = 'loja.sessao';

/**
 * Guarda a sessão (token JWT) no localStorage para manter o login entre visitas.
 * O token expira em poucas horas e é descartado assim que vence.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessao = signal<Sessao | null>(lerSessaoSalva());

  readonly usuario = computed(() => this.sessao()?.usuario ?? null);
  readonly logado = computed(() => this.sessao() !== null);
  readonly ehAdmin = computed(() => this.usuario()?.perfis.includes('Admin') ?? false);

  /** Token válido para as requisições; descarta a sessão se já tiver expirado. */
  token(): string | null {
    const sessao = this.sessao();
    if (!sessao) return null;
    if (expirou(sessao)) {
      this.encerrar();
      return null;
    }
    return sessao.token;
  }

  async entrar(email: string, senha: string): Promise<void> {
    this.salvar(await firstValueFrom(this.http.post<Sessao>('/api/contas/login', { email, senha })));
  }

  async cadastrar(nome: string, email: string, senha: string): Promise<void> {
    this.salvar(await firstValueFrom(this.http.post<Sessao>('/api/contas/cadastro', { nome, email, senha })));
  }

  encerrar(): void {
    localStorage.removeItem(CHAVE);
    this.sessao.set(null);
  }

  private salvar(sessao: Sessao): void {
    localStorage.setItem(CHAVE, JSON.stringify(sessao));
    this.sessao.set(sessao);
  }
}

function expirou(sessao: Sessao): boolean {
  return new Date(sessao.expiraEm).getTime() <= Date.now();
}

function lerSessaoSalva(): Sessao | null {
  try {
    const sessao = JSON.parse(localStorage.getItem(CHAVE) ?? 'null') as Sessao | null;
    return sessao && !expirou(sessao) ? sessao : null;
  } catch {
    return null;
  }
}
