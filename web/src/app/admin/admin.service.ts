import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Pagina } from '../catalogo/catalogo.models';
import { PedidoDetalhe } from '../pedidos/pedidos.models';
import { ClienteAdmin, Dashboard, PedidoAdmin, ProdutoAdmin, SalvarProduto } from './admin.models';

function parametros(valores: Record<string, string | number | null | undefined>): HttpParams {
  let params = new HttpParams();
  for (const [chave, valor] of Object.entries(valores)) {
    if (valor !== null && valor !== undefined && valor !== '') params = params.set(chave, String(valor));
  }
  return params;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/admin';

  dashboard() {
    return this.http.get<Dashboard>(`${this.base}/dashboard`);
  }

  produtos(busca: string, pagina: number) {
    return this.http.get<Pagina<ProdutoAdmin>>(`${this.base}/produtos`, { params: parametros({ busca, pagina }) });
  }

  produto(id: number) {
    return this.http.get<ProdutoAdmin>(`${this.base}/produtos/${id}`);
  }

  criarProduto(dados: SalvarProduto) {
    return this.http.post<ProdutoAdmin>(`${this.base}/produtos`, dados);
  }

  atualizarProduto(id: number, dados: SalvarProduto) {
    return this.http.put<ProdutoAdmin>(`${this.base}/produtos/${id}`, dados);
  }

  reporEstoque(id: number, quantidade: number) {
    return this.http.post<ProdutoAdmin>(`${this.base}/produtos/${id}/estoque`, { quantidade });
  }

  definirAtivo(id: number, ativo: boolean) {
    return this.http.post<ProdutoAdmin>(`${this.base}/produtos/${id}/${ativo ? 'ativar' : 'desativar'}`, null);
  }

  pedidos(status: string, pagina: number) {
    return this.http.get<Pagina<PedidoAdmin>>(`${this.base}/pedidos`, { params: parametros({ status, pagina }) });
  }

  pedido(id: string) {
    return this.http.get<PedidoDetalhe>(`${this.base}/pedidos/${id}`);
  }

  avancarPedido(id: string, acao: 'enviar' | 'entregar' | 'cancelar') {
    return this.http.post<PedidoDetalhe>(`${this.base}/pedidos/${id}/${acao}`, null);
  }

  clientes(busca: string, pagina: number) {
    return this.http.get<Pagina<ClienteAdmin>>(`${this.base}/clientes`, { params: parametros({ busca, pagina }) });
  }

  desbloquear(id: string) {
    return this.http.post<void>(`${this.base}/clientes/${id}/desbloquear`, null);
  }
}
