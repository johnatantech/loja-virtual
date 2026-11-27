import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-cabecalho',
  imports: [RouterLink],
  template: `
    <div class="conteudo">
      <a routerLink="/" class="marca">Loja Virtual</a>
      <form role="search" class="busca" (submit)="$event.preventDefault(); buscar(termo.value)">
        <label for="busca" class="visualmente-oculto">Buscar produtos</label>
        <input id="busca" #termo type="search" placeholder="Buscar produtos" autocomplete="off" />
        <button type="submit" class="botao-principal">Buscar</button>
      </form>
    </div>
  `,
  styleUrl: './cabecalho.scss',
})
export class Cabecalho {
  private readonly router = inject(Router);

  protected buscar(termo: string): void {
    const busca = termo.trim() || null;
    void this.router.navigate(['/'], { queryParams: { busca } });
  }
}
