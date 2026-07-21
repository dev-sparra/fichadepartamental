import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { UpperCasePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { CatalogsApiService } from '../administration/services/catalogs-api.service';
import { DepartmentCatalogOption } from '../../shared/models/catalog.models';

interface ReportTemplate {
  id: string;
  name: string;
  description: string;
  icon: string;
}

const REPORT_TYPES: ReportTemplate[] = [
  {
    id: 'fichas',
    name: 'Fichas departamentales',
    description: 'Consolidado de fichas con diagnóstico, oportunidades, ejes PNMC y actores',
    icon: 'description'
  },
  {
    id: 'indicators',
    name: 'Indicadores de gestión',
    description: 'Avance de metas, cumplimiento porcentual y proyecciones por departamento',
    icon: 'bar_chart'
  },
  {
    id: 'actors',
    name: 'Directorio de actores',
    description: 'Listado de agentes del ecosistema musical con datos de contacto',
    icon: 'people'
  },
  {
    id: 'compliance',
    name: 'Informe de cumplimiento',
    description: 'Resumen ejecutivo con estado general de gobernanza y niveles de avance',
    icon: 'verified'
  }
];

@Component({
  selector: 'app-reports-home',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatSelectModule,
    MatSnackBarModule,
    UpperCasePipe
  ],
  template: `
    <section class="reports-page">
      <header class="rpt-header">
        <h1 class="rpt-title">Reportes</h1>
        <p class="rpt-subtitle">Genera y exporta informes del sistema</p>
      </header>

      <div class="rpt-grid">
        <!-- Report type selection -->
        <mat-card class="rpt-card">
          <mat-card-header>
            <mat-card-title>Tipo de reporte</mat-card-title>
            <mat-card-subtitle>Selecciona qué información quieres consolidar</mat-card-subtitle>
          </mat-card-header>
          <mat-divider />
          <mat-card-content>
            <div class="rpt-types">
              @for (type of reportTypes; track type.id) {
                <button
                  class="rpt-type-card"
                  [class.rpt-type-card--selected]="selectedType() === type.id"
                  (click)="selectedType.set(type.id)"
                >
                  <mat-icon class="rpt-type-icon">{{ type.icon }}</mat-icon>
                  <div class="rpt-type-info">
                    <strong>{{ type.name }}</strong>
                    <span>{{ type.description }}</span>
                  </div>
                  @if (selectedType() === type.id) {
                    <mat-icon class="rpt-type-check">check_circle</mat-icon>
                  }
                </button>
              }
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Filters & Generate -->
        <mat-card class="rpt-card">
          <mat-card-header>
            <mat-card-title>Parámetros</mat-card-title>
            <mat-card-subtitle>Ajusta el alcance del reporte antes de generarlo</mat-card-subtitle>
          </mat-card-header>
          <mat-divider />
          <mat-card-content>
            <form [formGroup]="reportForm" class="rpt-form">
              <mat-form-field appearance="outline">
                <mat-label>Departamento</mat-label>
                <mat-icon matPrefix>apartment</mat-icon>
                <mat-select formControlName="department">
                  <mat-option [value]="null">Todos los departamentos</mat-option>
                  @for (dept of departments(); track dept.id) {
                    <mat-option [value]="dept.id">{{ dept.name }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Año</mat-label>
                <mat-icon matPrefix>calendar_today</mat-icon>
                <mat-select formControlName="year">
                  <mat-option [value]="null">Todos los años</mat-option>
                  <mat-option value="2026">2026</mat-option>
                  <mat-option value="2025">2025</mat-option>
                  <mat-option value="2024">2024</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Formato de salida</mat-label>
                <mat-icon matPrefix>save_alt</mat-icon>
                <mat-select formControlName="format">
                  <mat-option value="excel">Excel (.xlsx)</mat-option>
                  <mat-option value="pdf">PDF</mat-option>
                  <mat-option value="csv">CSV</mat-option>
                </mat-select>
              </mat-form-field>

              <button
                mat-flat-button
                color="primary"
                type="button"
                class="rpt-generate"
                [disabled]="!selectedType() || generating()"
                (click)="generate()"
              >
                @if (generating()) {
                  <ng-container>
                    <mat-icon class="spin">sync</mat-icon>
                    Generando...
                  </ng-container>
                } @else {
                  <ng-container>
                    <mat-icon>download</mat-icon>
                    Generar reporte
                  </ng-container>
                }
              </button>
            </form>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Preview -->
      @if (generatedReport()) {
        <mat-card class="rpt-preview-card">
          <mat-card-header>
            <mat-card-title>Vista previa</mat-card-title>
            <mat-card-subtitle>{{ getReportName() }}</mat-card-subtitle>
          </mat-card-header>
          <mat-divider />
          <mat-card-content>
            <div class="preview-table-wrapper">
              <table class="preview-table">
                <thead>
                  <tr>
                    <th>Departamento</th>
                    <th class="col-num">Fichas</th>
                    <th class="col-num">Oportunidades</th>
                    <th class="col-num">Ejes PNMC</th>
                    <th class="col-num">Actores</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of previewData; track row.dept) {
                    <tr>
                      <td>{{ row.dept }}</td>
                      <td class="col-num">{{ row.fichas }}</td>
                      <td class="col-num">{{ row.opps }}</td>
                      <td class="col-num">{{ row.ejes }}</td>
                      <td class="col-num">{{ row.actors }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>

            <div class="preview-actions">
              <button mat-stroked-button type="button">
                <mat-icon>print</mat-icon>
                Imprimir
              </button>
              <button mat-flat-button color="primary" type="button" (click)="download()">
                <mat-icon>file_download</mat-icon>
                Descargar {{ reportForm.get('format')?.value | uppercase }}
              </button>
            </div>
          </mat-card-content>
        </mat-card>
      }
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .reports-page {
        display: flex;
        flex-direction: column;
        gap: var(--space-7);
      }

      .rpt-header {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .rpt-title {
        font-size: 1.75rem;
        font-weight: var(--font-weight-extrabold);
        margin: 0;
        color: var(--color-on-surface);
        letter-spacing: -0.02em;
      }

      .rpt-subtitle {
        margin: 0;
        color: var(--color-on-surface-secondary);
        font-size: var(--font-size-body);
      }

      .rpt-grid {
        display: grid;
        grid-template-columns: 1.2fr 1fr;
        gap: var(--space-6);
        align-items: start;
      }

      .rpt-card {
        border-radius: var(--radius-xl);
      }

      ::ng-deep .rpt-card > .mat-mdc-card-header {
        padding: var(--space-5) var(--space-6);
      }

      ::ng-deep .rpt-card > .mat-mdc-card-header .mat-mdc-card-header-text {
        margin: 0;
        width: 100%;
      }

      ::ng-deep .rpt-card .mat-mdc-card-title {
        font-size: var(--font-size-h6) !important;
        margin-bottom: var(--space-1) !important;
      }

      ::ng-deep .rpt-card .mat-mdc-card-subtitle {
        font-size: var(--font-size-body-sm) !important;
        color: var(--color-on-surface-secondary) !important;
        margin: 0 !important;
      }

      ::ng-deep .rpt-card > .mat-mdc-card-content {
        padding: var(--space-6) !important;
      }

      /* ── Report types ── */
      .rpt-types {
        display: flex;
        flex-direction: column;
        gap: var(--space-3);
      }

      .rpt-type-card {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-4) var(--space-5);
        border: 1px solid var(--color-border-light);
        border-radius: var(--radius-lg);
        background: var(--color-surface);
        cursor: pointer;
        text-align: left;
        transition:
          border-color var(--transition-fast),
          background-color var(--transition-fast);
      }

      .rpt-type-card:hover {
        border-color: var(--color-primary-300);
      }

      .rpt-type-card--selected {
        border-color: var(--color-primary-500);
        background: var(--color-primary-50);
      }

      .rpt-type-icon {
        color: var(--color-primary-500);
        flex-shrink: 0;
      }

      .rpt-type-info {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
        min-width: 0;
      }

      .rpt-type-info strong {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
      }

      .rpt-type-info span {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        line-height: 1.5;
        white-space: normal;
      }

      .rpt-type-check {
        color: var(--color-primary-600);
      }

      /* ── Filters form ── */
      .rpt-form {
        display: flex;
        flex-direction: column;
        gap: var(--space-5);
      }

      .rpt-generate {
        width: 100%;
        min-height: 48px;
        margin-top: var(--space-2);
        display: flex;
        align-items: center;
        justify-content: center;
        gap: var(--space-2);
      }

      .spin {
        animation: spin 1s linear infinite;
      }

      @keyframes spin {
        to { transform: rotate(360deg); }
      }

      /* ── Preview ── */
      .rpt-preview-card {
        border-radius: var(--radius-xl);
      }

      .preview-table-wrapper {
        overflow-x: auto;
      }

      .preview-table {
        width: 100%;
        border-collapse: collapse;
        font-size: var(--font-size-body-sm);
      }

      .preview-table thead {
        background: var(--color-surface-container-low);
      }

      .preview-table th {
        padding: var(--space-3) var(--space-4);
        text-align: left;
        font-weight: var(--font-weight-bold);
        color: var(--color-primary-900);
        font-size: var(--font-size-label);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        border-bottom: 1px solid var(--color-border-light);
      }

      .preview-table td {
        padding: var(--space-3) var(--space-4);
        border-bottom: 1px solid var(--color-border-light);
        color: var(--color-on-surface);
      }

      .preview-table tbody tr:hover {
        background: var(--color-hover);
      }

      .col-num {
        text-align: right;
        font-variant-numeric: tabular-nums;
      }

      .preview-actions {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: var(--space-3);
        margin-top: var(--space-4);
        padding-top: var(--space-4);
        border-top: 1px solid var(--color-border-light);
      }

      /* ── Responsive ── */
      @media (max-width: 768px) {
        .rpt-grid {
          grid-template-columns: 1fr;
        }

        .preview-actions {
          flex-direction: column;
          align-items: stretch;
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsHomeComponent {
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly catalogsApi = inject(CatalogsApiService);

  readonly reportTypes = REPORT_TYPES;
  readonly selectedType = signal<string | null>(null);
  readonly generating = signal(false);
  readonly generatedReport = signal(false);
  readonly departments = signal<DepartmentCatalogOption[]>([]);

  readonly reportForm = this.fb.group({
    department: [null as number | null],
    year: [null as number | null],
    format: ['excel']
  });

  constructor() {
    this.catalogsApi.getDepartments().subscribe({ next: (data) => this.departments.set(data) });
  }

  readonly previewData = [
    { dept: 'Antioquia', fichas: 12, opps: 48, ejes: 36, actors: 95 },
    { dept: 'Cundinamarca', fichas: 8, opps: 31, ejes: 24, actors: 67 },
    { dept: 'Valle del Cauca', fichas: 6, opps: 22, ejes: 18, actors: 43 },
    { dept: 'Atlántico', fichas: 4, opps: 15, ejes: 12, actors: 28 },
    { dept: 'Santander', fichas: 3, opps: 11, ejes: 9, actors: 21 },
    { dept: 'Bolívar', fichas: 3, opps: 10, ejes: 9, actors: 19 }
  ];

  getReportName(): string {
    return this.reportTypes.find((t) => t.id === this.selectedType())?.name ?? '';
  }

  generate(): void {
    if (!this.selectedType()) return;

    this.generating.set(true);
    setTimeout(() => {
      this.generating.set(false);
      this.generatedReport.set(true);
      this.snackBar.open('Reporte generado correctamente', 'Cerrar', { duration: 3000 });
    }, 1500);
  }

  download(): void {
    this.snackBar.open('Descargando reporte...', 'Cerrar', { duration: 3000 });
  }
}
