import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith } from 'rxjs';
import { CatalogoService } from './catalogo/catalogo.service';
import { Categoria } from './catalogo/catalogo.models';

type Estado =
  | { tipo: 'carregando' }
  | { tipo: 'pronto'; categorias: Categoria[] }
  | { tipo: 'erro' };

@Component({
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  private readonly catalogo = inject(CatalogoService);

  protected readonly estado = toSignal(
    this.catalogo.listarCategorias().pipe(
      map((categorias): Estado => ({ tipo: 'pronto', categorias })),
      catchError(() => of<Estado>({ tipo: 'erro' })),
      startWith<Estado>({ tipo: 'carregando' }),
    ),
    { requireSync: true },
  );
}
