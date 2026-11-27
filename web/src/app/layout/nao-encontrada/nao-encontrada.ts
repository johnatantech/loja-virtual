import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-nao-encontrada',
  imports: [RouterLink],
  template: `
    <h1>Página não encontrada</h1>
    <p>O endereço que você abriu não existe na loja.</p>
    <a routerLink="/" class="botao-secundario">Voltar para a loja</a>
  `,
  styles: `:host { display: block; padding: 3rem 0; } p { color: var(--texto-suave); }`,
})
export class NaoEncontrada {}
