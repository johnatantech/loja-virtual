export interface Usuario {
  id: string;
  nome: string;
  email: string;
  perfis: string[];
}

export interface Sessao {
  token: string;
  expiraEm: string;
  usuario: Usuario;
}
