import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CepPipe } from '../../core/cep.pipe';
import { lerErro } from '../../core/erros';
import { Pagina } from '../../catalogo/catalogo.models';
import { PedidoDetalhe, ROTULO_STATUS, StatusPedido, numeroPedido } from '../../pedidos/pedidos.models';
import { PedidoAdmin } from '../admin.models';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-pedidos-admin',
  imports: [RouterLink, CurrencyPipe, DatePipe],
  template: `
    <h1>Pedidos</h1>
    <nav class="abas" aria-label="Filtrar por status">
      <a [routerLink]="[]" [queryParams]="{ status: null }" [class.ativa]="!status()">Todos</a>
      @for (s of todosStatus; track s) {
        <a [routerLink]="[]" [queryParams]="{ status: s }" [class.ativa]="status() === s">{{ rotulos[s] }}</a>
      }
    </nav>

    @if (pagina(); as p) {
      <div class="tabela">
        <table>
          <thead><tr><th>Pedido</th><th>Data</th><th>Cliente</th><th>Status</th><th class="numero">Itens</th><th class="numero">Total</th></tr></thead>
          <tbody>
            @for (pedido of p.itens; track pedido.id) {
              <tr>
                <td><a [routerLink]="['/admin/pedidos', pedido.id]">{{ numero(pedido.id) }}</a></td>
                <td>{{ pedido.criadoEm | date: 'dd/MM/yyyy HH:mm' }}</td>
                <td>{{ pedido.clienteNome }}<br /><span class="suave">{{ pedido.clienteEmail }}</span></td>
                <td><span class="status" [class]="pedido.status">{{ rotulos[pedido.status] }}</span></td>
                <td class="numero">{{ pedido.quantidadeItens }}</td>
                <td class="numero">{{ pedido.total | currency }}</td>
              </tr>
            } @empty {
              <tr><td colspan="6" class="suave">Nenhum pedido com este status.</td></tr>
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
  styleUrls: ['../admin.scss', '../../pedidos/status-pedido.scss'],
  styles: `
    .abas { display: flex; flex-wrap: wrap; gap: 0.4rem; margin-bottom: 1.25rem; }
    .abas a { padding: 0.35rem 0.8rem; border: 1px solid var(--borda); border-radius: 999px; color: var(--texto); text-decoration: none; font-size: 0.9rem; }
    .abas a.ativa { border-color: var(--destaque); color: var(--destaque); font-weight: 700; }
  `,
})
export class PedidosAdmin {
  private readonly admin = inject(AdminService);
  readonly status = input<StatusPedido>();

  protected readonly rotulos = ROTULO_STATUS;
  protected readonly todosStatus = Object.keys(ROTULO_STATUS) as StatusPedido[];
  protected readonly numero = numeroPedido;
  protected readonly pagina = signal<Pagina<PedidoAdmin> | null>(null);

  constructor() {
    effect(() => {
      this.status(); // recarrega quando a aba (query param) muda
      void this.carregar(1);
    });
  }

  async carregar(numero: number): Promise<void> {
    this.pagina.set(await firstValueFrom(this.admin.pedidos(this.status() ?? '', numero)));
  }
}

@Component({
  selector: 'app-pedido-admin',
  imports: [RouterLink, CurrencyPipe, DatePipe, CepPipe],
  template: `
    <nav class="trilha"><a routerLink="/admin/pedidos">Pedidos</a> <span aria-hidden="true">/</span> {{ numero(id()) }}</nav>
    @if (pedido(); as p) {
      <div class="barra">
        <h1>Pedido {{ numero(p.id) }}</h1>
        <span class="status" [class]="p.status">{{ rotulos[p.status] }}</span>
      </div>
      <p class="suave">Feito em {{ p.criadoEm | date: "dd/MM/yyyy 'às' HH:mm" }}{{ p.pagoEm ? ' · pago em ' + (p.pagoEm | date: 'dd/MM/yyyy HH:mm') : '' }}</p>

      @if (erro(); as mensagem) {
        <p class="alerta" role="alert">{{ mensagem }}</p>
      }

      <div class="acoes">
        @if (p.status === 'Pago') {
          <button type="button" class="botao-principal" [disabled]="ocupado()" (click)="avancar('enviar')">Marcar como enviado</button>
        }
        @if (p.status === 'Enviado') {
          <button type="button" class="botao-principal" [disabled]="ocupado()" (click)="avancar('entregar')">Confirmar entrega</button>
        }
        @if (p.status === 'AguardandoPagamento' || p.status === 'Pago') {
          <button type="button" class="botao-secundario" [disabled]="ocupado()" (click)="avancar('cancelar')">Cancelar pedido</button>
        }
      </div>

      <div class="tabela">
        <table>
          <thead><tr><th>Item</th><th class="numero">Qtd.</th><th class="numero">Unitário</th><th class="numero">Subtotal</th></tr></thead>
          <tbody>
            @for (item of p.itens; track item.produtoId) {
              <tr><td>{{ item.nome }}</td><td class="numero">{{ item.quantidade }}</td><td class="numero">{{ item.precoUnitario | currency }}</td><td class="numero">{{ item.subtotal | currency }}</td></tr>
            }
            <tr><td colspan="3">Frete</td><td class="numero">{{ p.valorFrete | currency }}</td></tr>
            <tr class="total"><td colspan="3">Total</td><td class="numero">{{ p.total | currency }}</td></tr>
          </tbody>
        </table>
      </div>

      @if (p.endereco; as e) {
        <h2>Entrega</h2>
        <address>{{ e.destinatario }} · {{ e.logradouro }}, {{ e.numero }}{{ e.complemento ? ' - ' + e.complemento : '' }} · {{ e.bairro }} · {{ e.cidade }}/{{ e.uf }} · CEP {{ e.cep | cep }}</address>
      }
    } @else {
      <p class="suave">Carregando...</p>
    }
  `,
  styleUrls: ['../admin.scss', '../../pedidos/status-pedido.scss'],
  styles: `
    .trilha { display: flex; gap: 0.5rem; margin-bottom: 1rem; font-size: 0.9rem; color: var(--texto-suave); }
    .acoes { display: flex; gap: 0.75rem; margin: 1.25rem 0 1.75rem; }
    .total td { font-weight: 800; }
    h2 { margin: 2rem 0 0.5rem; font-size: 1rem; }
    address { font-style: normal; }
  `,
})
export class PedidoAdminDetalhe {
  private readonly admin = inject(AdminService);
  private readonly router = inject(Router);
  readonly id = input.required<string>();

  protected readonly rotulos = ROTULO_STATUS;
  protected readonly numero = numeroPedido;
  protected readonly pedido = signal<PedidoDetalhe | null>(null);
  protected readonly ocupado = signal(false);
  protected readonly erro = signal<string | null>(null);

  ngOnInit(): void {
    firstValueFrom(this.admin.pedido(this.id()))
      .then((p) => this.pedido.set(p))
      .catch(() => void this.router.navigateByUrl('/admin/pedidos'));
  }

  protected async avancar(acao: 'enviar' | 'entregar' | 'cancelar'): Promise<void> {
    if (acao === 'cancelar' && !confirm('Cancelar este pedido? Os itens voltam para o estoque. Se já foi pago, o estorno deve ser feito no gateway.')) return;
    this.ocupado.set(true);
    this.erro.set(null);
    try {
      this.pedido.set(await firstValueFrom(this.admin.avancarPedido(this.id(), acao)));
    } catch (e) {
      this.erro.set(lerErro(e).mensagem);
    } finally {
      this.ocupado.set(false);
    }
  }
}
