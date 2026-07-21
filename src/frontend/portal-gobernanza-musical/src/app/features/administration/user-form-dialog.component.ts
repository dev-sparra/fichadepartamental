import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { CreateUserRequest, UpdateUserRequest, UserDto } from './services/users-api.service';
import { ConfirmDialogService } from '../../shared/services/confirm-dialog.service';

export interface UserFormDialogData {
  user: UserDto | null;
  availableRoles: string[];
}

export type UserFormDialogResult =
  | { mode: 'create'; request: CreateUserRequest }
  | { mode: 'edit'; id: string; request: UpdateUserRequest };

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule
  ],
  template: `
    <h2 mat-dialog-title class="dialog-title">
      <span class="dialog-title-icon-box">
        <mat-icon>{{ isEdit ? 'manage_accounts' : 'person_add' }}</mat-icon>
      </span>
      <span class="dialog-title-text">
        <strong>{{ isEdit ? 'Editar usuario' : 'Nuevo usuario' }}</strong>
        <span class="dialog-title-subtitle">
          {{ isEdit ? 'Actualiza los datos y permisos de la cuenta.' : 'Crea una cuenta y asigna sus roles de acceso.' }}
        </span>
      </span>
    </h2>

    <mat-divider />

    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content class="dialog-content">
        <section class="form-section">
          <h3 class="form-section-title">Datos de la cuenta</h3>

          <mat-form-field appearance="outline" class="form-field">
            <mat-label>Correo electrónico</mat-label>
            <input matInput type="email" formControlName="email" required autocomplete="email" />
            <mat-icon matPrefix>mail_outline</mat-icon>
            @if (form.controls.email.invalid && form.controls.email.touched) {
              <mat-error>Ingresa un correo electrónico válido</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="form-field">
            <mat-label>Nombre completo</mat-label>
            <input matInput formControlName="displayName" autocomplete="name" />
            <mat-icon matPrefix>badge</mat-icon>
          </mat-form-field>

          @if (!isEdit) {
            <mat-form-field appearance="outline" class="form-field">
              <mat-label>Contraseña</mat-label>
              <input matInput type="password" formControlName="password" required autocomplete="new-password" />
              <mat-icon matPrefix>lock_outline</mat-icon>
              @if (form.controls.password.invalid && form.controls.password.touched) {
                <mat-error>Requerida (mínimo 8 caracteres)</mat-error>
              } @else {
                <mat-hint>Mínimo 8 caracteres</mat-hint>
              }
            </mat-form-field>
          }
        </section>

        <section class="form-section">
          <h3 class="form-section-title">Permisos</h3>

          <mat-form-field appearance="outline" class="form-field">
            <mat-label>Roles</mat-label>
            <mat-select formControlName="roleNames" multiple required>
              @for (role of data.availableRoles; track role) {
                <mat-option [value]="role">{{ role }}</mat-option>
              }
            </mat-select>
            <mat-icon matPrefix>shield_person</mat-icon>
            @if (form.controls.roleNames.invalid && form.controls.roleNames.touched) {
              <mat-error>Selecciona al menos un rol</mat-error>
            } @else {
              <mat-hint>Define qué módulos podrá ver y editar este usuario</mat-hint>
            }
          </mat-form-field>

          @if (isEdit) {
            <div class="status-toggle">
              <div class="status-toggle-info">
                <span class="status-dot" [class.status-dot--active]="form.controls.isActive.value"></span>
                <div class="status-toggle-text">
                  <strong>{{ form.controls.isActive.value ? 'Usuario activo' : 'Usuario inactivo' }}</strong>
                  <p>{{ form.controls.isActive.value ? 'Puede iniciar sesión actualmente.' : 'No puede iniciar sesión actualmente.' }}</p>
                </div>
              </div>
              <button mat-stroked-button type="button" (click)="toggleActive()">
                {{ form.controls.isActive.value ? 'Desactivar' : 'Activar' }}
              </button>
            </div>
          }
        </section>
      </mat-dialog-content>

      <mat-divider />

      <mat-dialog-actions align="end" class="dialog-actions">
        <button mat-button type="button" mat-dialog-close>Cancelar</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          <mat-icon>save</mat-icon>
          {{ isEdit ? 'Guardar cambios' : 'Crear usuario' }}
        </button>
      </mat-dialog-actions>
    </form>
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
        display: flex;
        flex-direction: column;
        gap: var(--space-6);
        padding: var(--space-6) !important;
        min-width: 440px;
      }

      .form-section {
        display: flex;
        flex-direction: column;
        gap: var(--space-4);
      }

      .form-section-title {
        margin: 0;
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-bold);
        text-transform: uppercase;
        letter-spacing: var(--letter-spacing-label);
        color: var(--color-primary-700);
        padding-bottom: var(--space-2);
        border-bottom: 1px solid var(--color-border-light);
      }

      .form-field {
        width: 100%;
      }

      .status-toggle {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-4);
        padding: var(--space-4) var(--space-5);
        background: var(--color-surface-container-low);
        border-radius: var(--radius-lg);
        border: 1px solid var(--color-border-light);
      }

      .status-toggle-info {
        display: flex;
        align-items: center;
        gap: var(--space-3);
      }

      .status-toggle-text strong {
        display: block;
        color: var(--color-on-surface);
        font-size: var(--font-size-body-sm);
      }

      .status-toggle-text p {
        margin: 2px 0 0;
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
      }

      .status-dot {
        display: inline-block;
        width: 12px;
        height: 12px;
        border-radius: var(--radius-full);
        background: var(--color-border);
        flex-shrink: 0;
        box-shadow: 0 0 0 2px #ffffff;
      }

      .status-dot--active {
        background: var(--color-success);
        box-shadow: 0 0 0 2px #ffffff, 0 0 0 3px var(--color-success);
      }

      .dialog-actions {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: var(--space-3);
        padding: var(--space-4) var(--space-6) var(--space-6) !important;
        margin: 0 !important;
      }

      @media (max-width: 640px) {
        .dialog-content {
          min-width: 0;
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly confirmDialog = inject(ConfirmDialogService);
  readonly data = inject<UserFormDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent>);

  readonly isEdit = !!this.data.user;

  readonly form = this.fb.group({
    email: [this.data.user?.email ?? '', [Validators.required, Validators.email]],
    displayName: [this.data.user?.displayName ?? ''],
    password: ['', this.isEdit ? [] : [Validators.required, Validators.minLength(8)]],
    roleNames: [this.data.user?.roles ?? ([] as string[]), [Validators.required, Validators.minLength(1)]],
    isActive: [this.data.user?.isActive ?? true]
  });

  toggleActive(): void {
    const isActive = this.form.controls.isActive.value;
    if (!isActive) {
      this.form.controls.isActive.setValue(true);
      return;
    }

    this.confirmDialog
      .confirm({
        title: 'Desactivar usuario',
        message: 'El usuario no podrá iniciar sesión mientras su cuenta esté inactiva. ¿Deseas continuar?',
        confirmLabel: 'Desactivar',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (confirmed) {
          this.form.controls.isActive.setValue(false);
        }
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    if (this.isEdit) {
      this.dialogRef.close({
        mode: 'edit',
        id: this.data.user!.id,
        request: {
          email: raw.email ?? '',
          displayName: raw.displayName?.trim() || null,
          isActive: raw.isActive ?? false,
          roleNames: raw.roleNames ?? []
        }
      } satisfies UserFormDialogResult);
    } else {
      this.dialogRef.close({
        mode: 'create',
        request: {
          email: raw.email ?? '',
          password: raw.password ?? '',
          displayName: raw.displayName?.trim() || null,
          roleNames: raw.roleNames ?? []
        }
      } satisfies UserFormDialogResult);
    }
  }
}
