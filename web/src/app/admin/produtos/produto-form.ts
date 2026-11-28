import { Component, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CatalogoService } from '../../catalogo/catalogo.service';
import { lerErro } from '../../core/erros';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-produto-form',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <nav class="trilha"><a routerLink="/admin/produtos">Produtos</a> <span aria-hidden="true">/</span> {{ ehNovo() ? 'Novo' : 'Editar' }}</nav>
    <h1>{{ ehNovo() ? 'Novo produto' : 'Editar produto' }}</h1>

    <form [formGroup]="form" (ngSubmit)="salvar()" novalidate>
      <label>
        Nome
        <input formControlName="nome" [attr.aria-invalid]="invalido('nome')" />
      </label>

      <div class="linha">
        <label>
          Categoria
          <select formControlName="categoriaId" [attr.aria-invalid]="invalido('categoriaId')">
            <option [ngValue]="0" disabled>Escolha</option>
            @for (c of categorias(); track c.id) {
              <option [ngValue]="c.id">{{ c.nome }}</option>
            }
          </select>
        </label>
        <label>
          Preço (R$)
          <input type="number" step="0.01" min="0.01" formControlName="preco" [attr.aria-invalid]="invalido('preco')" />
        </label>
        @if (ehNovo()) {
          <label>
            Estoque inicial
            <input type="number" step="1" min="0" formControlName="estoqueInicial" />
          </label>
        }
      </div>

      <label>
        Descrição
        <textarea rows="5" formControlName="descricao"></textarea>
      </label>

      <label>
        Imagem <span class="opcional">(opcional: endereço https:// ou foto da loja, como /produtos/nome.webp)</span>
        <input formControlName="imagemUrl" placeholder="https://... ou /produtos/nome.webp" />
      </label>

      @if (!ehNovo()) {
        <p class="suave">Para mudar o estoque, use "Repor" na lista de produtos. Mudanças de preço não alteram pedidos já feitos.</p>
      }
      @if (erro(); as mensagem) {
        <p class="alerta" role="alert">{{ mensagem }}</p>
      }

      <div class="acoes">
        <button type="submit" class="botao-principal" [disabled]="salvando()">{{ salvando() ? 'Salvando...' : 'Salvar' }}</button>
        <a routerLink="/admin/produtos" class="botao-secundario">Cancelar</a>
      </div>
    </form>
  `,
  styleUrls: ['../admin.scss'],
  styles: `
    .trilha { display: flex; gap: 0.5rem; margin-bottom: 1rem; font-size: 0.9rem; color: var(--texto-suave); }
    form { display: flex; flex-direction: column; gap: 1rem; max-width: 640px; }
    label { display: flex; flex-direction: column; gap: 0.35rem; font-weight: 600; font-size: 0.95rem; }
    input, select, textarea { width: 100%; font: inherit; }
    textarea { padding: 0.55rem 0.7rem; border: 1px solid var(--borda); border-radius: var(--raio-sm); resize: vertical; }
    [aria-invalid='true'] { border-color: var(--alerta); }
    .linha { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 1rem; }
    .opcional { font-weight: 400; font-size: 0.85rem; color: var(--texto-suave); }
    .acoes { display: flex; gap: 0.75rem; }
  `,
})
export class ProdutoForm {
  private readonly admin = inject(AdminService);
  private readonly router = inject(Router);

  /** "novo" ou o id do produto, vindo da rota. */
  readonly id = input.required<string>();
  protected readonly ehNovo = () => this.id() === 'novo';

  protected readonly categorias = toSignal(inject(CatalogoService).categorias$, { initialValue: [] });
  protected readonly salvando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    categoriaId: [0, Validators.min(1)],
    preco: [0, [Validators.required, Validators.min(0.01)]],
    estoqueInicial: [0, Validators.min(0)],
    descricao: ['', Validators.maxLength(2000)],
    imagemUrl: [''],
  });

  ngOnInit(): void {
    if (this.ehNovo()) return;
    firstValueFrom(this.admin.produto(Number(this.id())))
      .then((p) => this.form.patchValue({ ...p, imagemUrl: p.imagemUrl ?? '' }))
      .catch((e: unknown) => this.erro.set(lerErro(e, 'Produto não encontrado.').mensagem));
  }

  protected invalido(campo: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[campo];
    return c.touched && c.invalid;
  }

  protected async salvar(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.salvando.set(true);
    this.erro.set(null);
    try {
      const v = this.form.getRawValue();
      const dados = { ...v, imagemUrl: v.imagemUrl.trim() || null };
      await firstValueFrom(this.ehNovo() ? this.admin.criarProduto(dados) : this.admin.atualizarProduto(Number(this.id()), dados));
      await this.router.navigateByUrl('/admin/produtos');
    } catch (e) {
      this.erro.set(lerErro(e).mensagem);
    } finally {
      this.salvando.set(false);
    }
  }
}
