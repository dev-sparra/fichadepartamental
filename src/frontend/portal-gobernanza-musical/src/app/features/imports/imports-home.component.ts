import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';

import { AuthTokenService } from '../../core/services/auth-token.service';
import { ImportsApiService } from '../../core/services/imports-api.service';
import { AppRoles } from '../../core/constants/app-roles.constant';
import { ConfirmDialogService } from '../../shared/services/confirm-dialog.service';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';
import { ImportBatchSummary, ImportValidationIssue } from '../../shared/models/import.models';

function severityColor(severity: string): string {
  switch (severity.toLowerCase()) {
    case 'error':
      return 'var(--color-error)';
    case 'warning':
      return 'var(--color-warning)';
    case 'info':
      return 'var(--color-info)';
    default:
      return 'var(--color-on-surface-variant)';
  }
}

function batchStatusColor(status: string): string {
  switch (status.toLowerCase()) {
    case 'completed':
      return 'var(--color-success)';
    case 'processing':
      return 'var(--color-info)';
    case 'failed':
      return 'var(--color-error)';
    default:
      return 'var(--color-on-surface-variant)';
  }
}

@Component({
  selector: 'app-imports-home',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatIconModule,
    MatProgressBarModule,
    MatSnackBarModule
  ],
  template: `
    <section class="imports-page">
      <header class="imp-header">
        <h1 class="imp-title">Importaciones</h1>
        <p class="imp-subtitle">Carga masiva de datos desde archivos Excel</p>
      </header>

      <!-- Upload zone -->
      <mat-card class="imp-upload-card">
        <mat-card-header>
          <mat-card-title>Cargar archivo</mat-card-title>
        </mat-card-header>
        <mat-divider />
        <mat-card-content>
          <div
            class="drop-zone"
            [class.drop-zone--active]="isDragging()"
            [class.drop-zone--has-file]="!!selectedFile()"
            (dragover)="onDragOver($event)"
            (dragleave)="onDragLeave($event)"
            (drop)="onDrop($event)"
          >
            <input
              #fileInput
              type="file"
              accept=".xlsx,.xlsm"
              class="file-input-hidden"
              (change)="selectFile($event)"
              aria-label="Seleccionar archivo Excel"
            />

            @if (selectedFile(); as file) {
              <div class="drop-file-info">
                <mat-icon class="drop-file-icon">description</mat-icon>
                <div class="drop-file-details">
                  <strong>{{ file.name }}</strong>
                  <span>{{ (file.size / 1024 / 1024) | number: '1.1-1' }} MB</span>
                </div>
                <button mat-icon-button type="button" (click)="clearFile()" aria-label="Quitar archivo">
                  <mat-icon>close</mat-icon>
                </button>
              </div>
            } @else {
              <mat-icon class="drop-icon">cloud_upload</mat-icon>
              <p class="drop-text">
                <strong>Arrastra un archivo aquí</strong> o haz clic para seleccionar
              </p>
              <p class="drop-hint">Formatos aceptados: .xlsx, .xlsm (máx. 10 MB)</p>
            }

            @if (uploadProgress() > 0 && uploadProgress() < 100) {
              <mat-progress-bar mode="determinate" [value]="uploadProgress()" class="drop-progress" />
            }

            @if (message()) {
              <div class="imp-message" [class]="'imp-message--' + messageType()">
                <mat-icon>{{ messageType() === 'error' ? 'error_outline' : 'check_circle' }}</mat-icon>
                <span>{{ message() }}</span>
              </div>
            }
          </div>

          <div class="upload-actions">
            <button
              mat-flat-button
              color="primary"
              type="button"
              [disabled]="!selectedFile() || loading()"
              (click)="upload()"
            >
              @if (loading()) {
                <ng-container>
                  <mat-icon class="spin">sync</mat-icon>
                  Procesando...
                </ng-container>
              } @else {
                <ng-container>
                  <mat-icon>upload</mat-icon>
                  Cargar y validar
                </ng-container>
              }
            </button>

            <p class="upload-help">
              <mat-icon class="help-icon">info</mat-icon>
              El archivo debe seguir el formato de <strong>ficha_departamental_gobernanza.xlsm</strong>.
              <a href="#" (click)="downloadTemplate($event)">
                @if (downloadingTemplate()) {
                  Descargando plantilla...
                } @else {
                  Descargar plantilla
                }
              </a>
            </p>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Batch list -->
      <mat-card class="imp-batch-card">
        <mat-card-header>
          <mat-card-title>Historial de lotes</mat-card-title>
        </mat-card-header>
        <mat-divider />
        @if (batches().length > 0) {
          <div class="batch-list">
            @for (batch of batches(); track batch.importBatchId) {
              <div
                class="batch-row"
                [class.batch-row--selected]="selectedBatch()?.importBatchId === batch.importBatchId"
                (click)="loadIssues(batch)"
                role="button"
                tabindex="0"
                [attr.aria-label]="'Ver incidencias del lote ' + batch.fileName"
                (keydown.enter)="loadIssues(batch)"
              >
                <div class="batch-main">
                  <mat-icon class="batch-icon">table_chart</mat-icon>
                  <div class="batch-info">
                    <strong class="batch-name">{{ batch.fileName }}</strong>
                    <span class="batch-date">{{ batch.startedAtUtc | date: 'medium' }}</span>
                  </div>
                </div>

                <div class="batch-status">
                  <span class="status-badge" [style.--badge-color]="batchStatusColor(batch.status)">
                    {{ batch.status }}
                  </span>
                </div>

                <div class="batch-stats">
                  <span class="batch-stat batch-stat--ok">
                    <mat-icon>check</mat-icon>{{ batch.validRowCount }}
                  </span>
                  <span class="batch-stat batch-stat--err">
                    <mat-icon>close</mat-icon>{{ batch.invalidRowCount }}
                  </span>
                  <span class="batch-stat batch-stat--warn">
                    <mat-icon>warning</mat-icon>{{ batch.warningCount }}
                  </span>
                </div>

                @if (canDelete()) {
                  <button
                    mat-icon-button
                    type="button"
                    class="batch-delete-btn"
                    [attr.aria-label]="'Eliminar lote ' + batch.fileName"
                    (click)="deleteBatch(batch, $event)"
                  >
                    <mat-icon>delete_outline</mat-icon>
                  </button>
                }

                <mat-icon class="batch-arrow">chevron_right</mat-icon>
              </div>
              @if (!$last) {
                <mat-divider />
              }
            }
          </div>
        } @else {
          <mat-card-content class="imp-empty">
            <mat-icon>history_off</mat-icon>
            <p>No hay lotes de importación. Carga tu primer archivo.</p>
          </mat-card-content>
        }
      </mat-card>

      <!-- Issues detail -->
      @if (selectedBatch()) {
        <mat-card class="imp-issues-card">
          <mat-card-header>
            <mat-card-title>Incidencias</mat-card-title>
            <mat-card-subtitle>{{ selectedBatch()?.fileName }}</mat-card-subtitle>
          </mat-card-header>
          <mat-divider />
          @if (issues().length > 0) {
            <div class="issues-table-wrapper">
              <table class="issues-table">
                <thead>
                  <tr>
                    <th class="col-sev">Severidad</th>
                    <th>Hoja</th>
                    <th>Celda</th>
                    <th>Código</th>
                    <th class="col-msg">Mensaje</th>
                    <th>Valor</th>
                  </tr>
                </thead>
                <tbody>
                  @for (issue of issues(); track issue.id) {
                    <tr>
                      <td class="col-sev">
                        <span class="sev-badge" [style.--sev-color]="severityColor(issue.severity)">
                          {{ issue.severity }}
                        </span>
                      </td>
                      <td>{{ issue.sheetName }}</td>
                      <td class="col-mono">{{ issue.cellReference ?? '---' }}</td>
                      <td class="col-mono">{{ issue.errorCode }}</td>
                      <td class="col-msg">{{ issue.message }}</td>
                      <td class="col-mono col-val">{{ issue.rawValue ?? '---' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          } @else {
            <mat-card-content class="imp-empty">
              <mat-icon>check_circle_outline</mat-icon>
              <p>Sin incidencias en este lote.</p>
            </mat-card-content>
          }
        </mat-card>
      }
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .imports-page {
        display: flex;
        flex-direction: column;
        gap: var(--space-7);
      }

      .imp-header {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .imp-title {
        font-size: 1.75rem;
        font-weight: var(--font-weight-extrabold);
        margin: 0;
        color: var(--color-on-surface);
        letter-spacing: -0.02em;
      }

      .imp-subtitle {
        margin: 0;
        color: var(--color-on-surface-secondary);
        font-size: var(--font-size-body);
      }

      /* ── Upload card ── */
      .imp-upload-card {
        border-radius: var(--radius-xl);
        border: 1px solid var(--color-border-light);
        box-shadow: var(--shadow-sm);
      }

      .drop-zone {
        border: 2px dashed var(--color-border);
        border-radius: var(--radius-xl);
        padding: var(--space-10) var(--space-6);
        text-align: center;
        cursor: pointer;
        transition:
          border-color var(--transition-fast),
          background-color var(--transition-fast);
        position: relative;
      }

      .drop-zone:hover {
        border-color: var(--color-primary-300);
        background: var(--color-primary-50);
      }

      .drop-zone--active {
        border-color: var(--color-primary-500);
        background: var(--color-primary-100);
      }

      .drop-zone--has-file {
        border-style: solid;
        border-color: var(--color-primary-300);
        padding: var(--space-6);
      }

      .file-input-hidden {
        position: absolute;
        inset: 0;
        opacity: 0;
        cursor: pointer;
      }

      .drop-icon {
        font-size: 3rem;
        width: 3rem;
        height: 3rem;
        color: var(--color-border);
        margin-bottom: var(--space-3);
      }

      .drop-text {
        margin: 0;
        color: var(--color-on-surface);
      }

      .drop-text strong {
        color: var(--color-primary-600);
      }

      .drop-hint {
        margin: var(--space-2) 0 0;
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-variant);
      }

      .drop-file-info {
        display: flex;
        align-items: center;
        gap: var(--space-3);
      }

      .drop-file-icon {
        color: var(--color-primary-500);
      }

      .drop-file-details {
        flex: 1;
        display: flex;
        flex-direction: column;
        text-align: left;
      }

      .drop-file-details strong {
        font-size: var(--font-size-body);
        color: var(--color-on-surface);
      }

      .drop-file-details span {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
      }

      .drop-progress {
        margin-top: var(--space-4);
        border-radius: var(--radius-full);
      }

      .imp-message {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        padding: var(--space-3) var(--space-4);
        border-radius: var(--radius-lg);
        font-size: var(--font-size-body-sm);
        margin-top: var(--space-4);
      }

      .imp-message--error {
        background: var(--color-error-bg);
        color: var(--color-error-text);
        border: 1px solid var(--color-error-border);
      }

      .imp-message--success {
        background: var(--color-success-bg);
        color: var(--color-success-text);
        border: 1px solid var(--color-success-border);
      }

      .upload-actions {
        display: flex;
        flex-direction: column;
        gap: var(--space-3);
        margin-top: var(--space-4);
      }

      .upload-help {
        display: flex;
        align-items: center;
        gap: var(--space-1);
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        margin: 0;
        flex-wrap: wrap;
      }

      .help-icon {
        font-size: 1rem;
        width: 1rem;
        height: 1rem;
      }

      .upload-help a {
        color: var(--color-primary-600);
      }

      .spin {
        animation: spin 1s linear infinite;
      }

      @keyframes spin {
        to { transform: rotate(360deg); }
      }

      /* ── Batch list ── */
      .imp-batch-card {
        border-radius: var(--radius-xl);
        border: 1px solid var(--color-border-light);
        box-shadow: var(--shadow-sm);
      }

      .batch-list {
        display: flex;
        flex-direction: column;
      }

      .batch-row {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-4);
        cursor: pointer;
        transition: background var(--transition-fast);
      }

      .batch-row:hover {
        background: var(--color-hover);
      }

      .batch-row--selected {
        background: var(--color-primary-50);
      }

      .batch-main {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        min-width: 0;
        flex: 1;
      }

      .batch-icon {
        color: var(--color-primary-500);
        flex-shrink: 0;
      }

      .batch-info {
        display: flex;
        flex-direction: column;
        min-width: 0;
      }

      .batch-name {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .batch-date {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
      }

      .batch-status {
        flex-shrink: 0;
      }

      .status-badge {
        display: inline-block;
        padding: 2px var(--space-2);
        border-radius: var(--radius-md);
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-semibold);
        text-transform: uppercase;
        color: var(--badge-color);
        background: color-mix(in srgb, var(--badge-color) 12%, transparent);
        border: 1px solid color-mix(in srgb, var(--badge-color) 25%, transparent);
      }

      .batch-stats {
        display: flex;
        gap: var(--space-3);
        flex-shrink: 0;
      }

      .batch-stat {
        display: flex;
        align-items: center;
        gap: 2px;
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-medium);
      }

      .batch-stat .mat-icon {
        font-size: 0.875rem;
        width: 0.875rem;
        height: 0.875rem;
      }

      .batch-stat--ok { color: var(--color-success); }
      .batch-stat--err { color: var(--color-error); }
      .batch-stat--warn { color: var(--color-warning); }

.batch-arrow {
      color: var(--color-border);
      flex-shrink: 0;
    }

    .batch-delete-btn {
      color: var(--color-on-surface-variant);
      flex-shrink: 0;
      transition: color var(--transition-fast), background-color var(--transition-fast);
    }

    .batch-delete-btn:hover {
      color: var(--color-error);
      background: var(--color-error-bg);
    }

      /* ── Issues table ── */
      .imp-issues-card {
        border-radius: var(--radius-xl);
        border: 1px solid var(--color-border-light);
        box-shadow: var(--shadow-sm);
      }

      .issues-table-wrapper {
        overflow-x: auto;
      }

      .issues-table {
        width: 100%;
        border-collapse: collapse;
        font-size: var(--font-size-body-sm);
      }

      .issues-table thead {
        background: var(--color-surface-container-low);
      }

      .issues-table th {
        padding: var(--space-3) var(--space-4);
        text-align: left;
        font-weight: var(--font-weight-bold);
        color: var(--color-primary-900);
        font-size: var(--font-size-label);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        border-bottom: 1px solid var(--color-border-light);
        white-space: nowrap;
      }

      .issues-table td {
        padding: var(--space-3) var(--space-4);
        border-bottom: 1px solid var(--color-border-light);
        color: var(--color-on-surface);
      }

      .issues-table tbody tr:hover {
        background: var(--color-hover);
      }

      .issues-table tbody tr:last-child td {
        border-bottom: none;
      }

      .col-sev { width: 90px; }
      .col-msg { min-width: 200px; }
      .col-mono {
        font-family: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
        font-size: var(--font-size-caption);
      }
      .col-val {
        max-width: 160px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .sev-badge {
        display: inline-block;
        padding: 2px var(--space-2);
        border-radius: var(--radius-md);
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-semibold);
        text-transform: uppercase;
        color: var(--sev-color);
        background: color-mix(in srgb, var(--sev-color) 10%, transparent);
      }

      /* ── Empty state ── */
      .imp-empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-12) var(--space-6);
        text-align: center;
      }

      .imp-empty mat-icon {
        font-size: 3.5rem;
        width: 3.5rem;
        height: 3.5rem;
        color: var(--color-primary-200);
      }

      .imp-empty p {
        color: var(--color-on-surface-secondary);
        margin: 0;
        max-width: 360px;
        font-size: var(--font-size-body-sm);
        line-height: 1.6;
      }

      /* ── Responsive ── */
      @media (max-width: 768px) {
        .batch-row {
          flex-wrap: wrap;
          gap: var(--space-2);
        }

        .batch-stats {
          width: 100%;
          justify-content: flex-start;
        }

        .batch-arrow {
          display: none;
        }

        .issues-table th:nth-child(4),
        .issues-table td:nth-child(4),
        .issues-table th:nth-child(3),
        .issues-table td:nth-child(3) {
          display: none;
        }
      }

      @media (max-width: 480px) {
        .drop-zone {
          padding: var(--space-6) var(--space-4);
        }

        .batch-status {
          order: -1;
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ImportsHomeComponent {
  private readonly importsApiService = inject(ImportsApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly authTokenService = inject(AuthTokenService);

  readonly loading = signal(false);
  readonly uploadProgress = signal(0);
  readonly message = signal('');
  readonly messageType = signal<'error' | 'success'>('success');
  readonly selectedFile = signal<File | null>(null);
  readonly isDragging = signal(false);

  readonly batches = signal<ImportBatchSummary[]>([]);
  readonly issues = signal<ImportValidationIssue[]>([]);
  readonly selectedBatch = signal<ImportBatchSummary | null>(null);
  readonly downloadingTemplate = signal(false);

  /** Solo el Administrador puede eliminar lotes de importación. */
  readonly canDelete = computed(() => this.authTokenService.hasAnyRole([AppRoles.Administrador]));

  constructor() {
    this.refreshBatches();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);

    const file = event.dataTransfer?.files?.item(0) ?? null;
    if (file) {
      if (!file.name.match(/\.xlsx?m?$/i)) {
        this.snackBar.open('Solo se aceptan archivos Excel (.xlsx, .xlsm)', 'Cerrar', { duration: 5000 });
        return;
      }
      this.selectedFile.set(file);
    }
  }

  selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.item(0) ?? null);
    this.message.set('');
  }

  clearFile(): void {
    this.selectedFile.set(null);
    this.message.set('');
    this.uploadProgress.set(0);
  }

  upload(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.loading.set(true);
    this.message.set('');
    this.uploadProgress.set(10);

    this.importsApiService
      .importExcel(file)
      .pipe(
        finalize(() => {
          this.loading.set(false);
          this.uploadProgress.set(100);
          setTimeout(() => this.uploadProgress.set(0), 1500);
        })
      )
      .subscribe({
        next: (result) => {
          this.uploadProgress.set(90);
          this.issues.set(result.issues);
          this.selectedBatch.set({
            importBatchId: result.importBatchId,
            fileName: this.selectedFile()?.name ?? '',
            status: result.status,
            validRowCount: result.validRowCount,
            invalidRowCount: result.invalidRowCount,
            warningCount: result.warningCount,
            startedAtUtc: new Date().toISOString(),
            completedAtUtc: new Date().toISOString()
          });

          if (result.issues.length > 0 && result.validRowCount === 0 && result.invalidRowCount === 0) {
            this.message.set(`Importación con incidencias. Revisa la tabla de incidencias abajo para ver el detalle.`);
            this.messageType.set('error');
          } else if (result.invalidRowCount > 0) {
            this.message.set(`Lote procesado: ${result.validRowCount} válidos, ${result.invalidRowCount} inválidos. Revisa las incidencias abajo.`);
            this.messageType.set('error');
          } else {
            this.message.set(`Lote procesado correctamente: ${result.validRowCount} filas válidas.`);
            this.messageType.set('success');
          }

          this.selectedFile.set(null);
          this.refreshBatches();
        },
        error: () => {
          this.message.set('Error al procesar el archivo. Verifica el formato e intenta de nuevo.');
          this.messageType.set('error');
        }
      });
  }

  downloadTemplate(event: Event): void {
    event.preventDefault();
    if (this.downloadingTemplate()) return;

    this.downloadingTemplate.set(true);
    this.importsApiService
      .downloadTemplate()
      .pipe(finalize(() => this.downloadingTemplate.set(false)))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = 'ficha_departamental_gobernanza.xlsm';
          link.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.snackBar.open('Error al descargar la plantilla', 'Cerrar', { duration: 5000 })
      });
  }

  loadIssues(batch: ImportBatchSummary): void {
    this.selectedBatch.set(batch);
    this.importsApiService.getIssues(batch.importBatchId).subscribe({
      next: (issues) => this.issues.set(issues),
      error: () => this.snackBar.open('Error al consultar incidencias', 'Cerrar', { duration: 5000 })
    });
  }

  deleteBatch(batch: ImportBatchSummary, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    this.confirmDialog
      .confirm({
        title: 'Eliminar lote de importación',
        message: `¿Deseas eliminar el lote "${batch.fileName}"? Se borrarán las filas staging y las incidencias, pero los datos ya persistidos en fichas e indicadores no se revertirán.`,
        confirmLabel: 'Eliminar',
        icon: 'delete_forever',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (!confirmed) return;

        this.importsApiService.deleteBatch(batch.importBatchId).subscribe({
          next: () => {
            // Si el lote eliminado era el seleccionado, limpiar la tabla de incidencias.
            if (this.selectedBatch()?.importBatchId === batch.importBatchId) {
              this.selectedBatch.set(null);
              this.issues.set([]);
            }
            this.snackBar.open('Lote eliminado correctamente', 'Cerrar', { duration: 3000 });
            this.refreshBatches();
          },
          error: (err: HttpErrorResponse) => {
            this.snackBar.open(extractErrorMessage(err, 'Error al eliminar el lote'), 'Cerrar', { duration: 5000 });
          }
        });
      });
  }

  severityColor(severity: string): string {
    return severityColor(severity);
  }

  batchStatusColor(status: string): string {
    return batchStatusColor(status);
  }

  private refreshBatches(): void {
    this.importsApiService.getBatches().subscribe({
      next: (batches) => this.batches.set(batches),
      error: () => this.snackBar.open('Error al cargar lotes', 'Cerrar', { duration: 5000 })
    });
  }
}
