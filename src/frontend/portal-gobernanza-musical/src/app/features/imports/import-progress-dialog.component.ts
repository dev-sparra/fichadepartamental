import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Observable, Subscription, interval } from 'rxjs';

import { OFFICIAL_IMPORT_FILE_NAME } from '../../shared/utils/import-file-rules.util';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';
import { ImportTone, ImportWorkbookResult } from '../../shared/models/import.models';
import {
  IMPORT_FLOW_STAGES,
  ImportStage,
  ImportStageKey,
  ImportStageState,
  resolveFailedStage
} from './import-flow.constant';

export interface ImportProgressDialogData {
  fileName: string;
  /** Petición de carga. La abre el diálogo para poder acompañarla con el avance del flujo. */
  request$: Observable<ImportWorkbookResult>;
}

export type ImportProgressOutcome =
  | { kind: 'result'; result: ImportWorkbookResult }
  | { kind: 'error'; error: HttpErrorResponse };

/** Cada cuánto se marca como alcanzada la siguiente etapa mientras el servidor responde. */
const STAGE_TICK_MS = 400;

/**
 * Modal que acompaña la carga del archivo: muestra el flujo de la importación mientras el
 * servidor trabaja y termina mostrando el resultado real de la carga.
 *
 * El servidor ejecuta todo el flujo en una sola petición, así que mientras espera la respuesta el
 * avance por las primeras etapas es una estimación de tiempo (no un reporte del servidor). El
 * estado final —etapa en la que se detuvo, observaciones o éxito— sí sale de la respuesta.
 */
