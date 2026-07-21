import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import { CatalogsApiService } from './services/catalogs-api.service';
import { CatalogDefinition, CatalogItem } from '../../shared/models/catalog.models';
import { ConfirmDialogService } from '../../shared/services/confirm-dialog.service';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';

export interface CatalogManageDialogData {
  definition: CatalogDefinition;
  childDefinition?: CatalogDefinition;
  parentId?: number;
  parentLabel?: string;
}

@Component({
  selector: 'app-catalog-manage-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  template: `
    <h2 mat-dialog-title class="dialog-title">
      <span class="dialog-title-icon-box">
        <mat-icon>list_alt</mat-icon>
      </span>
      <span class="dialog-title-text">
        <strong>{{ data.definition.displayName }}</strong>
        @if (data.parentLabel) {
          <span class="dialog-title-subtitle">{{ data.parentLabel }}</span>
        } @else {
          <span class="dialog-title-subtitle">Administra los elementos del catálogo</span>
        }
      </span>
    </h2>

    <mat-divider />

    <mat-dialog-content class="dialog-content">
      @if (loading()) {
        <mat-progress-bar mode="indeterminate" />
      }

      <form [formGroup]="form" class="item-form" (ngSubmit)="save()">
        <mat-form-field appearance="outline" class="form-field form-field--name">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="name" maxlength="200" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="form-field form-field--order">
          <mat-label>Orden</mat-label>
          <input matInput type="number" formControlName="displayOrder" />
        </mat-form-field>

        <mat-slide-toggle formControlName="isActive" class="form-field--active">Activo</mat-slide-toggle>

        <div class="form-actions">
          @if (editingId() !== null) {
            <button mat-button type="button" (click)="cancelEdit()">Cancelar</button>
          }
          <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">
            <mat-icon>{{ editingId() !== null ? 'save' : 'add' }}</mat-icon>
            {{ editingId() !== null ? 'Guardar cambios' : 'Agregar' }}
          </button>
        </div>
      </form>

      <div class="item-list">
        @if (items().length === 0 && !loading()) {
          <div class="empty-state">
            <mat-icon>inbox</mat-icon>
            <p>Este catálogo no tiene elementos registrados.</p>
          </div>
        }

        @for (item of items(); track item.id) {
          <div class="item-row" [class.item-row--inactive]="!item.isActive">
            <div class="item-main">
              <span class="item-order">{{ item.displayOrder }}</span>
              <span class="item-name">{{ item.name }}</span>
              @if (!item.isActive) {
                <span class="item-inactive-badge">Inactivo</span>
              }
            </div>

            <div class="item-actions">
              @if (data.childDefinition) {
                <button
                  mat-icon-button
                  type="button"
                  matTooltip="Gestionar {{ data.childDefinition.displayName }}"
                  (click)="manageChildren(item)"
                >
                  <mat-icon>account_tree</mat-icon>
                </button>
              }
              <button mat-icon-button type="button" matTooltip="Editar" (click)="edit(item)">
                <mat-icon>edit</mat-icon>
              </button>
              <button mat-icon-button type="button" matTooltip="Eliminar" (click)="remove(item)">
                <mat-icon>delete_outline</mat-icon>
              </button>
            </div>
          </div>
        }
      </div>
    </mat-dialog-content>

    <mat-divider />

    <mat-dialog-actions align="end" class="dialog-actions">
      <button mat-flat-button color="primary" type="button" mat-dialog-close>Cerrar</button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .dialog-title {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-6) var(--space-6) var(--space-5) !important;
        margin: 0 !important;
      }

      .dialog-title-icon-box {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 48px;
        height: 48px;
        border-radius: var(--radius-xl);
        background: var(--color-primary-50);
        color: var(--color-primary-700);
        flex-shrink: 0;
        border: 1px solid var(--color-primary-100);
      }

      .dialog-title-icon-box mat-icon {
        font-size: 24px;
        width: 24px;
        height: 24px;
      }

      .dialog-title-text {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }

      .dialog-title-text strong {
        font-size: var(--font-size-h5);
        font-weight: var(--font-weight-extrabold);
        color: var(--color-on-surface);
        letter-spacing: -0.01em;
      }

      .dialog-title-subtitle {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-regular);
      }

      .dialog-content {
        width: 540px;
        max-width: 80vw;
        max-height: 65vh;
        padding: var(--space-6) !important;
      }

      .item-form {
        display: flex;
        flex-wrap: wrap;
        align-items: flex-start;
        gap: var(--space-3);
        padding: var(--space-5);
        margin: 0 0 var(--space-5);
        background: var(--color-surface-container-low);
        border-radius: var(--radius-xl);
        border: 1px solid var(--color-border-light);
      }

      .form-field {
        margin-bottom: 0;
      }

      .form-field--name {
        flex: 1 1 220px;
      }

      .form-field--order {
        flex: 0 1 100px;
      }

      .form-field--active {
        align-self: center;
        margin-top: var(--space-2);
      }

      .form-actions {
        flex-basis: 100%;
        display: flex;
        justify-content: flex-end;
        gap: var(--space-2);
      }

      .item-list {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .item-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-3);
        padding: var(--space-3) var(--space-4);
        border-radius: var(--radius-lg);
        border: 1px solid var(--color-border-light);
        background: #ffffff;
        transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
      }

      .item-row:hover {
        border-color: var(--color-primary-200);
        box-shadow: var(--shadow-xs);
      }

      .item-row--inactive {
        opacity: 0.6;
        background: var(--color-surface-container-low);
      }

      .item-main {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        min-width: 0;
      }

      .item-order {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        min-width: 28px;
        height: 28px;
        border-radius: var(--radius-full);
        background: var(--color-surface-container-low);
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-bold);
        flex-shrink: 0;
      }

      .item-name {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        font-weight: var(--font-weight-semibold);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .item-inactive-badge {
        font-size: var(--font-size-label);
        color: var(--color-on-surface-secondary);
        border: 1px solid var(--color-border-light);
        border-radius: var(--radius-md);
        padding: 1px var(--space-2);
        font-weight: var(--font-weight-semibold);
      }

      .item-actions {
        display: flex;
        align-items: center;
        gap: var(--space-1);
        flex-shrink: 0;
      }

      .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--space-3);
        padding: var(--space-12) var(--space-4);
        text-align: center;
      }

      .empty-state mat-icon {
        font-size: 3rem;
        width: 3rem;
        height: 3rem;
        color: var(--color-primary-200);
      }

      .empty-state p {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-medium);
        max-width: 320px;
        line-height: 1.5;
      }

      .dialog-actions {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: var(--space-3);
        padding: var(--space-4) var(--space-6) var(--space-6) !important;
        margin: 0 !important;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CatalogManageDialogComponent {
  readonly data = inject<CatalogManageDialogData>(MAT_DIALOG_DATA);
  private readonly catalogsApi = inject(CatalogsApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly dialog = inject(MatDialog);

  readonly items = signal<CatalogItem[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<number | null>(null);

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    displayOrder: [0, [Validators.required]],
    isActive: [true]
  });

  constructor() {
    this.loadItems();
  }

  edit(item: CatalogItem): void {
    this.editingId.set(item.id);
    this.form.setValue({ name: item.name, displayOrder: item.displayOrder, isActive: item.isActive });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', displayOrder: this.nextDisplayOrder(), isActive: true });
  }

  save(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const request = {
      name: (value.name ?? '').trim(),
      displayOrder: value.displayOrder ?? 0,
      isActive: value.isActive ?? true,
      parentId: this.data.parentId ?? null
    };

    this.saving.set(true);
    const editingId = this.editingId();
    const request$ = editingId
      ? this.catalogsApi.updateCatalogItem(this.data.definition.key, editingId, request)
      : this.catalogsApi.createCatalogItem(this.data.definition.key, request);

    request$.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.snackBar.open(editingId ? 'Elemento actualizado' : 'Elemento agregado', 'Cerrar', { duration: 3000 });
        this.cancelEdit();
        this.loadItems();
      },
      error: (err: HttpErrorResponse) =>
        this.snackBar.open(extractErrorMessage(err, 'Error al guardar el elemento'), 'Cerrar', { duration: 5000 })
    });
  }

  remove(item: CatalogItem): void {
    this.confirmDialog
      .confirm({
        title: 'Eliminar elemento',
        message: `¿Seguro que deseas eliminar "${item.name}"? El elemento quedará inactivo y dejará de estar disponible en los formularios.`,
        confirmLabel: 'Eliminar',
        icon: 'delete_outline',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (!confirmed) return;

        this.catalogsApi.deleteCatalogItem(this.data.definition.key, item.id).subscribe({
          next: () => {
            this.snackBar.open('Elemento eliminado', 'Cerrar', { duration: 3000 });
            this.loadItems();
          },
          error: (err: HttpErrorResponse) =>
            this.snackBar.open(extractErrorMessage(err, 'Error al eliminar el elemento'), 'Cerrar', { duration: 5000 })
        });
      });
  }

  manageChildren(item: CatalogItem): void {
    if (!this.data.childDefinition) return;

    this.dialog
      .open(CatalogManageDialogComponent, {
        data: {
          definition: this.data.childDefinition,
          parentId: item.id,
          parentLabel: item.name
        } satisfies CatalogManageDialogData,
        width: '600px',
        autoFocus: false,
        restoreFocus: true,
        panelClass: 'catalog-manage-dialog-panel'
      })
      .afterClosed()
      .subscribe(() => this.loadItems());
  }

  private loadItems(): void {
    this.loading.set(true);
    this.catalogsApi.getCatalogItems(this.data.definition.key, this.data.parentId).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
        this.form.patchValue({ displayOrder: this.nextDisplayOrder() });
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.snackBar.open(extractErrorMessage(err, 'Error al cargar el catálogo'), 'Cerrar', { duration: 5000 });
      }
    });
  }

  private nextDisplayOrder(): number {
    const items = this.items();
    return items.length === 0 ? 1 : Math.max(...items.map((x) => x.displayOrder)) + 1;
  }
}
