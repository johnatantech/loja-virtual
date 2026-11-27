import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { comoCarregavel } from '../../catalogo/catalogo.service';
import { Carregavel } from '../../catalogo/catalogo.models';
import { PedidoResumo, ROTULO_STATUS, numeroPedido } from '../pedidos.models';
import { PedidosService } from '../pedidos.service';

@Component({
  selector: 'app-pedidos-lista',
  imports: [RouterLink, CurrencyPipe, DatePipe],
  template: `
    <h1>Meus pedidos</h1>
    @let e = estado();
    @switch (e.tipo) {
      @case ('carregando') { <p class="suave">Carregando pedidos...</p> }
      @case ('erro') { <p class="erro" role="alert">Não foi possível carregar seus pedidos.</p> }
      @case ('pronto') {
        @if (e.tipo === 'pronto') {
          @if (e.dados.length === 0) {
            <p class="suave">Você ainda não fez nenhum pedido.</p>
            <a routerLink="/" class="botao-principal">Ver produtos</a>
          } @else {
            <ul>
              @for (p of e.dados; track p.id) {
                <li>
                  <a [routerLink]="['/pedidos', p.id]">
                    <span class="numero">Pedido {{ numero(p.id) }}</span>
                    <span class="data">{{ p.criadoEm | date: 'dd/MM/yyyy HH:mm' }}</span>
                    <span class="status" [class]="p.status">{{ rotulos[p.status] }}</span>
                    <span class="itens">{{ p.quantidadeItens }} {{ p.quantidadeItens === 1 ? 'item' : 'itens' }}</span>
                    <span class="total">{{ p.total | currency }}</span>
                  </a>
                </li>
              }
            </ul>
          }
        }
      }
    }
  `,
  styleUrls: ['../status-pedido.scss', './pedidos-lista.scss'],
})
export class PedidosLista {
  protected readonly rotulos = ROTULO_STATUS;
  protected readonly numero = numeroPedido;
  protected readonly estado = toSignal(comoCarregavel(inject(PedidosService).listar()), {
    initialValue: { tipo: 'carregando' } as Carregavel<PedidoResumo[]>,
  });
}
