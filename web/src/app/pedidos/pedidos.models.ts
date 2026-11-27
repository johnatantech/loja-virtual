export interface Endereco {
  destinatario: string;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string | null;
  bairro: string;
  cidade: string;
  uf: string | null;
}

export interface CotacaoFrete {
  uf: string;
  subtotal: number;
  valorFrete: number;
  prazoDias: number;
  freteGratis: boolean;
  total: number;
}

export interface PedidoCriado {
  id: string;
  linkPagamento: string | null;
}

export type StatusPedido = 'AguardandoPagamento' | 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado';

export interface PedidoResumo {
  id: string;
  criadoEm: string;
  status: StatusPedido;
  quantidadeItens: number;
  total: number;
}

export interface PedidoDetalhe {
  id: string;
  criadoEm: string;
  status: StatusPedido;
  itens: { produtoId: number; nome: string; precoUnitario: number; quantidade: number; subtotal: number }[];
  endereco: Endereco | null;
  subtotal: number;
  valorFrete: number;
  total: number;
  prazoEntregaDias: number;
  linkPagamento: string | null;
  pagoEm: string | null;
}

export const ROTULO_STATUS: Record<StatusPedido, string> = {
  AguardandoPagamento: 'Aguardando pagamento',
  Pago: 'Pago',
  Enviado: 'Enviado',
  Entregue: 'Entregue',
  Cancelado: 'Cancelado',
};

/** Número curto para mostrar ao cliente (os 8 primeiros caracteres do id). */
export function numeroPedido(id: string): string {
  return id.slice(0, 8).toUpperCase();
}