@Component({
  selector: 'app-import-progress-dialog',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule],
  template: `
    <section class="progress-dialog">
      <header class="pd-head">
        <div class="pd-icon" [class]="'pd-icon--' + tone()">
          <mat-icon [class.spin]="running()">{{ headIcon() }}</mat-icon>
        </div>
        <div class="pd-head-text">
          <h2 class="pd-title">{{ title() }}</h2>
          <p class="pd-file">
            <mat-icon>description</mat-icon>
            <span>{{ fileName }}</span>
          </p>
        </div>
      </header>

      @if (running()) {
        <mat-progress-bar mode="indeterminate" class="pd-bar" />
      }

      <ol class="flow-list">
        @for (stage of stages; track stage.key; let index = $index) {
          <li class="flow-item" [class]="'flow-item--' + stageState(stage.key)">
            <div class="flow-marker">
              <mat-icon>{{ stageIcon(stage) }}</mat-icon>
            </div>
            <div class="flow-body">
              <strong class="flow-label">{{ index + 1 }}. {{ stage.label }}</strong>
              <span class="flow-detail">{{ stage.detail }}</span>
              @if (stageState(stage.key) === 'failed') {
                <span class="flow-state-note">
                  La importación se detuvo en esta etapa. No se guardó ningún dato.
                </span>
              }
            </div>
          </li>
        }
      </ol>

      @if (!running()) {
        @if (result(); as result) {
          <div class="pd-summary" [class]="'pd-summary--' + tone()">
            <strong>{{ result.statusLabel }}</strong>
            <span>{{ result.statusDescription }}</span>
            <span class="pd-next">
              <mat-icon>arrow_forward</mat-icon>
              <span><strong>Siguiente paso:</strong> {{ result.statusNextStep }}</span>
            </span>
          </div>
        } @else {
          <div class="pd-summary pd-summary--error">
            <strong>No fue posible cargar el archivo</strong>
            <span>{{ errorMessage() }}</span>
            <span class="pd-next">
              <mat-icon>arrow_forward</mat-icon>
              <span>
                <strong>Siguiente paso:</strong> Verifique que esté usando el archivo oficial
                {{ officialFileName }} e intente de nuevo.
              </span>
            </span>
          </div>
        }
      }

      <div class="pd-actions">
        <button mat-flat-button type="button" class="pd-close" [disabled]="running()" (click)="close()">
          @if (running()) {
            Procesando el archivo...
          } @else {
            Ver el resultado
          }
        </button>
      </div>
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .progress-dialog {
        display: flex;
        flex-direction: column;
        gap: var(--space-4);
        min-width: 320px;
        max-width: 520px;
      }

      .pd-head {
        display: flex;
        align-items: center;
        gap: var(--space-4);
      }

      .pd-icon {
        width: 48px;
        height: 48px;
        flex-shrink: 0;
        border-radius: var(--radius-full);
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-primary-50);
        color: var(--color-primary-700);
      }

      .pd-icon--success {
        background: var(--color-success-bg);
        color: var(--color-success-text);
      }

      .pd-icon--warning {
        background: var(--color-warning-bg);
        color: var(--color-warning-text);
      }

      .pd-icon--error {
        background: var(--color-error-bg);
        color: var(--color-error-text);
      }

      .pd-head-text {
        display: flex;
        flex-direction: column;
        gap: 2px;
        min-width: 0;
      }

      .pd-title {
        margin: 0;
        font-size: var(--font-size-h5);
        font-weight: var(--font-weight-extrabold);
        color: var(--color-on-surface);
        letter-spacing: -0.01em;
      }

      .pd-file {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        margin: 0;
        min-width: 0;
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
      }

      .pd-file .mat-icon {
        font-size: 1rem;
        width: 1rem;
        height: 1rem;
        flex-shrink: 0;
      }

      .pd-file span {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .pd-bar {
        border-radius: var(--radius-full);
      }

      .spin {
        animation: spin 1.4s linear infinite;
      }

      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }

      /* ── Flujo (mismo recorrido que ejecuta el servidor) ── */
      .flow-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        max-height: 46vh;
        overflow-y: auto;
      }

      .flow-item {
        display: flex;
        gap: var(--space-4);
        padding-bottom: var(--space-4);
        position: relative;
      }

      .flow-item:not(:last-child)::before {
        content: '';
        position: absolute;
        left: 15px;
        top: 32px;
        bottom: 0;
        width: 2px;
        background: var(--color-border-light);
      }

      .flow-marker {
        flex-shrink: 0;
        width: 32px;
        height: 32px;
        border-radius: var(--radius-full);
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-surface-container-low);
        border: 1px solid var(--color-border-light);
        color: var(--color-on-surface-variant);
        z-index: 1;
      }

      .flow-marker .mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }

      .flow-item--done .flow-marker {
        background: var(--color-success-bg);
        border-color: var(--color-success-border);
        color: var(--color-success-text);
      }

      .flow-item--warning .flow-marker {
        background: var(--color-warning-bg);
        border-color: var(--color-warning-border);
        color: var(--color-warning-text);
      }

      .flow-item--active .flow-marker {
        background: var(--color-primary-100);
        border-color: var(--color-primary-300);
        color: var(--color-primary-700);
      }

      .flow-item--failed .flow-marker {
        background: var(--color-error-bg);
        border-color: var(--color-error-border);
        color: var(--color-error-text);
      }

      .flow-body {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }

      .flow-label {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
      }

      .flow-detail {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        line-height: 1.5;
      }

      .flow-state-note {
        margin-top: var(--space-1);
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-semibold);
        color: var(--color-error-text);
      }

      .flow-item--pending .flow-label,
      .flow-item--pending .flow-detail {
        opacity: 0.55;
      }

      /* ── Resultado ── */
      .pd-summary {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        padding: var(--space-4);
        border-radius: var(--radius-lg);
        border: 1px solid var(--color-border-light);
        background: var(--color-surface-container-low);
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        line-height: 1.6;
      }

      .pd-summary strong {
        color: var(--color-on-surface);
      }

      .pd-summary--success {
        background: var(--color-success-bg);
        border-color: var(--color-success-border);
      }

      .pd-summary--warning {
        background: var(--color-warning-bg);
        border-color: var(--color-warning-border);
      }

      .pd-summary--error {
        background: var(--color-error-bg);
        border-color: var(--color-error-border);
      }

      .pd-next {
        display: flex;
        align-items: flex-start;
        gap: var(--space-2);
      }

      .pd-next .mat-icon {
        font-size: 1.125rem;
        width: 1.125rem;
        height: 1.125rem;
        flex-shrink: 0;
      }

      .pd-actions {
        display: flex;
        justify-content: flex-end;
      }

      ::ng-deep .pd-close:not([disabled]) {
        background-color: var(--color-primary-900) !important;
        color: #ffffff !important;
      }

      .pd-close {
        min-height: 42px;
        min-width: 180px;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ImportProgressDialogComponent implements OnDestroy {
  private readonly data = inject<ImportProgressDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef =
    inject<MatDialogRef<ImportProgressDialogComponent, ImportProgressOutcome>>(MatDialogRef);

  readonly stages: ImportStage[] = IMPORT_FLOW_STAGES;
  readonly fileName = this.data.fileName;
  readonly officialFileName = OFFICIAL_IMPORT_FILE_NAME;

  readonly running = signal(true);
  readonly result = signal<ImportWorkbookResult | null>(null);
  readonly errorMessage = signal('');

  /**
   * Etapa que se está mostrando mientras se espera la respuesta del servidor. Empieza en la
   * segunda: cuando la modal se abre el archivo ya está elegido y enviado.
   */
  private readonly reachedIndex = signal(1);
  /** Etapa en la que se detuvo la importación, deducida de los códigos de incidencia. */
  private readonly failedStage = signal<ImportStageKey | null>(null);

  /** Hasta dónde avanza la animación: lo que sigue depende del resultado de la carga. */
  private readonly lastAnimatedIndex = IMPORT_FLOW_STAGES.findIndex((stage) => stage.key === 'processing');

  private outcome: ImportProgressOutcome | null = null;
  private readonly ticker: Subscription;
  private readonly request: Subscription;

  constructor() {
    this.ticker = interval(STAGE_TICK_MS).subscribe(() => {
      if (this.reachedIndex() < this.lastAnimatedIndex) {
        this.reachedIndex.update((index) => index + 1);
      }
    });

    this.request = this.data.request$.subscribe({
      next: (result) => {
        this.result.set(result);
        if (!result.accepted) {
          this.failedStage.set(resolveFailedStage(result.issues.map((issue) => issue.errorCode)));
        }
        this.finish({ kind: 'result', result });
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(extractErrorMessage(error, 'No fue posible cargar el archivo.'));
        this.failedStage.set('format');
        this.finish({ kind: 'error', error });
      }
    });
  }

  ngOnDestroy(): void {
    this.ticker.unsubscribe();
    this.request.unsubscribe();
  }

  /** Estado visual de cada etapa: estimado mientras se procesa, real cuando llega la respuesta. */
  stageState(key: ImportStageKey): ImportStageState {
    const index = this.stages.findIndex((stage) => stage.key === key);

    if (this.running()) {
      if (index < this.reachedIndex()) return 'done';
      return index === this.reachedIndex() ? 'active' : 'pending';
    }

    const failed = this.failedStage();
    if (failed) {
      const failedIndex = this.stages.findIndex((stage) => stage.key === failed);
      if (index < failedIndex) return 'done';
      return index === failedIndex ? 'failed' : 'pending';
    }

    const result = this.result();
    if (result?.accepted) {
      // El estado lo decide el servidor: hay observaciones que aparecen al guardar (no solo al
      // validar) y por eso no siempre se reflejan en los contadores de filas.
      const withObservations = result.statusTone !== 'success';
      if (key === 'available') {
        return result.persistedRecordCount > 0 ? (withObservations ? 'warning' : 'done') : 'pending';
      }
      if (key === 'completed') {
        return withObservations ? 'warning' : 'done';
      }
      return 'done';
    }

    return 'pending';
  }

  stageIcon(stage: ImportStage): string {
    const state = this.stageState(stage.key);
    if (state === 'done') return 'check';
    if (state === 'failed') return 'close';
    if (state === 'warning') return 'priority_high';
    return stage.icon;
  }

  tone(): ImportTone {
    if (this.running()) return 'progress';

    const result = this.result();
    if (!result) return 'error';
    return result.statusTone;
  }

  headIcon(): string {
    switch (this.tone()) {
      case 'success':
        return 'check_circle';
      case 'warning':
        return 'warning';
      case 'error':
        return 'cancel';
      default:
        return 'sync';
    }
  }

  title(): string {
    if (this.running()) return 'Importando la ficha';
    return this.result()?.statusLabel ?? 'Importación fallida';
  }

  close(): void {
    if (this.outcome) {
      this.dialogRef.close(this.outcome);
    }
  }

  private finish(outcome: ImportProgressOutcome): void {
    this.outcome = outcome;
    this.ticker.unsubscribe();
    this.running.set(false);
  }
}
