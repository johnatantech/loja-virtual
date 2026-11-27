import { CurrencyPipe } from '@angular/common';
import { Component, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { firstValueFrom, switchMap } from 'rxjs';
import { comoCarregavel } from '../../catalogo/catalogo.service';
import { Carregavel } from '../../catalogo/catalogo.models';
import { lerErro } from '../../core/erros';
import { PedidoDetalhe, numeroPedido } from '../pedidos.models';
import { PedidosService } from '../pedidos.service';

/** Substitui a página do gateway quando a API usa o pagamento simulado (desenvolvimento e demo). */
@Component({
  selector: 'app-pagamento-simulado',
  imports: [CurrencyPipe],
  template: `
    <div class="caixa">
      <p class="selo">Ambiente de demonstração</p>
      <h1>Pagamento simulado</h1>
      @let e = estado();
      @if (e.tipo === 'pronto') {
        <p>Pedido <strong>{{ numero(e.dados.id) }}</strong></p>
        <p class="valor">{{ e.dados.total | currency }}</p>
        <p class="suave">Nenhum valor é cobrado. Escolha o resultado para testar o fluxo da loja.</p>
        @if (erro(); as mensagem) {
          <p class="alerta" role="alert">{{ mensagem }}</p>
        }
        <div class="acoes">
          <button type="button" class="botao-principal" [disabled]="enviando()" (click)="responder(e.dados, true)">Aprovar pagamento</button>
          <button type="button" class="botao-secundario" [disabled]="enviando()" (click)="responder(e.dados, false)">Recusar pagamento</button>
        </div>
      } @else if (e.tipo === 'carregando') {
        <p class="suave">Carregando...</p>
      } @else {
        <p class="alerta">Pedido não encontrado.</p>
      }
    </div>
  `,
  styles: `
    .caixa { max-width: 440px; margin: 2rem auto; padding: 2rem; border: 1px solid var(--borda); border-radius: var(--raio-sm); text-align: center; }
    .selo { display: inline-block; margin: 0 0 0.75rem; padding: 0.2rem 0.7rem; border-radius: 999px; background: #fff4e0; color: var(--atencao); font-size: 0.8rem; font-weight: 700; }
    h1 { margin: 0 0 1rem; }
    .valor { margin: 0.25rem 0 0.75rem; font-size: 2rem; font-weight: 800; font-variant-numeric: tabular-nums; }
    .suave { color: var(--texto-suave); font-size: 0.9rem; }
    .acoes { display: flex; flex-direction: column; gap: 0.6rem; margin-top: 1.5rem; }
    .alerta { padding: 0.75rem; border-radius: var(--raio-sm); background: #fdecea; color: var(--alerta); }
  `,
})
export class PagamentoSimulado {
  private readonly pedidos = inject(PedidosService);
  private readonly router = inject(Router);

  readonly id = input.required<string>();
  protected readonly numero = numeroPedido;
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly estado = toSignal(
    toObservable(this.id).pipe(switchMap((id) => comoCarregavel(this.pedidos.obter(id)))),
    { initialValue: { tipo: 'carregando' } as Carregavel<PedidoDetalhe> },
  );

  protected async responder(pedido: PedidoDetalhe, aprovar: boolean): Promise<void> {
    this.enviando.set(true);
    try {
      await firstValueFrom(this.pedidos.simularPagamento(pedido.id, aprovar));
      await this.router.navigate(['/pedidos', pedido.id]);
    } catch (e) {
      this.erro.set(lerErro(e).mensagem);
    } finally {
      this.enviando.set(false);
    }
  }
}
