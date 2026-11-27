import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { lerErro } from '../core/erros';
import { CARRINHO_VAZIO, Carrinho, ItemSolicitado, QUANTIDADE_MAXIMA } from './carrinho.models';

const CHAVE_VISITANTE = 'loja.carrinho';

/**
 * Carrinho único para a interface, com duas fontes por trás:
 * - visitante: itens no localStorage, precificados pela API (/cotacao) sem salvar nada;
 * - logado: carrinho salvo na API, disponível em qualquer dispositivo.
 * Ao entrar na conta, o carrinho de visitante é mesclado ao da conta.
 */
@Injectable({ providedIn: 'root' })
export class CarrinhoService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  private readonly estado = signal<Carrinho>(CARRINHO_VAZIO);
  readonly carrinho = this.estado.asReadonly();
  readonly quantidadeTotal = computed(() => this.estado().quantidadeTotal);
  readonly carregando = signal(false);

  constructor() {
    // Reage a login e logout: mescla o carrinho de visitante ou volta para ele.
    effect(() => {
      const logado = this.auth.logado();
      untracked(() => void (logado ? this.mesclarVisitante() : this.recarregar()));
    });
  }

  quantidadeDe(produtoId: number): number {
    return this.estado().itens.find((i) => i.produtoId === produtoId)?.quantidade ?? 0;
  }

  /** Soma ao que já está no carrinho. Retorna uma mensagem de erro, se houver. */
  adicionar(produtoId: number, quantidade = 1): Promise<string | null> {
    const total = Math.min(this.quantidadeDe(produtoId) + quantidade, QUANTIDADE_MAXIMA);
    return this.definirQuantidade(produtoId, total);
  }

  remover(produtoId: number): Promise<string | null> {
    return this.definirQuantidade(produtoId, 0);
  }

  async definirQuantidade(produtoId: number, quantidade: number): Promise<string | null> {
    return this.executar(async () => {
      if (this.auth.logado()) {
        return firstValueFrom(this.http.put<Carrinho>(`/api/carrinho/itens/${produtoId}`, { quantidade }));
      }
      const itens = lerVisitante().filter((i) => i.produtoId !== produtoId);
      if (quantidade > 0) itens.push({ produtoId, quantidade: Math.min(quantidade, QUANTIDADE_MAXIMA) });
      salvarVisitante(itens);
      return this.cotar(itens);
    });
  }

  async recarregar(): Promise<void> {
    await this.executar(() =>
      this.auth.logado() ? firstValueFrom(this.http.get<Carrinho>('/api/carrinho')) : this.cotar(lerVisitante()),
    );
  }

  private async mesclarVisitante(): Promise<void> {
    const itens = lerVisitante();
    if (itens.length === 0) return this.recarregar();
    const erro = await this.executar(() => firstValueFrom(this.http.post<Carrinho>('/api/carrinho/mesclar', { itens })));
    if (!erro) localStorage.removeItem(CHAVE_VISITANTE);
  }

  private cotar(itens: ItemSolicitado[]): Promise<Carrinho> {
    return itens.length === 0
      ? Promise.resolve(CARRINHO_VAZIO)
      : firstValueFrom(this.http.post<Carrinho>('/api/carrinho/cotacao', { itens }));
  }

  private async executar(acao: () => Promise<Carrinho>): Promise<string | null> {
    this.carregando.set(true);
    try {
      this.estado.set(await acao());
      return null;
    } catch (erro) {
      return lerErro(erro, 'Não foi possível atualizar o carrinho.').mensagem;
    } finally {
      this.carregando.set(false);
    }
  }
}

function lerVisitante(): ItemSolicitado[] {
  try {
    const itens = JSON.parse(localStorage.getItem(CHAVE_VISITANTE) ?? '[]') as ItemSolicitado[];
    return Array.isArray(itens) ? itens.filter((i) => i.quantidade > 0) : [];
  } catch {
    return [];
  }
}

function salvarVisitante(itens: ItemSolicitado[]): void {
  localStorage.setItem(CHAVE_VISITANTE, JSON.stringify(itens));
}
