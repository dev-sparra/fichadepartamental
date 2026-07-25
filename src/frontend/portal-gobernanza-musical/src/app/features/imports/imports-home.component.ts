import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';

import { AuthTokenService } from '../../core/services/auth-token.service';
import { ImportsApiService } from '../../core/services/imports-api.service';
import { AppRoles } from '../../core/constants/app-roles.constant';
import { ConfirmDialogService } from '../../shared/services/confirm-dialog.service';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';
import {
  MAX_IMPORT_FILE_SIZE_BYTES,
  OFFICIAL_IMPORT_FILE_NAME,
  checkImportFile
} from '../../shared/utils/import-file-rules.util';
import {
  ImportBatchSummary,
  ImportTone,
  ImportValidationIssue,
  ImportWorkbookResult
} from '../../shared/models/import.models';
import {
  IMPORT_FLOW_STAGES,
  ImportStage,
  ImportStageKey,
  ImportStageState,
  resolveFailedStage
} from './import-flow.constant';

interface IssueGroup {
  sheetName: string;
  errorCount: number;
  warningCount: number;
  issues: ImportValidationIssue[];
}

function toneColor(tone: ImportTone | string): string {
  switch (tone) {
    case 'success':
      return 'var(--color-success)';
    case 'warning':
      return 'var(--color-warning)';
    case 'error':
      return 'var(--color-error)';
    case 'progress':
      return 'var(--color-info)';
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
    MatExpansionModule,
    MatIconModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  template: `
    <section class="imports-page">
      <header class="imp-header">
        <h1 class="imp-title">Importaciones</h1>
        <p class="imp-subtitle">
          Carga de la Ficha Departamental de Gobernanza diligenciada en el archivo oficial
        </p>
      </header>

      <!-- ── Cargar archivo ── -->
      <mat-card class="imp-card">
        <mat-card-header>
          <mat-card-title>Cargar archivo</mat-card-title>
          <mat-card-subtitle>Solo se admite el archivo oficial {{ officialFileName }}</mat-card-subtitle>
        </mat-card-header>
        <mat-divider />
        <mat-card-content>
          <div
            class="drop-zone"
            [class.drop-zone--active]="isDragging()"
            [class.drop-zone--has-file]="!!selectedFile()"
            [class.drop-zone--invalid]="messageType() === 'error'"
            (dragover)="onDragOver($event)"
            (dragleave)="onDragLeave($event)"
            (drop)="onDrop($event)"
          >
            <input
              #fileInput
              type="file"
              accept=".xlsm"
              class="file-input-hidden"
              (change)="selectFile($event)"
              aria-label="Seleccionar el archivo oficial de la Ficha Departamental de Gobernanza"
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
                <strong>Arrastra el archivo aquí</strong> o haz clic para seleccionarlo
              </p>
              <p class="drop-hint">
                Formato aceptado: {{ officialFileName }} (máx. {{ maxFileSizeMb }} MB)
              </p>
            }

            @if (loading()) {
              <mat-progress-bar mode="indeterminate" class="drop-progress" />
            }
          </div>

          @if (message()) {
            <div class="imp-message" [class]="'imp-message--' + messageType()">
              <mat-icon>{{ messageIcon() }}</mat-icon>
              <div class="imp-message-body">
                <strong>{{ message() }}</strong>
                @if (messageHint()) {
                  <span>{{ messageHint() }}</span>
                }
              </div>
            </div>
          }

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
                  Validando y procesando...
                </ng-container>
              } @else {
                <ng-container>
                  <mat-icon>upload</mat-icon>
                  Cargar y validar
                </ng-container>
              }
            </button>

            <button mat-stroked-button type="button" [disabled]="downloadingTemplate()" (click)="downloadTemplate()">
              <mat-icon>download</mat-icon>
              @if (downloadingTemplate()) {
                Descargando plantilla...
              } @else {
                Descargar plantilla oficial
              }
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- ── Flujo de la importación ── -->
      <mat-card class="imp-card">
        <mat-card-header>
          <mat-card-title>Flujo de la importación</mat-card-title>
          <mat-card-subtitle>Qué ocurre con tu archivo, paso a paso</mat-card-subtitle>
        </mat-card-header>
        <mat-divider />
        <mat-card-content>
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
        </mat-card-content>
      </mat-card>

      <!-- ── Resultado de la última carga ── -->
      @if (lastResult(); as result) {
        <mat-card class="imp-card imp-result-card" [style.--tone-color]="toneColor(result.statusTone)">
          <mat-card-header>
            <mat-card-title>Resultado de la carga</mat-card-title>
          </mat-card-header>
          <mat-divider />
          <mat-card-content>
            <div class="result-head">
              <span class="status-badge" [style.--badge-color]="toneColor(result.statusTone)">
                {{ result.statusLabel }}
              </span>
              <div class="result-counts">
                <span class="result-count result-count--ok" matTooltip="Filas válidas">
                  <mat-icon>check_circle</mat-icon>{{ result.validRowCount }} válidas
                </span>
                <span class="result-count result-count--err" matTooltip="Filas con errores que no se importaron">
                  <mat-icon>cancel</mat-icon>{{ result.invalidRowCount }} con errores
                </span>
                <span class="result-count result-count--warn" matTooltip="Valores por revisar">
                  <mat-icon>warning</mat-icon>{{ result.warningCount }} observaciones
                </span>
                <span class="result-count" matTooltip="Registros guardados en la ficha">
                  <mat-icon>save</mat-icon>{{ result.persistedRecordCount }} registros guardados
                </span>
              </div>
            </div>
            <p class="result-description">{{ result.statusDescription }}</p>
            <p class="result-next">
              <mat-icon>arrow_forward</mat-icon>
              <span><strong>Siguiente paso:</strong> {{ result.statusNextStep }}</span>
            </p>
          </mat-card-content>
        </mat-card>
      }

      <!-- ── Historial de lotes ── -->
      <mat-card class="imp-card">
        <mat-card-header>
          <mat-card-title>Historial de lotes</mat-card-title>
          <mat-card-subtitle>Selecciona un lote para ver su detalle</mat-card-subtitle>
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
                [attr.aria-label]="'Ver detalle del lote ' + batch.fileName"
                (keydown.enter)="loadIssues(batch)"
              >
                <div class="batch-main">
                  <mat-icon class="batch-icon" [style.color]="toneColor(batch.statusTone)">
                    {{ batchIcon(batch) }}
                  </mat-icon>
                  <div class="batch-info">
                    <strong class="batch-name">{{ batch.fileName }}</strong>
                    <span class="batch-date">{{ batch.startedAtUtc | date: 'medium' }}</span>
                    <span class="batch-desc">{{ batch.statusDescription }}</span>
                  </div>
                </div>

                <div class="batch-side">
                  <span class="status-badge" [style.--badge-color]="toneColor(batch.statusTone)">
                    {{ batch.statusLabel }}
                  </span>
                  <div class="batch-stats">
                    <span class="batch-stat batch-stat--ok" matTooltip="Filas válidas">
                      <mat-icon>check</mat-icon>{{ batch.validRowCount }}
                    </span>
                    <span class="batch-stat batch-stat--err" matTooltip="Filas con errores">
                      <mat-icon>close</mat-icon>{{ batch.invalidRowCount }}
                    </span>
                    <span class="batch-stat batch-stat--warn" matTooltip="Observaciones">
                      <mat-icon>warning</mat-icon>{{ batch.warningCount }}
                    </span>
                  </div>
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
              </div>
              @if (!$last) {
                <mat-divider />
              }
            }
          </div>
        } @else {
          <mat-card-content class="imp-empty">
            <mat-icon>history</mat-icon>
            <p>Aún no hay cargas registradas. Carga el archivo oficial para empezar.</p>
          </mat-card-content>
        }
      </mat-card>

      <!-- ── Incidencias del lote seleccionado ── -->
      @if (selectedBatch(); as batch) {
        <mat-card class="imp-card">
          <mat-card-header>
            <mat-card-title>Qué debes corregir</mat-card-title>
            <mat-card-subtitle>
              {{ batch.fileName }} &middot; {{ batch.statusLabel }}
            </mat-card-subtitle>
          </mat-card-header>
          <mat-divider />

          @if (issues().length > 0) {
            <mat-card-content>
              <div class="issue-toolbar">
                <p class="issue-summary">
                  <strong>{{ errorCount() }}</strong> deben corregirse &middot;
                  <strong>{{ warningCount() }}</strong> por revisar
                </p>
                <div class="issue-filters">
                  <button
                    mat-stroked-button
                    type="button"
                    [class.issue-filter--active]="severityFilter() === 'all'"
                    (click)="severityFilter.set('all')"
                  >
                    Todas ({{ issues().length }})
                  </button>
                  <button
                    mat-stroked-button
                    type="button"
                    [class.issue-filter--active]="severityFilter() === 'Error'"
                    (click)="severityFilter.set('Error')"
                  >
                    Deben corregirse ({{ errorCount() }})
                  </button>
                  <button
                    mat-stroked-button
                    type="button"
                    [class.issue-filter--active]="severityFilter() === 'Warning'"
                    (click)="severityFilter.set('Warning')"
                  >
                    Por revisar ({{ warningCount() }})
                  </button>
                </div>
              </div>

              @for (group of issueGroups(); track group.sheetName) {
                <div class="issue-group">
                  <p class="issue-group-title">
                    <mat-icon>tab</mat-icon>
                    Hoja "{{ group.sheetName }}"
                    <span class="issue-group-count">{{ group.issues.length }}</span>
                  </p>

                  @for (issue of group.issues; track issue.id) {
                    <article
                      class="issue-card"
                      [class.issue-card--error]="issue.severity === 'Error'"
                      [class.issue-card--warning]="issue.severity === 'Warning'"
                    >
                      <header class="issue-card-head">
                        <span class="sev-badge">{{ issue.severityLabel }}</span>
                        <strong class="issue-title">{{ issue.title }}</strong>
                        @if (issue.cellReference) {
                          <span class="issue-cell" matTooltip="Ubicación en el archivo de Excel">
                            Celda {{ issue.cellReference }}
                          </span>
                        }
                      </header>

                      <p class="issue-message">{{ issue.message }}</p>

                      <dl class="issue-facts">
                        @if (issue.rowNumber) {
                          <div class="issue-fact">
                            <dt>Fila</dt>
                            <dd>{{ issue.rowNumber }}</dd>
                          </div>
                        }
                        @if (issue.columnLetter) {
                          <div class="issue-fact">
                            <dt>Columna</dt>
                            <dd>{{ issue.columnLetter }}</dd>
                          </div>
                        }
                        @if (issue.fieldLabel) {
                          <div class="issue-fact">
                            <dt>Campo</dt>
                            <dd>{{ issue.fieldLabel }}</dd>
                          </div>
                        }
                        @if (issue.rawValue) {
                          <div class="issue-fact">
                            <dt>Valor recibido</dt>
                            <dd class="issue-fact--received">{{ issue.rawValue }}</dd>
                          </div>
                        }
                        @if (issue.expectedValue) {
                          <div class="issue-fact issue-fact--wide">
                            <dt>Valor esperado</dt>
                            <dd>{{ issue.expectedValue }}</dd>
                          </div>
                        }
                      </dl>

                      @if (issue.howToFix) {
                        <p class="issue-fix">
                          <mat-icon>build</mat-icon>
                          <span><strong>Cómo corregirlo:</strong> {{ issue.howToFix }}</span>
                        </p>
                      }

                      @if (canSeeTechnicalDetail() && issue.technicalDetail) {
                        <details class="issue-technical">
                          <summary>Detalle técnico (soporte)</summary>
                          <code>{{ issue.errorCode }} · {{ issue.technicalDetail }}</code>
                        </details>
                      }
                    </article>
                  }
                </div>
              }
            </mat-card-content>
          } @else {
            <mat-card-content class="imp-empty">
              <mat-icon>check_circle_outline</mat-icon>
              <p>Esta carga no tiene incidencias: todos los datos cumplieron las validaciones.</p>
            </mat-card-content>
          }
        </mat-card>
      }

      <!-- ── Qué ocurre con los datos ── -->
      <mat-card class="imp-card">
        <mat-card-header>
          <mat-card-title>Qué ocurre con los datos importados</mat-card-title>
          <mat-card-subtitle>Dónde quedan, cuándo se ven y cuándo se pueden editar</mat-card-subtitle>
        </mat-card-header>
        <mat-divider />
        <mat-card-content>
          <mat-accordion class="info-accordion" multi>
            <mat-expansion-panel>
              <mat-expansion-panel-header>
                <mat-panel-title>¿Dónde quedan almacenados los datos?</mat-panel-title>
              </mat-expansion-panel-header>
              <p>
                Cada carga guarda primero una copia fiel del archivo (filas de trabajo del lote) y
                luego materializa la información en la ficha departamental: identificación,
                diagnóstico del ecosistema, oportunidades de cambio, ejes PNMC y actores. Las hojas
                de Indicadores y Detalle Indicadores alimentan el módulo de Indicadores.
              </p>
            </mat-expansion-panel>

            <mat-expansion-panel>
              <mat-expansion-panel-header>
                <mat-panel-title>¿Qué estado tienen y cuándo se vuelven visibles?</mat-panel-title>
              </mat-expansion-panel-header>
              <p>
                Al terminar una carga con estado <strong>Importación exitosa</strong> o
                <strong>Importación completada con observaciones</strong>, la ficha queda visible de
                inmediato en el módulo de Gobernanza con el estado de revisión
                <strong>Pendiente</strong>, para que el Líder de Gobernanza la apruebe o la devuelva.
                Si la carga se <strong>rechaza</strong>, no se crea ni se modifica ninguna ficha.
              </p>
            </mat-expansion-panel>

            <mat-expansion-panel>
              <mat-expansion-panel-header>
                <mat-panel-title>¿Cuándo se pueden editar?</mat-panel-title>
              </mat-expansion-panel-header>
              <p>
                Desde que quedan visibles. El Gestor Departamental puede ajustar cualquier sección
                en el módulo de Gobernanza y volver a guardar; el Líder de Gobernanza revisa y
                aprueba o devuelve la ficha, y el gestor recibe una notificación con el cambio de
                estado. Volver a cargar el mismo archivo actualiza la ficha del mismo departamento
                y fecha de levantamiento en lugar de duplicarla.
              </p>
            </mat-expansion-panel>

            <mat-expansion-panel>
              <mat-expansion-panel-header>
                <mat-panel-title>¿Qué pasa si hay errores parciales?</mat-panel-title>
              </mat-expansion-panel-header>
              <p>
                Si alguna fila tiene errores, esa información no se guarda y el lote queda como
                <strong>Importación completada con observaciones</strong>: las secciones correctas se
                conservan y las filas con errores se listan arriba con la corrección exacta. Al
                corregir el archivo y cargarlo de nuevo, la ficha se completa sin duplicar datos.
              </p>
            </mat-expansion-panel>
          </mat-accordion>
        </mat-card-content>
      </mat-card>
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

      .imp-card {
        border-radius: var(--radius-xl);
        border: 1px solid var(--color-border-light);
        box-shadow: var(--shadow-sm);
      }

      /* ── Zona de carga ── */
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

      .drop-zone--invalid {
        border-color: var(--color-error-border);
        background: var(--color-error-bg);
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
        align-items: flex-start;
        gap: var(--space-3);
        padding: var(--space-4);
        border-radius: var(--radius-lg);
        font-size: var(--font-size-body-sm);
        margin-top: var(--space-4);
      }

      .imp-message-body {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }

      .imp-message--error {
        background: var(--color-error-bg);
        color: var(--color-error-text);
        border: 1px solid var(--color-error-border);
      }

      .imp-message--warning {
        background: var(--color-warning-bg);
        color: var(--color-warning-text);
        border: 1px solid var(--color-warning-border);
      }

      .imp-message--success {
        background: var(--color-success-bg);
        color: var(--color-success-text);
        border: 1px solid var(--color-success-border);
      }

      .upload-actions {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        margin-top: var(--space-5);
        flex-wrap: wrap;
      }

      .spin {
        animation: spin 1s linear infinite;
      }

      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }

      /* ── Flujo ── */
      .flow-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
      }

      .flow-item {
        display: flex;
        gap: var(--space-4);
        padding-bottom: var(--space-5);
        position: relative;
      }

      .flow-item:not(:last-child)::before {
        content: '';
        position: absolute;
        left: 17px;
        top: 36px;
        bottom: 0;
        width: 2px;
        background: var(--color-border-light);
      }

      .flow-marker {
        flex-shrink: 0;
        width: 36px;
        height: 36px;
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
        font-size: 20px;
        width: 20px;
        height: 20px;
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
        padding-top: var(--space-1);
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
        opacity: 0.65;
      }

      /* ── Resultado ── */
      .imp-result-card {
        border-left: 4px solid var(--tone-color);
      }

      .result-head {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        flex-wrap: wrap;
        margin-bottom: var(--space-3);
      }

      .result-counts {
        display: flex;
        gap: var(--space-4);
        flex-wrap: wrap;
      }

      .result-count {
        display: flex;
        align-items: center;
        gap: 4px;
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-semibold);
        color: var(--color-on-surface-secondary);
      }

      .result-count .mat-icon {
        font-size: 1rem;
        width: 1rem;
        height: 1rem;
      }

      .result-count--ok {
        color: var(--color-success);
      }
      .result-count--err {
        color: var(--color-error);
      }
      .result-count--warn {
        color: var(--color-warning);
      }

      .result-description {
        margin: 0 0 var(--space-3);
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        line-height: 1.6;
      }

      .result-next {
        display: flex;
        align-items: flex-start;
        gap: var(--space-2);
        margin: 0;
        padding: var(--space-3) var(--space-4);
        background: var(--color-surface-container-low);
        border-radius: var(--radius-lg);
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
      }

      .result-next .mat-icon {
        font-size: 1.125rem;
        width: 1.125rem;
        height: 1.125rem;
        flex-shrink: 0;
        color: var(--color-primary-600);
      }

      .status-badge {
        display: inline-block;
        padding: 3px var(--space-3);
        border-radius: var(--radius-full);
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-bold);
        color: var(--badge-color);
        background: color-mix(in srgb, var(--badge-color) 12%, transparent);
        border: 1px solid color-mix(in srgb, var(--badge-color) 28%, transparent);
        white-space: nowrap;
      }

      /* ── Historial ── */
      .batch-list {
        display: flex;
        flex-direction: column;
      }

      .batch-row {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-4) var(--space-5);
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
        align-items: flex-start;
        gap: var(--space-3);
        min-width: 0;
        flex: 1;
      }

      .batch-icon {
        flex-shrink: 0;
      }

      .batch-info {
        display: flex;
        flex-direction: column;
        min-width: 0;
        gap: 2px;
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

      .batch-desc {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-variant);
        line-height: 1.5;
      }

      .batch-side {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        gap: var(--space-2);
        flex-shrink: 0;
      }

      .batch-stats {
        display: flex;
        gap: var(--space-3);
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

      .batch-stat--ok {
        color: var(--color-success);
      }
      .batch-stat--err {
        color: var(--color-error);
      }
      .batch-stat--warn {
        color: var(--color-warning);
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

      /* ── Incidencias ── */
      .issue-toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-4);
        flex-wrap: wrap;
        margin-bottom: var(--space-5);
      }

      .issue-summary {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
      }

      .issue-filters {
        display: flex;
        gap: var(--space-2);
        flex-wrap: wrap;
      }

      .issue-filters button {
        font-size: var(--font-size-caption) !important;
      }

      .issue-filter--active {
        background: var(--color-primary-50) !important;
        border-color: var(--color-primary-300) !important;
        color: var(--color-primary-900) !important;
      }

      .issue-group {
        margin-bottom: var(--space-6);
      }

      .issue-group-title {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        margin: 0 0 var(--space-3);
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-bold);
        text-transform: uppercase;
        letter-spacing: var(--letter-spacing-label);
        color: var(--color-primary-700);
      }

      .issue-group-title .mat-icon {
        font-size: 1rem;
        width: 1rem;
        height: 1rem;
      }

      .issue-group-count {
        padding: 1px var(--space-2);
        border-radius: var(--radius-full);
        background: var(--color-primary-50);
        color: var(--color-primary-700);
      }

      .issue-card {
        border: 1px solid var(--color-border-light);
        border-left: 4px solid var(--color-border);
        border-radius: var(--radius-lg);
        padding: var(--space-4);
        margin-bottom: var(--space-3);
        background: var(--color-surface-container-lowest);
      }

      .issue-card--error {
        border-left-color: var(--color-error);
      }

      .issue-card--warning {
        border-left-color: var(--color-warning);
      }

      .issue-card-head {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        flex-wrap: wrap;
        margin-bottom: var(--space-2);
      }

      .sev-badge {
        padding: 2px var(--space-2);
        border-radius: var(--radius-md);
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-bold);
        text-transform: uppercase;
        background: var(--color-surface-container-low);
        color: var(--color-on-surface-secondary);
      }

      .issue-card--error .sev-badge {
        background: var(--color-error-bg);
        color: var(--color-error-text);
      }

      .issue-card--warning .sev-badge {
        background: var(--color-warning-bg);
        color: var(--color-warning-text);
      }

      .issue-title {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
      }

      .issue-cell {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-variant);
        font-family: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
      }

      .issue-message {
        margin: 0 0 var(--space-3);
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        line-height: 1.6;
      }

      .issue-facts {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: var(--space-3);
        margin: 0 0 var(--space-3);
      }

      .issue-fact--wide {
        grid-column: 1 / -1;
      }

      .issue-fact dt {
        font-size: var(--font-size-label);
        text-transform: uppercase;
        letter-spacing: var(--letter-spacing-label);
        color: var(--color-on-surface-variant);
        margin-bottom: 2px;
      }

      .issue-fact dd {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        overflow-wrap: anywhere;
      }

      .issue-fact--received {
        font-family: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
        font-weight: var(--font-weight-semibold);
      }

      .issue-fix {
        display: flex;
        align-items: flex-start;
        gap: var(--space-2);
        margin: 0;
        padding: var(--space-3);
        background: var(--color-surface-container-low);
        border-radius: var(--radius-md);
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        line-height: 1.6;
      }

      .issue-fix .mat-icon {
        font-size: 1.125rem;
        width: 1.125rem;
        height: 1.125rem;
        flex-shrink: 0;
        color: var(--color-primary-600);
      }

      .issue-technical {
        margin-top: var(--space-3);
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-variant);
      }

      .issue-technical summary {
        cursor: pointer;
        font-weight: var(--font-weight-semibold);
      }

      .issue-technical code {
        display: block;
        margin-top: var(--space-2);
        padding: var(--space-2);
        background: var(--color-surface-container-low);
        border-radius: var(--radius-sm);
        overflow-x: auto;
      }

      /* ── Panel informativo ── */
      .info-accordion p {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        line-height: 1.7;
      }

      /* ── Vacíos ── */
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
        max-width: 420px;
        font-size: var(--font-size-body-sm);
        line-height: 1.6;
      }

      @media (max-width: 768px) {
        .batch-row {
          flex-wrap: wrap;
        }

        .batch-side {
          align-items: flex-start;
          width: 100%;
          flex-direction: row;
          justify-content: space-between;
        }

        .issue-toolbar {
          flex-direction: column;
          align-items: flex-start;
        }
      }

      @media (max-width: 480px) {
        .drop-zone {
          padding: var(--space-6) var(--space-4);
        }

        .upload-actions button {
          width: 100%;
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

  readonly officialFileName = OFFICIAL_IMPORT_FILE_NAME;
  readonly maxFileSizeMb = MAX_IMPORT_FILE_SIZE_BYTES / 1024 / 1024;
  readonly stages: ImportStage[] = IMPORT_FLOW_STAGES;

  readonly loading = signal(false);
  readonly message = signal('');
  readonly messageHint = signal('');
  readonly messageType = signal<'error' | 'success' | 'warning'>('success');
  readonly selectedFile = signal<File | null>(null);
  readonly isDragging = signal(false);
  readonly downloadingTemplate = signal(false);

  readonly batches = signal<ImportBatchSummary[]>([]);
  readonly issues = signal<ImportValidationIssue[]>([]);
  readonly selectedBatch = signal<ImportBatchSummary | null>(null);
  readonly lastResult = signal<ImportWorkbookResult | null>(null);
  readonly severityFilter = signal<'all' | 'Error' | 'Warning'>('all');

  /** Etapa en la que se detuvo el flujo (rechazo por formato, estructura, datos o proceso). */
  private readonly failedStage = signal<ImportStageKey | null>(null);

  /** Solo el Administrador puede eliminar lotes de importación. */
  readonly canDelete = computed(() => this.authTokenService.hasAnyRole([AppRoles.Administrador]));

  /** El detalle técnico es para soporte: se muestra únicamente al Administrador. */
  readonly canSeeTechnicalDetail = computed(() =>
    this.authTokenService.hasAnyRole([AppRoles.Administrador])
  );

  readonly errorCount = computed(() => this.issues().filter((issue) => issue.severity === 'Error').length);
  readonly warningCount = computed(() => this.issues().filter((issue) => issue.severity === 'Warning').length);

  readonly issueGroups = computed<IssueGroup[]>(() => {
    const filter = this.severityFilter();
    const filtered = filter === 'all' ? this.issues() : this.issues().filter((issue) => issue.severity === filter);

    const groups = new Map<string, IssueGroup>();
    for (const issue of filtered) {
      const group = groups.get(issue.sheetName) ?? {
        sheetName: issue.sheetName,
        errorCount: 0,
        warningCount: 0,
        issues: []
      };

      group.issues.push(issue);
      if (issue.severity === 'Error') group.errorCount++;
      if (issue.severity === 'Warning') group.warningCount++;
      groups.set(issue.sheetName, group);
    }

    return [...groups.values()];
  });

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
    this.applyFile(event.dataTransfer?.files?.item(0) ?? null);
  }

  selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.applyFile(input.files?.item(0) ?? null);
    input.value = '';
  }

  clearFile(): void {
    this.selectedFile.set(null);
    this.clearMessage();
    this.failedStage.set(null);
  }

  upload(): void {
    const file = this.selectedFile();
    if (!file) return;

    // Última verificación local antes de enviar: el servidor la repite como validación definitiva.
    const check = checkImportFile(file);
    if (!check.valid) {
      this.rejectLocally(check.message!, check.hint!);
      return;
    }

    this.loading.set(true);
    this.clearMessage();
    this.failedStage.set(null);
    this.lastResult.set(null);

    this.importsApiService
      .importExcel(file)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          this.lastResult.set(result);
          this.issues.set(result.issues);
          this.severityFilter.set('all');
          this.selectedBatch.set(null);

          if (!result.accepted) {
            this.failedStage.set(resolveFailedStage(result.issues.map((issue) => issue.errorCode)));
            this.setMessage('error', result.statusLabel, result.statusNextStep);
          } else if (result.invalidRowCount > 0 || result.warningCount > 0) {
            this.setMessage('warning', result.statusLabel, result.statusNextStep);
          } else {
            this.setMessage('success', result.statusLabel, result.statusNextStep);
            this.selectedFile.set(null);
          }

          this.refreshBatches();
        },
        error: (err: HttpErrorResponse) => {
          this.failedStage.set('format');
          this.setMessage(
            'error',
            extractErrorMessage(err, 'No fue posible cargar el archivo.'),
            `Verifique que esté usando el archivo oficial ${OFFICIAL_IMPORT_FILE_NAME} e intente de nuevo.`
          );
        }
      });
  }

  downloadTemplate(): void {
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
          link.download = OFFICIAL_IMPORT_FILE_NAME;
          link.click();
          URL.revokeObjectURL(url);
        },
        error: () =>
          this.snackBar.open('No fue posible descargar la plantilla oficial', 'Cerrar', { duration: 5000 })
      });
  }

  loadIssues(batch: ImportBatchSummary): void {
    this.selectedBatch.set(batch);
    this.severityFilter.set('all');
    this.importsApiService.getIssues(batch.importBatchId).subscribe({
      next: (issues) => this.issues.set(issues),
      error: () =>
        this.snackBar.open('No fue posible consultar el detalle de la carga', 'Cerrar', { duration: 5000 })
    });
  }

  deleteBatch(batch: ImportBatchSummary, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    this.confirmDialog
      .confirm({
        title: 'Eliminar registro de la carga',
        message: `¿Deseas eliminar el registro de la carga "${batch.fileName}"? Se borrará su historial de incidencias, pero los datos ya guardados en fichas e indicadores no se revierten.`,
        confirmLabel: 'Eliminar',
        icon: 'delete_forever',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (!confirmed) return;

        this.importsApiService.deleteBatch(batch.importBatchId).subscribe({
          next: () => {
            if (this.selectedBatch()?.importBatchId === batch.importBatchId) {
              this.selectedBatch.set(null);
              this.issues.set([]);
            }
            this.snackBar.open('Registro de la carga eliminado', 'Cerrar', { duration: 3000 });
            this.refreshBatches();
          },
          error: (err: HttpErrorResponse) =>
            this.snackBar.open(extractErrorMessage(err, 'No fue posible eliminar el registro'), 'Cerrar', {
              duration: 5000
            })
        });
      });
  }

  /** Estado visual de cada etapa del flujo según lo que va ocurriendo con la carga. */
  stageState(key: ImportStageKey): ImportStageState {
    const failed = this.failedStage();
    const result = this.lastResult();
    const order = this.stages.map((stage) => stage.key);
    const index = order.indexOf(key);

    if (failed) {
      const failedIndex = order.indexOf(failed);
      if (index < failedIndex) return 'done';
      return index === failedIndex ? 'failed' : 'pending';
    }

    if (this.loading()) {
      // Durante la carga el servidor recorre formato → estructura → datos → lote → proceso.
      return index === 0 ? 'done' : index <= order.indexOf('processing') ? 'active' : 'pending';
    }

    if (result?.accepted) {
      const withObservations = result.invalidRowCount > 0 || result.warningCount > 0;
      if (key === 'available') {
        return result.persistedRecordCount > 0 ? (withObservations ? 'warning' : 'done') : 'pending';
      }
      if (key === 'completed') {
        return withObservations ? 'warning' : 'done';
      }
      return 'done';
    }

    if (this.selectedFile()) {
      return index === 0 ? 'done' : index === 1 ? 'active' : 'pending';
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

  batchIcon(batch: ImportBatchSummary): string {
    switch (batch.statusTone) {
      case 'success':
        return 'check_circle';
      case 'warning':
        return 'warning';
      case 'error':
        return 'cancel';
      case 'progress':
        return 'sync';
      default:
        return 'table_chart';
    }
  }

  messageIcon(): string {
    switch (this.messageType()) {
      case 'error':
        return 'error_outline';
      case 'warning':
        return 'warning_amber';
      default:
        return 'check_circle';
    }
  }

  toneColor(tone: ImportTone | string): string {
    return toneColor(tone);
  }

  /** Rechazo detectado en el navegador: no se envía nada al servidor. */
  private rejectLocally(message: string, hint: string): void {
    this.failedStage.set('format');
    this.lastResult.set(null);
    this.setMessage('error', message, hint);
  }

  private applyFile(file: File | null): void {
    if (!file) {
      return;
    }

    const check = checkImportFile(file);
    if (!check.valid) {
      this.selectedFile.set(null);
      this.rejectLocally(check.message!, check.hint!);
      return;
    }

    this.selectedFile.set(file);
    this.clearMessage();
    this.failedStage.set(null);
    this.lastResult.set(null);
  }

  private setMessage(type: 'error' | 'success' | 'warning', message: string, hint: string): void {
    this.messageType.set(type);
    this.message.set(message);
    this.messageHint.set(hint);
  }

  private clearMessage(): void {
    this.message.set('');
    this.messageHint.set('');
  }

  private refreshBatches(): void {
    this.importsApiService.getBatches().subscribe({
      next: (batches) => this.batches.set(batches),
      error: () => this.snackBar.open('No fue posible cargar el historial', 'Cerrar', { duration: 5000 })
    });
  }
}
