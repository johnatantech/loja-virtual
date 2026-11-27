import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="admin">
      <nav aria-label="Painel administrativo">
        <p class="titulo">Painel</p>
        <a routerLink="/admin" routerLinkActive="ativo" [routerLinkActiveOptions]="{ exact: true }">Visão geral</a>
        <a routerLink="/admin/produtos" routerLinkActive="ativo">Produtos</a>
        <a routerLink="/admin/pedidos" routerLinkActive="ativo">Pedidos</a>
        <a routerLink="/admin/clientes" routerLinkActive="ativo">Clientes</a>
      </nav>
      <section class="conteudo"><router-outlet /></section>
    </div>
  `,
  styles: `
    .admin { display: grid; grid-template-columns: 190px minmax(0, 1fr); gap: 2.5rem; }
    nav { display: flex; flex-direction: column; gap: 0.15rem; }
    .titulo { margin: 0 0 0.5rem; font-size: 0.8rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: var(--texto-suave); }
    nav a { padding: 0.5rem 0.7rem; border-radius: var(--raio-sm); color: var(--texto); text-decoration: none; }
    nav a:hover { background: var(--superficie-2); }
    nav a.ativo { background: var(--superficie-2); font-weight: 700; color: var(--destaque); }
    @media (max-width: 760px) {
      .admin { grid-template-columns: 1fr; gap: 1.25rem; }
      nav { flex-direction: row; flex-wrap: wrap; }
      .titulo { display: none; }
    }
  `,
})
export class AdminLayout {}
