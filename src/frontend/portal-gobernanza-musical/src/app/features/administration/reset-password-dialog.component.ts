import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule } from '@angular/material/snack-bar';

import { ResetPasswordResult } from './services/users-api.service';

@Component({
  selector: 'app-reset-password-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatDividerModule,
    MatIconModule,
    MatSnackBarModule
  ],
  template: `
    <h2 mat-dialog-title class="dialog-title">
      <span class="dialog-title-icon-box">
        <mat-icon>vpn_key</mat-icon>
      </span>
      <span class="dialog-title-text">
        <strong>Contraseña restablecida</strong>
        <span class="dialog-title-subtitle">
          Comparte esta contraseña temporal con {{ result.email }} por un canal seguro.
        </span>
      </span>
    </h2>

    <mat-divider />

    <mat-dialog-content class="dialog-content">
      <div class="warning-banner">
        <mat-icon>warning</mat-icon>
        <p>Esta contraseña temporal se mostrará <strong>una sola vez</strong>. El usuario deberá cambiarla en su próximo inicio de sesión.</p>
      </div>

      <div class="password-box">
        <span class="password-label">Contraseña temporal generada</span>
        <div class="password-value-row">
          <code class="password-value">{{ result.temporaryPassword }}</code>
          <button mat-icon-button type="button" (click)="copyToClipboard()" aria-label="Copiar contraseña">
            <mat-icon>content_copy</mat-icon>
          </button>
        </div>
      </div>
    </mat-dialog-content>

    <mat-divider />

    <mat-dialog-actions align="end" class="dialog-actions">
      <button mat-button type="button" mat-dialog-close>Cerrar</button>
      <button mat-flat-button color="primary" type="button" (click)="copyAndClose()">
        <mat-icon>content_copy</mat-icon>
        Copiar y cerrar
      </button>
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
        background: var(--color-warning-bg, #fff8e1);
        color: var(--color-warning, #f57c00);
        flex-shrink: 0;
        border: 1px solid var(--color-warning-border, #ffe0b2);
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
        gap: var(--space-5);
        padding: var(--space-6) !important;
        min-width: 420px;
      }

      .warning-banner {
        display: flex;
        align-items: flex-start;
        gap: var(--space-3);
        padding: var(--space-4) var(--space-5);
        background: var(--color-warning-bg, #fff8e1);
        border: 1px solid var(--color-warning-border, #ffe0b2);
        border-radius: var(--radius-lg);
      }

      .warning-banner mat-icon {
        color: var(--color-warning, #f57c00);
        flex-shrink: 0;
      }

      .warning-banner p {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        line-height: 1.5;
      }

      .password-box {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        padding: var(--space-4) var(--space-5);
        background: var(--color-surface-container-low);
        border: 1px solid var(--color-border-light);
        border-radius: var(--radius-lg);
      }

      .password-label {
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-bold);
        text-transform: uppercase;
        letter-spacing: var(--letter-spacing-label);
        color: var(--color-on-surface-secondary);
      }

      .password-value-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-3);
      }

      .password-value {
        font-family: 'Courier New', monospace;
        font-size: 1.15rem;
        font-weight: var(--font-weight-bold);
        color: var(--color-primary-700);
        letter-spacing: 0.03em;
        user-select: all;
        word-break: break-all;
        flex: 1;
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
export class ResetPasswordDialogComponent {
  readonly result = inject<ResetPasswordResult>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ResetPasswordDialogComponent>);

  copyToClipboard(): void {
    navigator.clipboard.writeText(this.result.temporaryPassword);
  }

  copyAndClose(): void {
    navigator.clipboard.writeText(this.result.temporaryPassword);
    this.dialogRef.close();
  }
}