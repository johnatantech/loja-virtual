import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CepPipe } from '../../core/cep.pipe';
import { lerErro } from '../../core/erros';
import { PedidoDetalhe as Pedido, ROTULO_STATUS, numeroPedido } from '../pedidos.models';
import { PedidosService, irParaPagamento } from '../pedidos.service';

@Component({
  selector: 'app-pedido-detalhe',
  imports: [RouterLink, CurrencyPipe, DatePipe, CepPipe],
  templateUrl: './pedido-detalhe.html',
  styleUrls: ['../status-pedido.scss', './pedido-detalhe.scss'],
})
export class PedidoDetalhe {
  private readonly pedidos = inject(PedidosService);
  private readonly router = inject(Router);

  /** Id da rota e, na volta do Mercado Pago, o status informado na URL. */
  readonly id = input.required<string>();
  readonly status = input<string>();

  protected readonly rotulos = ROTULO_STATUS;
  protected readonly numero = numeroPedido;
  protected readonly pedido = signal<Pedido | null>(null);
  protected readonly naoEncontrado = signal(false);
  protected readonly ocupado = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly aguardandoConfirmacao = signal(false);

  private consultas = 0;
  private temporizador?: ReturnType<typeof setTimeout>;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.temporizador));
    effect(() => {
      const id = this.id();
      this.aguardandoConfirmacao.set(!!this.status());
      void this.carregar(id);
    });
  }

  private async carregar(id: string): Promise<void> {
    try {
      const pedido = await firstValueFrom(this.pedidos.obter(id));
      this.pedido.set(pedido);
      // Voltando do gateway, a confirmação chega pelo webhook alguns segundos depois:
      // consulta de novo por até ~30 segundos enquanto o pedido ainda aguarda pagamento.
      if (this.aguardandoConfirmacao() && pedido.status === 'AguardandoPagamento' && this.consultas++ < 10) {
        this.temporizador = setTimeout(() => void this.carregar(id), 3000);
      } else {
        this.aguardandoConfirmacao.set(false);
      }
    } catch (e) {
      this.naoEncontrado.set(true);
      this.erro.set(lerErro(e).mensagem);
    }
  }

  protected async pagar(p: Pedido): Promise<void> {
    this.ocupado.set(true);
    this.erro.set(null);
    try {
      const link = p.linkPagamento ?? (await firstValueFrom(this.pedidos.gerarPagamento(p.id))).linkPagamento;
      if (link) irParaPagamento(link, (url) => void this.router.navigateByUrl(url));
    } catch (e) {
      this.erro.set(lerErro(e).mensagem);
    } finally {
      this.ocupado.set(false);
    }
  }

  protected async cancelar(p: Pedido): Promise<void> {
    if (!confirm(`Cancelar o pedido ${this.numero(p.id)}? Os itens voltam para o estoque.`)) return;
    this.ocupado.set(true);
    this.erro.set(null);
    try {
      this.pedido.set(await firstValueFrom(this.pedidos.cancelar(p.id)));
    } catch (e) {
      this.erro.set(lerErro(e).mensagem);
    } finally {
      this.ocupado.set(false);
    }
  }
}
