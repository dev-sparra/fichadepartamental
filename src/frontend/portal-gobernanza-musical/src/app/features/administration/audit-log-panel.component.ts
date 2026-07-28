import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';

import { AuditApiService } from '../../core/services/audit-api.service';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';
import { AuditFilterOptions, AuditLog } from '../../shared/models/audit.models';

/** Icono con el que se reconoce cada módulo de un vistazo. */
const MODULE_ICONS: Record<string, string> = {
  Autenticación: 'login',
  Seguridad: 'manage_accounts',
  Catálogos: 'list_alt',
  Gobernanza: 'account_balance',
  Importaciones: 'upload_file',
  Aprobaciones: 'fact_check',
  Reportes: 'download'
};

@Component({
  selector: 'app-audit-log-panel',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTooltipModule
  ],
  template: `
    <mat-card class="audit-card">
      @if (loading()) {
        <mat-progress-bar mode="indeterminate" class="card-progress" />
      }

      <!-- ── Filtros ── -->
      <div class="audit-filters">
        <mat-form-field appearance="outline" class="filter filter--search">
          <mat-label>Buscar</mat-label>
          <input
            matInput
            type="text"
            placeholder="Usuario, acción u objeto afectado"
            [ngModel]="search()"
            (ngModelChange)="search.set($event)"
            (keydown.enter)="applyFilters()"
          />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter">
          <mat-label>Módulo</mat-label>
          <mat-select [ngModel]="module()" (ngModelChange)="module.set($event); applyFilters()">
            <mat-option [value]="null">Todos</mat-option>
            @for (option of filterOptions().modules; track option) {
              <mat-option [value]="option">{{ option }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter">
          <mat-label>Usuario</mat-label>
          <mat-select [ngModel]="userEmail()" (ngModelChange)="userEmail.set($event); applyFilters()">
            <mat-option [value]="null">Todos</mat-option>
            @for (option of filterOptions().users; track option.email) {
              <mat-option [value]="option.email">{{ option.displayName }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter">
          <mat-label>Acción</mat-label>
          <mat-select [ngModel]="operation()" (ngModelChange)="operation.set($event); applyFilters()">
            <mat-option [value]="null">Todas</mat-option>
            @for (option of filterOptions().operations; track option) {
              <mat-option [value]="option">{{ option }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter filter--date">
          <mat-label>Desde</mat-label>
          <input matInput [matDatepicker]="fromPicker" [ngModel]="from()" (ngModelChange)="from.set($event); applyFilters()" />
          <mat-datepicker-toggle matIconSuffix [for]="fromPicker" />
          <mat-datepicker #fromPicker />
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter filter--date">
          <mat-label>Hasta</mat-label>
          <input matInput [matDatepicker]="toPicker" [ngModel]="to()" (ngModelChange)="to.set($event); applyFilters()" />
          <mat-datepicker-toggle matIconSuffix [for]="toPicker" />
          <mat-datepicker #toPicker />
        </mat-form-field>

        <div class="filter-actions">
          <button mat-flat-button type="button" class="filter-apply" (click)="applyFilters()">
            <mat-icon>filter_alt</mat-icon>
            Filtrar
          </button>
          @if (hasFilters()) {
            <button mat-stroked-button type="button" (click)="clearFilters()">
              <mat-icon>filter_alt_off</mat-icon>
              Limpiar
            </button>
          }
        </div>
      </div>

      @if (total() > 0) {
        <p class="audit-count">
          {{ total() }} {{ total() === 1 ? 'acción registrada' : 'acciones registradas' }}
          @if (hasFilters()) {
            con los filtros aplicados
          }
        </p>
      }

      <!-- ── Historial ── -->
      <div class="table-responsive">
        <table class="data-table audit-table">
          <thead>
            <tr>
              <th class="col-expand"></th>
              <th>Fecha y hora</th>
              <th>Usuario</th>
              <th>Módulo</th>
              <th>Acción</th>
              <th>Objeto afectado</th>
            </tr>
          </thead>
          <tbody>
            @for (log of logs(); track log.id) {
              <tr
                class="audit-row"
                [class.audit-row--failed]="log.result === 'Fallido'"
                [class.audit-row--open]="expandedId() === log.id"
                (click)="toggle(log.id)"
                role="button"
                tabindex="0"
                [attr.aria-expanded]="expandedId() === log.id"
                [attr.aria-label]="'Ver detalle de: ' + log.operation"
                (keydown.enter)="toggle(log.id)"
              >
                <td class="col-expand">
                  <mat-icon class="expand-icon">
                    {{ expandedId() === log.id ? 'expand_less' : 'expand_more' }}
                  </mat-icon>
                </td>
                <td class="audit-date">{{ log.timestampUtc | date: 'dd/MM/yyyy HH:mm:ss' }}</td>
                <td>
                  <div class="audit-user">
                    <strong>{{ log.userDisplayName }}</strong>
                    <span>{{ log.userEmail }}</span>
                  </div>
                </td>
                <td>
                  <span class="module-badge">
                    <mat-icon>{{ moduleIcon(log.module) }}</mat-icon>
                    {{ log.module }}
                  </span>
                </td>
                <td>
                  <span class="op-badge" [class.op-badge--failed]="log.result === 'Fallido'">
                    {{ log.operation }}
                  </span>
                </td>
                <td class="audit-target">{{ log.entityLabel ?? log.entityName }}</td>
              </tr>

              @if (expandedId() === log.id) {
                <tr class="audit-detail-row">
                  <td colspan="6">
                    <div class="audit-detail">
                      @if (log.description) {
                        <p class="detail-description">{{ log.description }}</p>
                      }

                      @if (log.changes.length > 0) {
                        <div class="detail-block">
                          <p class="detail-title">
                            <mat-icon>compare_arrows</mat-icon>
                            Qué cambió
                          </p>
                          <table class="changes-table">
                            <thead>
                              <tr>
                                <th>Campo</th>
                                <th>Antes</th>
                                <th>Después</th>
                              </tr>
                            </thead>
                            <tbody>
                              @for (change of log.changes; track change.field) {
                                <tr>
                                  <td class="change-label">{{ change.label }}</td>
                                  <td class="change-before">{{ change.before }}</td>
                                  <td class="change-after">{{ change.after }}</td>
                                </tr>
                              }
                            </tbody>
                          </table>
                        </div>
                      }

                      <div class="detail-block">
                        <p class="detail-title">
                          <mat-icon>info_outline</mat-icon>
                          Datos del registro
                        </p>
                        <dl class="detail-facts">
                          <div class="detail-fact">
                            <dt>Resultado</dt>
                            <dd [class.fact-failed]="log.result === 'Fallido'">{{ log.result }}</dd>
                          </div>
                          @if (log.userRoles) {
                            <div class="detail-fact">
                              <dt>Roles del usuario</dt>
                              <dd>{{ log.userRoles }}</dd>
                            </div>
                          }
                          @if (log.ipAddress) {
                            <div class="detail-fact">
                              <dt>Dirección IP</dt>
                              <dd class="fact-mono">{{ log.ipAddress }}</dd>
                            </div>
                          }
                          @if (log.requestPath) {
                            <div class="detail-fact">
                              <dt>Petición</dt>
                              <dd class="fact-mono">{{ log.requestMethod }} {{ log.requestPath }}</dd>
                            </div>
                          }
                          <div class="detail-fact">
                            <dt>Tipo de objeto</dt>
                            <dd class="fact-mono">{{ log.entityName }}</dd>
                          </div>
                          @if (log.entityId ?? log.entityKey) {
                            <div class="detail-fact">
                              <dt>Identificador</dt>
                              <dd class="fact-mono">{{ log.entityId ?? log.entityKey }}</dd>
                            </div>
                          }
                        </dl>
                      </div>

                      @if (log.oldValuesJson || log.newValuesJson) {
                        <details class="detail-raw">
                          <summary>Registro completo antes y después (soporte)</summary>
                          @if (log.oldValuesJson) {
                            <p class="raw-title">Antes</p>
                            <pre>{{ pretty(log.oldValuesJson) }}</pre>
                          }
                          @if (log.newValuesJson) {
                            <p class="raw-title">Después</p>
                            <pre>{{ pretty(log.newValuesJson) }}</pre>
                          }
                        </details>
                      }
                    </div>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
      </div>

      @if (logs().length === 0 && !loading()) {
        <div class="empty-state">
          <div class="empty-icon-box">
            <mat-icon>history_edu</mat-icon>
          </div>
          <h3 class="empty-title">
            {{ hasFilters() ? 'Sin resultados' : 'Sin registros de auditoría' }}
          </h3>
          <p class="empty-desc">
            {{
              hasFilters()
                ? 'Ninguna acción coincide con los filtros. Prueba con otro rango de fechas o quita algún filtro.'
                : 'Cada acción que se haga en el portal quedará registrada aquí.'
            }}
          </p>
        </div>
      }

      @if (totalPages() > 1) {
        <div class="audit-pager">
          <button mat-stroked-button type="button" [disabled]="page() === 1 || loading()" (click)="goToPage(page() - 1)">
            <mat-icon>chevron_left</mat-icon>
            Anterior
          </button>
          <span class="pager-status">Página {{ page() }} de {{ totalPages() }}</span>
          <button
            mat-stroked-button
            type="button"
            [disabled]="page() >= totalPages() || loading()"
            (click)="goToPage(page() + 1)"
          >
            Siguiente
            <mat-icon>chevron_right</mat-icon>
          </button>
        </div>
      }
    </mat-card>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .audit-card {
        border-radius: var(--radius-xl);
        border: 1px solid var(--color-border-light);
        overflow: hidden;
      }

      .card-progress {
        border-radius: 0;
      }

      /* ── Filtros ── */
      .audit-filters {
        display: flex;
        flex-wrap: wrap;
        align-items: flex-start;
        gap: var(--space-3);
        padding: var(--space-5) var(--space-5) 0;
      }

      .filter {
        flex: 1 1 180px;
        min-width: 160px;
      }

      .filter--search {
        flex: 2 1 260px;
      }

      .filter--date {
        flex: 0 1 170px;
      }

      .filter-actions {
        display: flex;
        gap: var(--space-2);
        padding-top: 6px;
      }

      ::ng-deep .filter-apply {
        background-color: var(--color-primary-900) !important;
        color: #ffffff !important;
      }

      .audit-count {
        margin: 0 var(--space-5) var(--space-3);
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
      }

      /* ── Tabla ── */
      .table-responsive {
        overflow-x: auto;
      }

      .audit-table {
        width: 100%;
      }

      .col-expand {
        width: 36px;
      }

      .expand-icon {
        color: var(--color-on-surface-variant);
      }

      .audit-row {
        cursor: pointer;
        transition: background var(--transition-fast);
      }

      .audit-row:hover {
        background: var(--color-hover);
      }

      .audit-row--open {
        background: var(--color-primary-50);
      }

      .audit-row--failed .audit-date {
        color: var(--color-error-text);
      }

      .audit-date {
        white-space: nowrap;
        font-variant-numeric: tabular-nums;
      }

      .audit-user {
        display: flex;
        flex-direction: column;
        line-height: 1.35;
      }

      .audit-user strong {
        color: var(--color-on-surface);
      }

      .audit-user span {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-variant);
      }

      .module-badge {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 2px var(--space-2);
        border-radius: var(--radius-full);
        background: var(--color-surface-container-low);
        color: var(--color-on-surface-secondary);
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-medium);
        white-space: nowrap;
      }

      .module-badge .mat-icon {
        font-size: 0.9rem;
        width: 0.9rem;
        height: 0.9rem;
      }

      .op-badge {
        display: inline-block;
        padding: 2px var(--space-2);
        border-radius: var(--radius-md);
        background: var(--color-primary-50);
        color: var(--color-primary-900);
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-semibold);
        white-space: nowrap;
      }

      .op-badge--failed {
        background: var(--color-error-bg);
        color: var(--color-error-text);
      }

      .audit-target {
        max-width: 320px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        color: var(--color-on-surface-secondary);
      }

      /* ── Detalle ── */
      .audit-detail-row > td {
        padding: 0 !important;
        background: var(--color-surface-container-lowest);
      }

      .audit-detail {
        display: flex;
        flex-direction: column;
        gap: var(--space-4);
        padding: var(--space-5);
        border-left: 3px solid var(--color-primary-300);
      }

      .detail-description {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        line-height: 1.6;
      }

      .detail-block {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .detail-title {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        margin: 0;
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-bold);
        text-transform: uppercase;
        letter-spacing: var(--letter-spacing-label);
        color: var(--color-primary-700);
      }

      .detail-title .mat-icon {
        font-size: 1rem;
        width: 1rem;
        height: 1rem;
      }

      .changes-table {
        width: 100%;
        border-collapse: collapse;
        font-size: var(--font-size-body-sm);
      }

      .changes-table th {
        text-align: left;
        padding: var(--space-2) var(--space-3);
        font-size: var(--font-size-label);
        text-transform: uppercase;
        letter-spacing: var(--letter-spacing-label);
        color: var(--color-on-surface-variant);
        border-bottom: 1px solid var(--color-border-light);
      }

      .changes-table td {
        padding: var(--space-2) var(--space-3);
        border-bottom: 1px solid var(--color-border-light);
        vertical-align: top;
        overflow-wrap: anywhere;
      }

      .change-label {
        font-weight: var(--font-weight-semibold);
        color: var(--color-on-surface);
        width: 26%;
      }

      .change-before {
        color: var(--color-error-text);
        text-decoration: line-through;
        text-decoration-color: var(--color-error-border);
        width: 37%;
      }

      .change-after {
        color: var(--color-success-text);
        width: 37%;
      }

      .detail-facts {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: var(--space-3);
        margin: 0;
      }

      .detail-fact dt {
        font-size: var(--font-size-label);
        text-transform: uppercase;
        letter-spacing: var(--letter-spacing-label);
        color: var(--color-on-surface-variant);
        margin-bottom: 2px;
      }

      .detail-fact dd {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        overflow-wrap: anywhere;
      }

      .fact-mono {
        font-family: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
        font-size: var(--font-size-caption) !important;
      }

      .fact-failed {
        color: var(--color-error-text);
        font-weight: var(--font-weight-semibold);
      }

      .detail-raw {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-variant);
      }

      .detail-raw summary {
        cursor: pointer;
        font-weight: var(--font-weight-semibold);
      }

      .raw-title {
        margin: var(--space-2) 0 2px;
        font-weight: var(--font-weight-semibold);
      }

      .detail-raw pre {
        margin: 0;
        padding: var(--space-3);
        background: var(--color-surface-container-low);
        border-radius: var(--radius-sm);
        overflow-x: auto;
        max-height: 260px;
        white-space: pre-wrap;
        overflow-wrap: anywhere;
      }

      /* ── Paginación ── */
      .audit-pager {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: var(--space-4);
        padding: var(--space-4);
        border-top: 1px solid var(--color-border-light);
      }

      .pager-status {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
      }

      /* ── Vacío ── */
      .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--space-3);
        padding: var(--space-12) var(--space-6);
        text-align: center;
      }

      .empty-icon-box {
        width: 64px;
        height: 64px;
        border-radius: var(--radius-full);
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-primary-50);
        color: var(--color-primary-500);
      }

      .empty-icon-box .mat-icon {
        font-size: 32px;
        width: 32px;
        height: 32px;
      }

      .empty-title {
        margin: 0;
        font-size: var(--font-size-h6);
        color: var(--color-on-surface);
      }

      .empty-desc {
        margin: 0;
        max-width: 460px;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        line-height: 1.6;
      }

      @media (max-width: 768px) {
        .audit-filters {
          padding: var(--space-4) var(--space-4) 0;
        }

        .filter,
        .filter--search,
        .filter--date {
          flex: 1 1 100%;
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuditLogPanelComponent {
  private readonly auditApi = inject(AuditApiService);
  private readonly snackBar = inject(MatSnackBar);

  private readonly pageSize = 25;

  readonly logs = signal<AuditLog[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly loading = signal(false);
  readonly expandedId = signal<string | null>(null);
  readonly filterOptions = signal<AuditFilterOptions>({ modules: [], operations: [], users: [] });

  readonly search = signal('');
  readonly module = signal<string | null>(null);
  readonly userEmail = signal<string | null>(null);
  readonly operation = signal<string | null>(null);
  readonly from = signal<Date | null>(null);
  readonly to = signal<Date | null>(null);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  readonly hasFilters = computed(
    () =>
      !!this.search().trim() ||
      !!this.module() ||
      !!this.userEmail() ||
      !!this.operation() ||
      !!this.from() ||
      !!this.to()
  );

  constructor() {
    this.loadFilterOptions();
    this.load();
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.search.set('');
    this.module.set(null);
    this.userEmail.set(null);
    this.operation.set(null);
    this.from.set(null);
    this.to.set(null);
    this.applyFilters();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.expandedId.set(null);
    this.load();
  }

  toggle(id: string): void {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  moduleIcon(module: string): string {
    return MODULE_ICONS[module] ?? 'history';
  }

  /** Formatea el JSON del registro completo para poder leerlo en el detalle. */
  pretty(json: string): string {
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }

  private load(): void {
    this.loading.set(true);
    this.auditApi
      .getLogs({
        page: this.page(),
        pageSize: this.pageSize,
        search: this.search().trim() || null,
        module: this.module(),
        userEmail: this.userEmail(),
        operation: this.operation(),
        from: this.from()?.toISOString() ?? null,
        // El "hasta" incluye el día completo que eligió el usuario.
        to: this.endOfDay(this.to())
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          this.logs.set(result.items);
          this.total.set(result.total);
        },
        error: (err: HttpErrorResponse) =>
          this.snackBar.open(extractErrorMessage(err, 'Error al cargar el historial de auditoría'), 'Cerrar', {
            duration: 5000
          })
      });
  }

  private endOfDay(date: Date | null): string | null {
    if (!date) {
      return null;
    }

    const end = new Date(date);
    end.setHours(23, 59, 59, 999);
    return end.toISOString();
  }

  private loadFilterOptions(): void {
    this.auditApi.getFilterOptions().subscribe({
      next: (options) => this.filterOptions.set(options),
      // Sin las opciones el historial se sigue viendo: solo se pierden las listas de filtro.
      error: () => this.filterOptions.set({ modules: [], operations: [], users: [] })
    });
  }
}
