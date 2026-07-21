import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal
} from '@angular/core';
import { DatePipe, DecimalPipe, SlicePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { AuthTokenService } from '../../core/services/auth-token.service';
import { GovernanceApiService } from '../../core/services/governance-api.service';
import { GovernanceFichaSummary } from '../../shared/models/governance.models';

interface KpiCard {
  label: string;
  value: number;
  icon: string;
  color: string;
  bgColor: string;
}

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    SlicePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatIconModule,
    MatListModule,
    MatProgressBarModule
  ],
  template: `
    <section class="dashboard">
      <header class="dash-header">
        <div>
          <h1 class="dash-title">Panel principal</h1>
          <p class="dash-welcome">Bienvenido, {{ authService.userDisplayName }}</p>
        </div>
      </header>

      <!-- KPIs -->
      <div class="kpi-grid">
        @for (kpi of kpis(); track kpi.label) {
          <mat-card class="kpi-card">
            <mat-card-content class="kpi-content">
              <div class="kpi-icon" [style.--kpi-bg]="kpi.bgColor" [style.--kpi-color]="kpi.color">
                <mat-icon>{{ kpi.icon }}</mat-icon>
              </div>
              <div class="kpi-info">
                <span class="kpi-value">{{ kpi.value | number }}</span>
                <span class="kpi-label">{{ kpi.label }}</span>
              </div>
            </mat-card-content>
          </mat-card>
        }
      </div>

      <!-- Main content area -->
      @if (fichas().length > 0) {
        <div class="dash-grid">
          <!-- Analytical SVG Chart -->
          <mat-card class="dash-card">
            <mat-card-header class="dash-card-header">
              <mat-card-title>Oportunidades por departamento</mat-card-title>
            </mat-card-header>
            <mat-card-content class="chart-content">
              <div class="chart-wrapper">
                <svg class="dashboard-chart" viewBox="0 0 450 200" width="100%" height="200" preserveAspectRatio="xMidYMid meet">
                  <!-- Grid lines -->
                  <line x1="45" y1="20" x2="430" y2="20" stroke="var(--color-border-light)" stroke-width="1" />
                  <line x1="45" y1="70" x2="430" y2="70" stroke="var(--color-border-light)" stroke-width="1" />
                  <line x1="45" y1="120" x2="430" y2="120" stroke="var(--color-border-light)" stroke-width="1" />
                  <line x1="45" y1="170" x2="430" y2="170" stroke="var(--color-outline-variant)" stroke-width="1.5" />

                  <!-- Y-Axis labels -->
                  <text x="35" y="24" fill="var(--color-on-surface-secondary)" font-size="10" font-weight="600" text-anchor="end">{{ maxOpportunities() }}</text>
                  <text x="35" y="74" fill="var(--color-on-surface-secondary)" font-size="10" font-weight="600" text-anchor="end">{{ maxOpportunities() / 2 | number: '1.0-0' }}</text>
                  <text x="35" y="124" fill="var(--color-on-surface-secondary)" font-size="10" font-weight="600" text-anchor="end">{{ maxOpportunities() / 4 | number: '1.0-0' }}</text>
                  <text x="35" y="174" fill="var(--color-on-surface-secondary)" font-size="10" font-weight="600" text-anchor="end">0</text>

                  <!-- Bars loop -->
                  @for (ficha of chartFichas(); track ficha.id; let i = $index) {
                    <rect
                      [attr.x]="65 + i * 75"
                      [attr.y]="170 - getBarHeight(ficha.opportunitiesCount)"
                      width="28"
                      [attr.height]="getBarHeight(ficha.opportunitiesCount)"
                      rx="6"
                      fill="var(--color-primary-800)"
                      class="chart-bar"
                    >
                      <title>{{ ficha.departmentName }}: {{ ficha.opportunitiesCount }} oportunidades</title>
                    </rect>

                    <!-- Text label -->
                    <text
                      [attr.x]="79 + i * 75"
                      y="188"
                      fill="var(--color-on-surface)"
                      font-size="9"
                      font-weight="700"
                      text-anchor="middle"
                      class="chart-bar-label"
                    >
                      {{ ficha.departmentName | slice: 0: 7 }}
                    </text>
                  }
                </svg>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Quick actions -->
          <mat-card class="dash-card">
            <mat-card-header class="dash-card-header">
              <mat-card-title>Acciones rápidas</mat-card-title>
            </mat-card-header>
            <mat-card-content class="quick-actions">
              <a mat-stroked-button routerLink="/governance" class="quick-action-btn">
                <mat-icon class="action-icon">add_circle_outline</mat-icon>
                <div class="action-text">
                  <strong>Gestionar fichas</strong>
                  <span>Diligencia, valida y edita fichas departamentales</span>
                </div>
              </a>
              <a mat-stroked-button routerLink="/imports" class="quick-action-btn">
                <mat-icon class="action-icon">upload_file</mat-icon>
                <div class="action-text">
                  <strong>Importar datos</strong>
                  <span>Sube archivos masivos en formato XML/Excel</span>
                </div>
              </a>
              <a mat-stroked-button routerLink="/indicators" class="quick-action-btn">
                <mat-icon class="action-icon">bar_chart</mat-icon>
                <div class="action-text">
                  <strong>Ver indicadores</strong>
                  <span>Consulta KPIs y estadísticas generales del programa</span>
                </div>
              </a>
              <a mat-stroked-button routerLink="/reports" class="quick-action-btn">
                <mat-icon class="action-icon">description</mat-icon>
                <div class="action-text">
                  <strong>Generar reporte</strong>
                  <span>Exporta informes consolidados en formato PDF</span>
                </div>
              </a>
            </mat-card-content>
          </mat-card>
        </div>

        <!-- Department summary table -->
        <mat-card class="dash-table-card">
          <mat-card-header class="dash-card-header">
            <mat-card-title>Resumen por departamento</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="table-responsive">
              <table class="dash-table">
                <thead>
                  <tr>
                    <th>Departamento</th>
                    <th>Fecha</th>
                    <th>Responsable</th>
                    <th class="col-num">Oportunidades</th>
                    <th class="col-num">Ejes PNMC</th>
                    <th class="col-num">Actores</th>
                    <th class="col-action">Acción</th>
                  </tr>
                </thead>
                <tbody>
                  @for (ficha of fichas(); track ficha.id) {
                    <tr>
                      <td>
                        <div class="table-dept">
                          <div class="latest-dot" [style.--dot-color]="getDepartmentColor($index)"></div>
                          <span class="dept-name">{{ ficha.departmentName }}</span>
                        </div>
                      </td>
                      <td class="col-date">{{ ficha.fechaLevantamiento | date: 'mediumDate' }}</td>
                      <td class="col-resp">{{ ficha.responsableRegistro }}</td>
                      <td class="col-num font-tabular">{{ ficha.opportunitiesCount | number }}</td>
                      <td class="col-num font-tabular">{{ ficha.axesCount | number }}</td>
                      <td class="col-num font-tabular">{{ ficha.actorsCount | number }}</td>
                      <td class="col-action">
                        <a mat-icon-button class="table-action-btn" routerLink="/governance" aria-label="Ver detalle de {{ ficha.departmentName }}" matTooltip="Ver detalle">
                          <mat-icon>open_in_new</mat-icon>
                        </a>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </mat-card-content>
        </mat-card>
      } @else if (loading()) {
        <!-- Skeleton / Loading Loader state -->
        <mat-card class="dash-card dash-loading-card">
          <mat-card-content class="empty-state">
            <mat-progress-bar mode="indeterminate" class="dash-progress" />
            <p class="loading-text">Cargando registros de gobernanza...</p>
          </mat-card-content>
        </mat-card>
      } @else {
        <!-- Empty State -->
        <div class="dash-grid">
          <mat-card class="dash-card empty-card-wrapper">
            <mat-card-content class="empty-state">
              <div class="empty-icon-box">
                <mat-icon class="empty-icon">folder_open</mat-icon>
              </div>
              <h3 class="empty-title">Sin registros aún</h3>
              <p class="empty-desc">Importa datos o crea tu primera ficha departamental para comenzar el análisis.</p>
              <div class="empty-actions">
                <a mat-flat-button color="primary" class="empty-btn" routerLink="/imports">
                  <mat-icon>upload_file</mat-icon>
                  <span>Importar ficha</span>
                </a>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Quick actions -->
          <mat-card class="dash-card">
            <mat-card-header class="dash-card-header">
              <mat-card-title>Acciones rápidas</mat-card-title>
            </mat-card-header>
            <mat-card-content class="quick-actions">
              <a mat-stroked-button routerLink="/governance" class="quick-action-btn">
                <mat-icon class="action-icon">add_circle_outline</mat-icon>
                <div class="action-text">
                  <strong>Gestionar fichas</strong>
                  <span>Diligencia, valida y edita fichas departamentales</span>
                </div>
              </a>
              <a mat-stroked-button routerLink="/imports" class="quick-action-btn">
                <mat-icon class="action-icon">upload_file</mat-icon>
                <div class="action-text">
                  <strong>Importar datos</strong>
                  <span>Sube archivos masivos en formato XML/Excel</span>
                </div>
              </a>
              <a mat-stroked-button routerLink="/indicators" class="quick-action-btn">
                <mat-icon class="action-icon">bar_chart</mat-icon>
                <div class="action-text">
                  <strong>Ver indicadores</strong>
                  <span>Consulta KPIs y estadísticas generales del programa</span>
                </div>
              </a>
              <a mat-stroked-button routerLink="/reports" class="quick-action-btn">
                <mat-icon class="action-icon">description</mat-icon>
                <div class="action-text">
                  <strong>Generar reporte</strong>
                  <span>Exporta informes consolidados en formato PDF</span>
                </div>
              </a>
            </mat-card-content>
          </mat-card>
        </div>
      }
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .dashboard {
        display: flex;
        flex-direction: column;
        gap: var(--space-8); /* Increased general gap for breathing room */
      }

      /* ── Header ── */
      .dash-header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        margin-bottom: var(--space-2);
      }

      .dash-title {
        font-size: 2rem; /* Matches design system H2 size */
        font-weight: var(--font-weight-extrabold);
        color: var(--color-primary-900);
        letter-spacing: -0.02em;
        margin: 0 0 var(--space-1);
      }

      .dash-welcome {
        margin: 0;
        color: var(--color-on-surface-secondary);
        font-size: var(--font-size-body);
        font-weight: var(--font-weight-medium);
      }

      /* ── KPI Cards ── */
      .kpi-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: var(--space-6); /* Spacious gap */
      }

      .kpi-card {
        background: #ffffff;
        border: 1px solid var(--color-border-light);
        border-radius: var(--radius-xl);
        box-shadow: var(--shadow-sm) !important;
        transition: transform var(--transition-fast), box-shadow var(--transition-fast);
      }

      .kpi-card:hover {
        transform: translateY(-2px);
        box-shadow: var(--shadow-md) !important;
      }

      .kpi-content {
        display: flex;
        align-items: center;
        gap: var(--space-5);
        padding: var(--space-5) var(--space-6) !important; /* Spacious internal padding */
      }

      .kpi-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 50px;
        height: 50px;
        border-radius: var(--radius-xl);
        background: var(--kpi-bg);
        color: var(--kpi-color);
        flex-shrink: 0;
      }

      .kpi-icon .mat-icon {
        font-size: 1.6rem;
        width: 1.6rem;
        height: 1.6rem;
      }

      .kpi-info {
        display: flex;
        flex-direction: column;
        gap: 1px;
      }

      .kpi-value {
        font-size: 1.85rem;
        font-weight: var(--font-weight-extrabold);
        color: var(--color-primary-900);
        line-height: 1.15;
      }

      .kpi-label {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-semibold);
      }

      /* ── Main grid ── */
      .dash-grid {
        display: grid;
        grid-template-columns: 1.25fr 0.75fr; /* Dynamic asymmetric width */
        gap: var(--space-6);
      }

      .dash-card {
        background: #ffffff;
        border: 1px solid var(--color-border-light);
        border-radius: var(--radius-xl);
        box-shadow: var(--shadow-sm) !important;
        padding: var(--space-6) !important; /* Generous card padding */
      }

      .dash-card-header {
        padding: 0 0 var(--space-5) 0 !important; /* Separator by spacing */
      }

      ::ng-deep .dash-card-header .mat-mdc-card-title {
        font-size: var(--font-size-h5) !important;
        font-weight: var(--font-weight-extrabold) !important;
        color: var(--color-primary-900);
        letter-spacing: -0.015em;
      }

      /* ── SVG Chart ── */
      .chart-content {
        padding: 0 !important;
      }

      .chart-wrapper {
        width: 100%;
        padding-top: var(--space-2);
      }

      .dashboard-chart {
        display: block;
        margin: 0 auto;
      }

      .chart-bar {
        transition: fill var(--transition-fast);
        cursor: pointer;
      }

      .chart-bar:hover {
        fill: var(--color-primary-500);
      }

      .chart-bar-label {
        font-family: var(--font-family);
        fill: var(--color-on-surface-secondary);
      }

      /* ── Quick actions ── */
      .quick-actions {
        display: flex;
        flex-direction: column;
        gap: var(--space-4); /* Separated action cards */
        padding: 0 !important;
      }

      ::ng-deep .quick-actions .quick-action-btn.mat-mdc-outlined-button {
        display: flex !important;
        align-items: center !important;
        justify-content: flex-start !important;
        padding: var(--space-4) var(--space-5) !important;
        height: auto !important;
        min-height: 72px;
        border-radius: var(--radius-xl) !important;
        border: 1px solid var(--color-border-light) !important;
        background: var(--color-surface-container-lowest) !important;
        transition: background-color var(--transition-fast), border-color var(--transition-fast), box-shadow var(--transition-fast) !important;
        text-align: left !important;
      }

      ::ng-deep .quick-actions .quick-action-btn.mat-mdc-outlined-button:hover {
        background: var(--color-primary-50) !important;
        border-color: var(--color-primary-300) !important;
        box-shadow: var(--shadow-xs) !important;
      }

      .action-icon {
        font-size: 24px;
        width: 24px;
        height: 24px;
        color: var(--color-primary-700);
        margin-right: var(--space-4);
        flex-shrink: 0;
      }

      .action-text {
        display: flex;
        flex-direction: column;
        line-height: 1.35;
        white-space: normal;
      }

      .action-text strong {
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-bold);
        color: var(--color-primary-900);
      }

      .action-text span {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-medium);
        margin-top: 1px;
      }

      /* ── Table ── */
      .dash-table-card {
        background: #ffffff;
        border: 1px solid var(--color-border-light);
        border-radius: var(--radius-xl);
        box-shadow: var(--shadow-sm) !important;
        padding: var(--space-6) !important;
      }

      .table-responsive {
        overflow-x: auto;
        border-radius: var(--radius-lg);
      }

      .dash-table {
        width: 100%;
        border-collapse: separate;
        border-spacing: 0;
        font-size: var(--font-size-body-sm);
      }

      .dash-table thead {
        background: var(--color-surface-container-low);
      }

      .dash-table th {
        padding: var(--space-4) var(--space-5);
        text-align: left;
        font-weight: var(--font-weight-bold);
        color: var(--color-primary-900);
        font-size: var(--font-size-label);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        border-bottom: 1px solid var(--color-border-light);
      }

      .dash-table th:first-child {
        border-top-left-radius: var(--radius-lg);
        border-bottom-left-radius: var(--radius-lg);
      }

      .dash-table th:last-child {
        border-top-right-radius: var(--radius-lg);
        border-bottom-right-radius: var(--radius-lg);
      }

      .dash-table td {
        padding: var(--space-4) var(--space-5);
        color: var(--color-on-surface);
        border-bottom: 1px solid var(--color-border-light);
        transition: background-color var(--transition-fast);
      }

      .dash-table tbody tr:hover td {
        background: var(--color-hover);
      }

      .dash-table tbody tr:last-child td {
        border-bottom: none;
      }

      .table-dept {
        display: flex;
        align-items: center;
        gap: var(--space-3);
      }

      .dept-name {
        font-weight: var(--font-weight-semibold);
        color: var(--color-on-surface);
      }

      .latest-dot {
        width: 10px;
        height: 10px;
        border-radius: var(--radius-full);
        background: var(--dot-color);
        display: inline-block;
        flex-shrink: 0;
        box-shadow: 0 0 0 2px #ffffff, 0 0 0 3px var(--dot-color);
      }

      .col-num {
        text-align: right;
      }

      .font-tabular {
        font-variant-numeric: tabular-nums;
        font-weight: var(--font-weight-semibold);
      }

      .col-date {
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-medium);
      }

      .col-resp {
        color: var(--color-on-surface);
        font-weight: var(--font-weight-medium);
      }

      .col-action {
        text-align: center;
        width: 60px;
      }

      ::ng-deep .table-action-btn.mat-mdc-icon-button {
        color: var(--color-primary-700) !important;
        transition: background-color var(--transition-fast) !important;
      }

      ::ng-deep .table-action-btn.mat-mdc-icon-button:hover {
        background-color: var(--color-primary-50) !important;
        color: var(--color-primary-900) !important;
      }

      /* ── Empty & Loading States ── */
      .empty-card-wrapper {
        display: flex;
        justify-content: center;
        align-items: center;
      }

      .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: var(--space-12) var(--space-6);
        text-align: center;
        gap: var(--space-4);
        width: 100%;
      }

      .empty-icon-box {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 72px;
        height: 72px;
        border-radius: var(--radius-full);
        background: var(--color-surface-container-low);
        color: var(--color-outline);
        margin-bottom: var(--space-2);
      }

      .empty-icon {
        font-size: 2.2rem;
        width: 2.2rem;
        height: 2.2rem;
      }

      .empty-title {
        font-size: var(--font-size-h6);
        font-weight: var(--font-weight-extrabold);
        color: var(--color-primary-900);
        margin: 0;
        letter-spacing: -0.01em;
      }

      .empty-desc {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        margin: 0 0 var(--space-2);
        max-width: 320px;
        line-height: 1.5;
        font-weight: var(--font-weight-medium);
      }

      ::ng-deep .empty-btn.mat-mdc-unelevated-button {
        background-color: var(--color-primary-900) !important;
        color: #ffffff !important;
        font-weight: var(--font-weight-bold) !important;
        padding: 0 var(--space-6) !important;
        height: 44px !important;
        border-radius: var(--radius-lg) !important;
      }

      .dash-loading-card {
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .dash-progress {
        width: 100%;
        max-width: 280px;
        border-radius: var(--radius-full);
      }

      .loading-text {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-semibold);
      }

      /* ── Responsive ── */
      @media (max-width: 1024px) {
        .kpi-grid {
          grid-template-columns: repeat(2, 1fr);
          gap: var(--space-4);
        }
        .dash-grid {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 768px) {
        .kpi-grid {
          grid-template-columns: repeat(2, 1fr);
        }

        .dash-table th:nth-child(3),
        .dash-table td:nth-child(3) {
          display: none;
        }
      }

      @media (max-width: 480px) {
        .kpi-grid {
          grid-template-columns: 1fr;
          gap: var(--space-3);
        }

        .dash-table th:nth-child(2),
        .dash-table td:nth-child(2) {
          display: none;
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardHomeComponent {
  private readonly governanceApiService = inject(GovernanceApiService);
  readonly authService = inject(AuthTokenService);

  readonly fichas = signal<GovernanceFichaSummary[]>([]);
  readonly loading = signal(true);

  private readonly departmentColors = [
    '#6250a6', '#4b3c8c', '#8977d1', '#b2a2f5',
    '#00876f', '#00a387', '#4edcbb', '#73f9d5'
  ];

  readonly maxOpportunities = computed(() => {
    const list = this.fichas();
    if (list.length === 0) return 100;
    const maxVal = Math.max(...list.map((f) => f.opportunitiesCount));
    return maxVal > 0 ? maxVal : 100;
  });

  readonly chartFichas = computed(() => {
    return [...this.fichas()]
      .sort((a, b) => b.opportunitiesCount - a.opportunitiesCount)
      .slice(0, 5);
  });

  readonly kpis = computed<KpiCard[]>(() => {
    const items = this.fichas();
    return [
      {
        label: 'Fichas registradas',
        value: items.length,
        icon: 'description',
        color: 'var(--color-primary-700)',
        bgColor: 'var(--color-primary-50)'
      },
      {
        label: 'Oportunidades',
        value: items.reduce((sum, f) => sum + f.opportunitiesCount, 0),
        icon: 'lightbulb',
        color: '#1b7a3d',
        bgColor: '#e6f4ea'
      },
      {
        label: 'Ejes PNMC',
        value: items.reduce((sum, f) => sum + f.axesCount, 0),
        icon: 'layers',
        color: '#b86000',
        bgColor: '#fff3e0'
      },
      {
        label: 'Actores mapeados',
        value: items.reduce((sum, f) => sum + f.actorsCount, 0),
        icon: 'people',
        color: '#005a9e',
        bgColor: '#e3f0fa'
      }
    ];
  });

  constructor() {
    this.governanceApiService.getFichas().subscribe({
      next: (fichas) => {
        this.fichas.set(fichas);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  getDepartmentColor(index: number): string {
    return this.departmentColors[index % this.departmentColors.length];
  }

  getBarHeight(value: number): number {
    const max = this.maxOpportunities();
    if (max === 0) return 0;
    return (value / max) * 150;
  }
}
