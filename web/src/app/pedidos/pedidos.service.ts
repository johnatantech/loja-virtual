import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { CotacaoFrete, NovoPedido, PedidoCriado, PedidoDetalhe, PedidoResumo } from './pedidos.models';

export interface EnderecoViaCep {
  logradouro: string;
  bairro: string;
  localidade: string;
  uf: string;
  erro?: boolean | string;
}

@Injectable({ providedIn: 'root' })
export class PedidosService {
  private readonly http = inject(HttpClient);

  /** Consulta pública do ViaCEP, direto do navegador (não passa pela nossa API nem leva token). */
  buscarCep(cep: string) {
    return this.http.get<EnderecoViaCep>(`https://viacep.com.br/ws/${cep}/json/`);
  }

  cotarFrete(cep: string) {
    return this.http.get<CotacaoFrete>('/api/checkout/frete', { params: { cep } });
  }

  criar(pedido: NovoPedido) {
    return this.http.post<PedidoCriado>('/api/pedidos', pedido);
  }

  listar() {
    return this.http.get<PedidoResumo[]>('/api/pedidos');
  }

  obter(id: string) {
    return this.http.get<PedidoDetalhe>(`/api/pedidos/${id}`);
  }

  cancelar(id: string) {
    return this.http.post<PedidoDetalhe>(`/api/pedidos/${id}/cancelar`, null);
  }

  gerarPagamento(id: string) {
    return this.http.post<PedidoCriado>(`/api/pedidos/${id}/pagamento`, null);
  }

  simularPagamento(id: string, aprovar: boolean) {
    return this.http.post<PedidoDetalhe>(`/api/pagamentos/simulado/${id}`, { aprovar });
  }
}

/** Link interno (gateway simulado) abre na própria loja; externo (Mercado Pago) troca de site. */
export function irParaPagamento(link: string, navegarInterno: (url: string) => void): void {
  if (link.startsWith('/')) navegarInterno(link);
  else window.location.assign(link);
}
