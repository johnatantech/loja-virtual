import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';

@Component({
  selector: 'app-cabecalho',
  imports: [RouterLink],
  template: `
    <div class="conteudo">
      <a routerLink="/" class="marca">Loja Virtual</a>
      <form role="search" class="busca" (submit)="$event.preventDefault(); buscar(termo.value)">
        <label for="busca" class="visualmente-oculto">Buscar produtos</label>
        <input id="busca" #termo type="search" placeholder="Buscar produtos" autocomplete="off"
               [value]="buscaAtual()" />
        <button type="submit" class="botao-principal">Buscar</button>
      </form>
    </div>
  `,
  styleUrl: './cabecalho.scss',
})
export class Cabecalho {
  private readonly router = inject(Router);

  /** O cabeçalho fica fora das rotas, então acompanha a URL pelos eventos de navegação. */
  protected readonly buscaAtual = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => this.router.parseUrl(this.router.url).queryParamMap.get('busca') ?? ''),
    ),
    { initialValue: '' },
  );

  protected buscar(termo: string): void {
    const busca = termo.trim() || null;
    void this.router.navigate(['/'], { queryParams: { busca } });
  }
}
