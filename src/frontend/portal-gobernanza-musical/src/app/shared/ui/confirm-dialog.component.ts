import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'default' | 'danger';
  icon?: string;
  showCommentField?: boolean;
  commentLabel?: string;
}

export interface ConfirmWithCommentResult {
  confirmed: boolean;
  comment: string | null;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  template: `
    <div class="confirm-dialog" [class.confirm-dialog--danger]="data.variant === 'danger'">
      <div class="confirm-icon">
        <mat-icon>{{ data.icon ?? (data.variant === 'danger' ? 'delete_outline' : 'help_outline') }}</mat-icon>
      </div>
      <h2 class="confirm-title">{{ data.title }}</h2>
      <p class="confirm-message">{{ data.message }}</p>
      @if (data.showCommentField) {
        <mat-form-field appearance="outline" class="confirm-comment-field">
          <mat-label>{{ data.commentLabel ?? 'Comentario' }}</mat-label>
          <textarea matInput rows="3" [ngModel]="comment()" (ngModelChange)="comment.set($event)"></textarea>
        </mat-form-field>
      }
      <div class="confirm-actions">
        <button mat-button type="button" class="confirm-cancel" (click)="onCancel()">
          {{ data.cancelLabel ?? 'Cancelar' }}
        </button>
        <button
          mat-flat-button
          type="button"
          class="confirm-btn"
          [class.confirm-btn--danger]="data.variant === 'danger'"
          (click)="onConfirm()"
        >
          {{ data.confirmLabel ?? 'Confirmar' }}
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .confirm-dialog {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        gap: var(--space-3);
        min-width: 300px;
        max-width: 380px;
      }

      .confirm-icon {
        width: 56px;
        height: 56px;
        border-radius: var(--radius-full);
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-primary-50);
        color: var(--color-primary-700);
        margin-bottom: var(--space-1);
      }

      .confirm-dialog--danger .confirm-icon {
        background: var(--color-error-bg);
        color: var(--color-error);
      }

      .confirm-icon mat-icon {
        font-size: 28px;
        width: 28px;
        height: 28px;
      }

      .confirm-title {
        margin: 0;
        font-size: var(--font-size-h5);
        font-weight: var(--font-weight-extrabold);
        color: var(--color-on-surface);
        letter-spacing: -0.01em;
      }

      .confirm-message {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        line-height: 1.6;
      }

      .confirm-actions {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: var(--space-3);
        margin-top: var(--space-4);
        width: 100%;
      }

      .confirm-cancel,
      .confirm-btn {
        flex: 1;
        min-height: 42px;
      }

      ::ng-deep .confirm-btn {
        background-color: var(--color-primary-900) !important;
        color: #ffffff !important;
      }

      ::ng-deep .confirm-btn.confirm-btn--danger {
        background-color: var(--color-error) !important;
        color: #ffffff !important;
      }

      .confirm-comment-field {
        width: 100%;
        margin-top: var(--space-2);
        text-align: left;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmDialogComponent {
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);

  readonly comment = signal('');

  onCancel(): void {
    if (this.data.showCommentField) {
      this.dialogRef.close({ confirmed: false, comment: null } satisfies ConfirmWithCommentResult);
    } else {
      this.dialogRef.close(false);
    }
  }

  onConfirm(): void {
    if (this.data.showCommentField) {
      this.dialogRef.close({
        confirmed: true,
        comment: this.comment().trim() || null
      } satisfies ConfirmWithCommentResult);
    } else {
      this.dialogRef.close(true);
    }
  }
}
