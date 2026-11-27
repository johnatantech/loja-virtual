import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Loja Virtual',
    loadComponent: () => import('./catalogo/vitrine/vitrine').then((m) => m.Vitrine),
  },
  {
    path: 'produto/:slug',
    title: 'Produto | Loja Virtual',
    loadComponent: () => import('./catalogo/produto-detalhe/produto-detalhe').then((m) => m.ProdutoDetalhe),
  },
  {
    path: '**',
    title: 'Página não encontrada | Loja Virtual',
    loadComponent: () => import('./layout/nao-encontrada/nao-encontrada').then((m) => m.NaoEncontrada),
  },
];
