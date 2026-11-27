import { CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { comoCarregavel } from '../../catalogo/catalogo.service';
import { Carregavel } from '../../catalogo/catalogo.models';
import { ROTULO_STATUS, StatusPedido } from '../../pedidos/pedidos.models';
import { Dashboard } from '../admin.models';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-painel',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <h1>Visão geral</h1>
    @let e = estado();
    @if (e.tipo === 'pronto') {
      @let d = e.dados;
      <div class="cartoes">
        <article><span>Faturamento total</span><strong>{{ d.faturamentoTotal | currency }}</strong></article>
        <article><span>Últimos 30 dias</span><strong>{{ d.faturamentoUltimos30Dias | currency }}</strong></article>
        <article><span>Ticket médio</span><strong>{{ d.ticketMedio | currency }}</strong></article>
        <a routerLink="/admin/pedidos" [queryParams]="{ status: 'Pago' }" class="destaque">
          <span>Aguardando envio</span><strong>{{ d.pedidosAguardandoEnvio }}</strong>
        </a>
      </div>

      <div class="grade">
        <section>
          <h2>Pedidos por status</h2>
          <ul class="lista">
            @for (s of status; track s) {
              <li>
                <a [routerLink]="['/admin/pedidos']" [queryParams]="{ status: s }">{{ rotulos[s] }}</a>
                <span>{{ d.pedidosPorStatus[s] }}</span>
              </li>
            }
          </ul>
        </section>

        <section>
          <h2>Estoque baixo</h2>
          @if (d.estoqueBaixo.length === 0) {
            <p class="suave">Nenhum produto com 5 unidades ou menos.</p>
          } @else {
            <ul class="lista">
              @for (p of d.estoqueBaixo; track p.id) {
                <li>
                  <a [routerLink]="['/admin/produtos', p.id]">{{ p.nome }}</a>
                  <span [class.zerado]="p.estoque === 0">{{ p.estoque === 0 ? 'Esgotado' : p.estoque + ' un.' }}</span>
                </li>
              }
            </ul>
          }
        </section>

        <section>
          <h2>Mais vendidos</h2>
          @if (d.maisVendidos.length === 0) {
            <p class="suave">Ainda não há vendas pagas.</p>
          } @else {
            <ol class="lista">
              @for (m of d.maisVendidos; track m.produtoId) {
                <li><span>{{ m.nome }}</span><span>{{ m.quantidade }} un. · {{ m.receita | currency }}</span></li>
              }
            </ol>
          }
        </section>
      </div>
    } @else if (e.tipo === 'erro') {
      <p class="alerta" role="alert">Não foi possível carregar o painel.</p>
    } @else {
      <p class="suave">Carregando...</p>
    }
  `,
  styleUrls: ['../admin.scss', './painel.scss'],
})
export class Painel {
  protected readonly rotulos = ROTULO_STATUS;
  protected readonly status = Object.keys(ROTULO_STATUS) as StatusPedido[];
  protected readonly estado = toSignal(comoCarregavel(inject(AdminService).dashboard()), {
    initialValue: { tipo: 'carregando' } as Carregavel<Dashboard>,
  });
}
