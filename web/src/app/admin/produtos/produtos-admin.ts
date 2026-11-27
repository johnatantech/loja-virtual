import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Pagina } from '../../catalogo/catalogo.models';
import { lerErro } from '../../core/erros';
import { ProdutoAdmin } from '../admin.models';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-produtos-admin',
  imports: [RouterLink, CurrencyPipe],
  template: `
    <div class="barra">
      <h1>Produtos</h1>
      <a routerLink="/admin/produtos/novo" class="botao-principal">Novo produto</a>
    </div>

    <form class="busca" (submit)="$event.preventDefault(); buscar(termo.value)">
      <input #termo type="search" placeholder="Buscar por nome" aria-label="Buscar produtos" />
      <button type="submit" class="botao-secundario">Buscar</button>
    </form>

    @if (mensagem(); as m) {
      <p [class]="m.tipo" role="status">{{ m.texto }}</p>
    }

    @if (pagina(); as p) {
      <div class="tabela">
        <table>
          <thead>
            <tr><th>Produto</th><th>Categoria</th><th class="numero">Preço</th><th class="numero">Estoque</th><th>Situação</th><th class="numero">Repor</th><th></th></tr>
          </thead>
          <tbody>
            @for (produto of p.itens; track produto.id) {
              <tr [class.inativo]="!produto.ativo">
                <td><a [routerLink]="['/admin/produtos', produto.id]">{{ produto.nome }}</a></td>
                <td>{{ produto.categoria }}</td>
                <td class="numero">{{ produto.preco | currency }}</td>
                <td class="numero" [class.baixo]="produto.estoque <= 5">{{ produto.estoque }}</td>
                <td>{{ produto.ativo ? 'Ativo' : 'Inativo' }}</td>
                <td class="numero">
                  <form class="repor" (submit)="$event.preventDefault(); repor(produto, qtd.value); qtd.value = ''">
                    <input #qtd type="number" min="1" max="1000" placeholder="+0" [attr.aria-label]="'Repor estoque de ' + produto.nome" />
                    <button type="submit" class="botao-secundario botao-pequeno" [disabled]="ocupado()">Repor</button>
                  </form>
                </td>
                <td>
                  <div class="acoes-linha">
                    <button type="button" class="botao-secundario botao-pequeno" [disabled]="ocupado()" (click)="alternar(produto)">
                      {{ produto.ativo ? 'Desativar' : 'Ativar' }}
                    </button>
                  </div>
                </td>
              </tr>
            } @empty {
              <tr><td colspan="7" class="suave">Nenhum produto encontrado.</td></tr>
            }
          </tbody>
        </table>
      </div>
      @if (p.totalPaginas > 1) {
        <div class="paginacao">
          <button type="button" class="botao-secundario botao-pequeno" [disabled]="p.numeroPagina === 1" (click)="carregar(p.numeroPagina - 1)">Anterior</button>
          Página {{ p.numeroPagina }} de {{ p.totalPaginas }}
          <button type="button" class="botao-secundario botao-pequeno" [disabled]="p.numeroPagina === p.totalPaginas" (click)="carregar(p.numeroPagina + 1)">Próxima</button>
        </div>
      }
    } @else {
      <p class="suave">Carregando...</p>
    }
  `,
  styleUrls: ['../admin.scss'],
  styles: `
    .busca { display: flex; gap: 0.5rem; max-width: 420px; margin-bottom: 1.25rem; }
    .busca input { flex: 1; }
    .repor { display: inline-flex; gap: 0.35rem; }
    .repor input { width: 4.5rem; padding: 0.35rem 0.5rem; }
    .baixo { color: var(--atencao); font-weight: 700; }
  `,
})
export class ProdutosAdmin {
  private readonly admin = inject(AdminService);
  protected readonly pagina = signal<Pagina<ProdutoAdmin> | null>(null);
  protected readonly ocupado = signal(false);
  protected readonly mensagem = signal<{ tipo: 'sucesso' | 'alerta'; texto: string } | null>(null);
  private busca = '';

  constructor() {
    void this.carregar(1);
  }

  protected buscar(termo: string): void {
    this.busca = termo.trim();
    void this.carregar(1);
  }

  async carregar(numero: number): Promise<void> {
    this.pagina.set(await firstValueFrom(this.admin.produtos(this.busca, numero)));
  }

  protected repor(produto: ProdutoAdmin, valor: string): Promise<void> {
    const quantidade = Number(valor);
    if (!Number.isInteger(quantidade) || quantidade <= 0) {
      this.mensagem.set({ tipo: 'alerta', texto: 'Informe uma quantidade inteira maior que zero.' });
      return Promise.resolve();
    }
    return this.executar(this.admin.reporEstoque(produto.id, quantidade), `Estoque de "${produto.nome}" atualizado.`);
  }

  protected alternar(produto: ProdutoAdmin): Promise<void> {
    const texto = produto.ativo ? `"${produto.nome}" saiu da vitrine.` : `"${produto.nome}" voltou para a vitrine.`;
    return this.executar(this.admin.definirAtivo(produto.id, !produto.ativo), texto);
  }

  /** Atualiza só a linha alterada, sem recarregar a tabela inteira. */
  private async executar(acao: ReturnType<AdminService['produto']>, sucesso: string): Promise<void> {
    this.ocupado.set(true);
    try {
      const atualizado = await firstValueFrom(acao);
      this.pagina.update((p) => p && { ...p, itens: p.itens.map((i) => (i.id === atualizado.id ? atualizado : i)) });
      this.mensagem.set({ tipo: 'sucesso', texto: sucesso });
    } catch (e) {
      this.mensagem.set({ tipo: 'alerta', texto: lerErro(e).mensagem });
    } finally {
      this.ocupado.set(false);
    }
  }
}
