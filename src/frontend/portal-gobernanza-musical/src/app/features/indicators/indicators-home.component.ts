import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe, SlicePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';

import { CatalogsApiService } from '../administration/services/catalogs-api.service';
import { IndicatorsApiService } from '../../core/services/indicators-api.service';
import { IndicatorDetailRecord, IndicatorMonthlyProgress, IndicatorRecord, IndicatorWorksheet } from '../../shared/models/indicator.models';
import { CatalogOption, DepartmentCatalogOption } from '../../shared/models/catalog.models';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';

function complianceColor(pct: number): string {
  if (pct >= 0.8) return 'var(--color-success)';
  if (pct >= 0.5) return 'var(--color-warning)';
  return 'var(--color-error)';
}

function complianceBg(pct: number): string {
  if (pct >= 0.8) return 'var(--color-success-bg)';
  if (pct >= 0.5) return 'var(--color-warning-bg)';
  return 'var(--color-error-bg)';
}

function complianceLabel(pct: number): string {
  if (pct >= 0.8) return 'En cumplimiento';
  if (pct >= 0.5) return 'En riesgo';
  return 'Retrasado';
}

@Component({
  selector: 'app-indicators-home',
  standalone: true,
  imports: [
    DecimalPipe,
    SlicePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTabsModule,
    MatTooltipModule
  ],
  template: `
    <section class="indicators-page">
      <header class="ind-header">
        <h1 class="ind-title">Indicadores</h1>
        <p class="ind-subtitle">Seguimiento de metas y avances por departamento</p>
      </header>

      <mat-card class="ind-provision-card">
        <mat-card-content class="provision-content">
          <div class="provision-intro">
            <mat-icon>playlist_add_check</mat-icon>
            <div>
              <strong>Diligenciar indicadores</strong>
              <p>Selecciona departamento y año para iniciar o continuar el diligenciamiento de los 7 indicadores fijos y sus detalles.</p>
            </div>
          </div>
          <div class="provision-controls">
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Departamento</mat-label>
              <mat-select [formControl]="provisionDepartment">
                @for (dept of departments(); track dept.id) {
                  <mat-option [value]="dept.id">{{ dept.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Año</mat-label>
              <mat-select [formControl]="provisionYear">
                @for (year of yearOptions(); track year.id) {
                  <mat-option [value]="year.id">{{ year.name }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <button mat-flat-button color="primary" type="button" [disabled]="!provisionDepartment.value || !provisionYear.value || saving()" (click)="startWorksheet()">
              <mat-icon>edit_note</mat-icon>
              Diligenciar
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Filters -->
      <div class="filter-bar">
        <div class="filter-bar-inner">
          <span class="filter-bar-label">
            <mat-icon>filter_list</mat-icon>
            Filtrar por
          </span>

          <div class="filter-buttons">
            <button type="button" class="filter-btn" [class.filter-btn--active]="!!filterDepartment.value" (click)="toggleFilterDropdown('dept')">
              <mat-icon>apartment</mat-icon>
              <span class="filter-btn-text">{{ filterDepartment.value ? selectedDepartmentName() : 'Departamento' }}</span>
              <mat-icon class="filter-btn-arrow">expand_more</mat-icon>
            </button>

            @if (filterDropdown() === 'dept') {
              <div class="filter-dropdown">
                <button type="button" class="filter-dropdown-item" [class.filter-dropdown-item--active]="!filterDepartment.value" (click)="setDepartmentFilter(null)">
                  Todos los departamentos
                </button>
                @for (dept of departments(); track dept.id) {
                  <button type="button" class="filter-dropdown-item" [class.filter-dropdown-item--active]="filterDepartment.value === dept.id" (click)="setDepartmentFilter(dept.id)">
                    {{ dept.name }}
                  </button>
                }
              </div>
            }

            <button type="button" class="filter-btn" [class.filter-btn--active]="!!filterYear.value" (click)="toggleFilterDropdown('year')">
              <mat-icon>calendar_today</mat-icon>
              <span class="filter-btn-text">{{ filterYear.value ?? 'Año' }}</span>
              <mat-icon class="filter-btn-arrow">expand_more</mat-icon>
            </button>

            @if (filterDropdown() === 'year') {
              <div class="filter-dropdown">
                <button type="button" class="filter-dropdown-item" [class.filter-dropdown-item--active]="!filterYear.value" (click)="setYearFilter(null)">
                  Todos los años
                </button>
                @for (year of availableYears(); track year) {
                  <button type="button" class="filter-dropdown-item" [class.filter-dropdown-item--active]="filterYear.value === year" (click)="setYearFilter(year)">
                    {{ year }}
                  </button>
                }
              </div>
            }

            @if (activeFilterCount() > 0) {
              <button type="button" class="filter-clear-btn" (click)="clearFilters()">
                <mat-icon>close</mat-icon>
              </button>
            }
          </div>
        </div>
      </div>

      <!-- KPI summary -->
      @if (filteredRecords().length > 0) {
        <div class="kpi-row">
          <div class="kpi-item">
            <span class="kpi-num">{{ filteredRecords().length }}</span>
            <span class="kpi-lbl">Indicadores</span>
          </div>
          <div class="kpi-item kpi-item--success">
            <span class="kpi-num">{{ onTrackCount() }}</span>
            <span class="kpi-lbl">En cumplimiento</span>
          </div>
          <div class="kpi-item kpi-item--warning">
            <span class="kpi-num">{{ atRiskCount() }}</span>
            <span class="kpi-lbl">En riesgo</span>
          </div>
          <div class="kpi-item kpi-item--error">
            <span class="kpi-num">{{ behindCount() }}</span>
            <span class="kpi-lbl">Retrasados</span>
          </div>
        </div>
      }

      <mat-tab-group [selectedIndex]="activeTab()" (selectedIndexChange)="activeTab.set($event)">
        <!-- Tab 1: Records -->
        <mat-tab label="Avance por indicador">
          @if (loading()) {
            <div class="ind-loading">
              <mat-progress-bar mode="indeterminate" />
              <p>Cargando indicadores...</p>
            </div>
          } @else if (filteredRecords().length === 0) {
            <div class="ind-empty">
              <mat-icon>bar_chart_off</mat-icon>
              <p>No hay indicadores registrados con los filtros actuales.</p>
            </div>
          } @else {
            <div class="accordion-list">
              @for (record of filteredRecords(); track record.id; let idx = $index) {
                <div class="accordion-card" [class.accordion-card--expanded]="expandedRecordId() === record.id" [class.accordion-card--editing]="editRecordId() === record.id">
                  <button class="accordion-header" type="button" (click)="toggleRecord(record.id)">
                    <div class="accordion-header-left">
                      <span class="indicator-badge" [style.background]="complianceBg(record.compliancePercentageCalculated)" [style.color]="complianceColor(record.compliancePercentageCalculated)">
                        {{ idx + 1 }}
                      </span>
                      <div class="accordion-header-info">
                        <span class="accordion-title">{{ record.indicatorName }}</span>
                        <span class="accordion-action">{{ record.actionName }}</span>
                      </div>
                    </div>
                    <div class="accordion-header-center">
                      <div class="mini-compliance">
                        <div class="mini-compliance-bar">
                          <div class="mini-compliance-fill" [style.width.%]="record.compliancePercentageCalculated * 100" [style.background]="complianceColor(record.compliancePercentageCalculated)"></div>
                        </div>
                        <span class="mini-compliance-pct" [style.color]="complianceColor(record.compliancePercentageCalculated)">
                          {{ (record.compliancePercentageCalculated * 100) | number: '1.0-0' }}%
                        </span>
                      </div>
                      <div class="mini-months">
                        <mat-icon class="mini-months-icon">calendar_month</mat-icon>
                        <span>{{ completedMonthsCount(record) }}/12</span>
                      </div>
                    </div>
                    <mat-icon class="accordion-chevron" [class.accordion-chevron--open]="expandedRecordId() === record.id">expand_more</mat-icon>
                  </button>

                  @if (expandedRecordId() === record.id) {
                    <div class="accordion-body">
                      <div class="record-summary-row">
                        <div class="summary-pill">
                          <span class="summary-pill-label">Meta</span>
                          <span class="summary-pill-value">{{ record.targetValue | number }}</span>
                        </div>
                        <div class="summary-pill">
                          <span class="summary-pill-label">Valor actual</span>
                          <span class="summary-pill-value">{{ record.currentValueCalculated | number }}</span>
                        </div>
                        <div class="summary-pill" [style.border-color]="complianceColor(record.compliancePercentageCalculated)" [style.background]="complianceBg(record.compliancePercentageCalculated)">
                          <span class="summary-pill-label">Cumplimiento</span>
                          <span class="summary-pill-value" [style.color]="complianceColor(record.compliancePercentageCalculated)">{{ (record.compliancePercentageCalculated * 100) | number: '1.1-1' }}%</span>
                        </div>
                        <div class="summary-pill summary-pill--status" [style.border-color]="complianceColor(record.compliancePercentageCalculated)" [style.background]="complianceBg(record.compliancePercentageCalculated)">
                          <span class="status-dot" [style.background]="complianceColor(record.compliancePercentageCalculated)"></span>
                          <span class="summary-pill-value" [style.color]="complianceColor(record.compliancePercentageCalculated)">{{ complianceLabel(record.compliancePercentageCalculated) }}</span>
                        </div>
                      </div>

                      <!-- Month timeline (read-only) -->
                      @if (editRecordId() !== record.id) {
                        <div class="month-timeline">
                          @for (progress of record.monthlyProgresses; track progress.id) {
                            <div
                              class="timeline-cell"
                              [class.timeline-cell--filled]="progress.quantitativeAdvance !== null"
                              [class.timeline-cell--has-detail]="!!progress.detail"
                              [matTooltip]="tooltipForProgress(progress)"
                              [matTooltipDisabled]="!progress.quantitativeAdvance && !progress.detail"
                            >
                              <span class="timeline-month">{{ progress.monthName | slice:0:3 }}</span>
                              <span class="timeline-value">{{ progress.quantitativeAdvance ?? '—' }}</span>
                              @if (progress.detail) {
                                <mat-icon class="timeline-note-icon">sticky_note_2</mat-icon>
                              }
                            </div>
                          }
                        </div>

                        @if (record.source || record.generalObservations) {
                          <div class="record-notes">
                            @if (record.source) {
                              <p><strong>Fuente:</strong> {{ record.source }}</p>
                            }
                            @if (record.generalObservations) {
                              <p><strong>Obs.:</strong> {{ record.generalObservations }}</p>
                            }
                          </div>
                        }

                        <div class="accordion-actions">
                          <button mat-flat-button color="primary" type="button" (click)="editRecord(record); $event.stopPropagation()">
                            <mat-icon>edit</mat-icon>
                            Editar avances
                          </button>
                        </div>
                      }

                      <!-- Edit form -->
                      @if (editRecordId() === record.id) {
                        <form [formGroup]="editForm" (ngSubmit)="saveRecord(record.id)" class="edit-form">
                          <div class="months-editor-summary">
                            <div class="summary-stat">
                              <span class="summary-num">{{ monthsFilledCount() }}/12</span>
                              <span class="summary-lbl">Meses diligenciados</span>
                            </div>
                            <div class="summary-stat">
                              <span class="summary-num">{{ currentValuePreview() | number }}</span>
                              <span class="summary-lbl">Valor actual (estimado)</span>
                            </div>
                            <div class="summary-stat" [style.color]="complianceColor(compliancePreview())">
                              <span class="summary-num">{{ (compliancePreview() * 100) | number: '1.1-1' }}%</span>
                              <span class="summary-lbl">Cumplimiento estimado</span>
                            </div>
                          </div>

                          <p class="form-section-label">Avance cuantitativo y detalle cualitativo por mes</p>
                          <div class="month-grid">
                            @for (quarter of quarterGroups(); track $index; let qi = $index) {
                              <div class="quarter-group">
                                <span class="quarter-label">T{{ qi + 1 }}</span>
                                <div class="quarter-months">
                                  @for (row of quarter; track row.monthOptionId; let i = $index) {
                                    <div class="month-cell" [class.month-cell--filled]="row.quantitativeAdvance !== null">
                                      <div class="month-cell-header">
                                        <span class="month-cell-name">{{ row.monthName }}</span>
                                        @if (row.quantitativeAdvance !== null) {
                                          <span class="month-cell-check"><mat-icon>check_circle</mat-icon></span>
                                        }
                                      </div>
                                      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="month-cell-number">
                                        <mat-label>Avance</mat-label>
                                        <input
                                          matInput
                                          type="number"
                                          step="0.01"
                                          [value]="row.quantitativeAdvance ?? ''"
                                          (input)="onMonthQuantitativeInput(globalMonthIndex(qi, i), $event)"
                                        />
                                      </mat-form-field>
                                      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="month-cell-detail">
                                        <mat-label>Detalle cualitativo</mat-label>
                                        <textarea
                                          matInput
                                          rows="2"
                                          [value]="row.detail ?? ''"
                                          (input)="onMonthDetailInput(globalMonthIndex(qi, i), $event)"
                                          placeholder="Descripción del avance..."
                                        ></textarea>
                                      </mat-form-field>
                                    </div>
                                  }
                                </div>
                              </div>
                            }
                          </div>

                          <mat-divider />

                          <div class="edit-form-footer">
                            <mat-form-field appearance="outline" class="form-full">
                              <mat-label>Fuente</mat-label>
                              <input matInput formControlName="source" />
                            </mat-form-field>
                            <mat-form-field appearance="outline" class="form-full">
                              <mat-label>Observaciones generales</mat-label>
                              <textarea matInput formControlName="generalObservations" rows="2"></textarea>
                            </mat-form-field>
                          </div>

                          <div class="edit-form-actions">
                            <button mat-button type="button" (click)="cancelEdit()">Cancelar</button>
                            <button mat-flat-button color="primary" type="button" (click)="saveRecord(record.id)" [disabled]="saving()">
                              <mat-icon>save</mat-icon>
                              Guardar cambios
                            </button>
                          </div>
                        </form>
                      }
                    </div>
                  }
                </div>
              }
            </div>
          }
        </mat-tab>

        <!-- Tab 2: Details -->
        <mat-tab label="Detalle de Indicadores">
          @if (loading()) {
            <div class="ind-loading">
              <mat-progress-bar mode="indeterminate" />
              <p>Cargando detalles...</p>
            </div>
          } @else if (filteredDetails().length === 0) {
            <div class="ind-empty">
              <mat-icon>functions_off</mat-icon>
              <p>No hay detalles de indicadores registrados.</p>
            </div>
          } @else {
            <div class="detail-list">
              @for (detail of filteredDetails(); track detail.id; let idx = $index) {
                <div class="detail-card" [class.detail-card--editing]="editDetailId() === detail.id">
                  <div class="detail-card-header">
                    <span class="detail-badge">{{ idx + 1 }}</span>
                    <div class="detail-card-info">
                      <span class="detail-card-title">{{ detail.formulaLabel }}</span>
                      <span class="detail-card-subtitle">{{ detail.departmentName }} &middot; {{ detail.indicatorName }} &middot; {{ detail.year }}</span>
                    </div>
                  </div>

                  <div class="detail-card-body">
                    <div class="detail-description-box">
                      <mat-icon class="detail-desc-icon">description</mat-icon>
                      <p class="detail-desc">{{ detail.detailDescription }}</p>
                    </div>

                    <div class="detail-value-row">
                      <span class="detail-label">Valor actual</span>
                      <span class="detail-num">{{ detail.currentValueCalculated ?? 'Pendiente' }}</span>
                    </div>

                    @if (editDetailId() !== detail.id) {
                      @if (detail.selectedMonthIds.length > 0) {
                        <div class="detail-months-section">
                          <span class="detail-months-label">Meses aplicables</span>
                          <div class="detail-months-grid">
                            @for (month of monthOptions(); track month.id) {
                              <span
                                class="detail-month-chip"
                                [class.detail-month-chip--active]="detail.selectedMonthIds.includes(month.id)"
                              >
                                {{ month.name | slice:0:3 }}
                              </span>
                            }
                          </div>
                        </div>
                      }

                      @if (detail.source || detail.observations) {
                        <div class="record-notes">
                          @if (detail.source) {
                            <p><strong>Fuente:</strong> {{ detail.source }}</p>
                          }
                          @if (detail.observations) {
                            <p><strong>Obs.:</strong> {{ detail.observations }}</p>
                          }
                        </div>
                      }

                      <div class="accordion-actions">
                        <button mat-flat-button color="primary" type="button" (click)="editDetail(detail)">
                          <mat-icon>edit</mat-icon>
                          Editar
                        </button>
                      </div>
                    }

                    @if (editDetailId() === detail.id) {
                      <form [formGroup]="editDetailForm" class="edit-form">
                        <p class="form-section-label">Selecciona los meses en que aplica este detalle</p>
                        <div class="month-toggle-grid">
                          @for (month of monthOptions(); track month.id) {
                            <button
                              type="button"
                              class="month-toggle"
                              [class.month-toggle--active]="isMonthSelected(month.id)"
                              (click)="toggleMonth(month.id)"
                            >
                              <span class="month-toggle-name">{{ month.name }}</span>
                              @if (isMonthSelected(month.id)) {
                                <mat-icon class="month-toggle-check">check</mat-icon>
                              }
                            </button>
                          }
                        </div>
                        <span class="month-toggle-count">{{ editDetailForm.value.selectedMonthIds?.length ?? 0 }} de 12 meses seleccionados</span>

                        <mat-divider />

                        <mat-form-field appearance="outline" class="form-full">
                          <mat-label>Fuente</mat-label>
                          <input matInput formControlName="source" />
                        </mat-form-field>
                        <mat-form-field appearance="outline" class="form-full">
                          <mat-label>Observaciones</mat-label>
                          <textarea matInput formControlName="observations" rows="2"></textarea>
                        </mat-form-field>

                        <div class="edit-form-actions">
                          <button mat-button type="button" (click)="cancelEdit()">Cancelar</button>
                          <button mat-flat-button color="primary" type="button" (click)="saveDetail(detail.id)" [disabled]="saving()">
                            <mat-icon>save</mat-icon>
                            Guardar
                          </button>
                        </div>
                      </form>
                    }
                  </div>
                </div>
              }
            </div>
          }
        </mat-tab>
      </mat-tab-group>
    </section>
  `,
  styles: [
    `
      :host { display: block; }
      .indicators-page { display: flex; flex-direction: column; gap: var(--space-7); }
      .ind-header { display: flex; flex-direction: column; gap: var(--space-2); }
      .ind-title { font-size: 1.75rem; font-weight: var(--font-weight-extrabold); margin: 0; color: var(--color-on-surface); letter-spacing: -0.02em; }
      .ind-subtitle { margin: 0; color: var(--color-on-surface-secondary); font-size: var(--font-size-body); }
      .ind-provision-card { border-radius: var(--radius-xl); border: 1px solid var(--color-primary-200); background: var(--color-primary-50); box-shadow: var(--shadow-sm); }
      .provision-content { display: flex; align-items: center; justify-content: space-between; gap: var(--space-5); flex-wrap: wrap; padding: var(--space-2) 0; }
      .provision-intro { display: flex; align-items: center; gap: var(--space-3); }
      .provision-intro mat-icon { color: var(--color-primary-700); flex-shrink: 0; }
      .provision-intro strong { display: block; color: var(--color-on-surface); font-size: var(--font-size-body); font-weight: var(--font-weight-bold); }
      .provision-intro p { margin: 2px 0 0; font-size: var(--font-size-caption); color: var(--color-on-surface-secondary); max-width: 440px; }
      .provision-controls { display: flex; align-items: center; gap: var(--space-3); flex-wrap: wrap; }
      .provision-controls mat-form-field { min-width: 160px; }
      .filter-bar { padding: var(--space-3) var(--space-6); background: #ffffff; border: 1px solid var(--color-border-light); border-radius: var(--radius-xl); box-shadow: var(--shadow-sm); }
      .filter-bar-inner { display: flex; align-items: center; gap: var(--space-4); flex-wrap: wrap; }
      .filter-bar-label { display: flex; align-items: center; gap: var(--space-2); font-size: var(--font-size-body-sm); font-weight: var(--font-weight-bold); color: var(--color-on-surface-secondary); white-space: nowrap; }
      .filter-bar-label mat-icon { font-size: 1.125rem; width: 1.125rem; height: 1.125rem; color: var(--color-on-surface-secondary); }
      .filter-buttons { display: flex; align-items: center; gap: var(--space-2); flex-wrap: wrap; position: relative; }
      .filter-btn { display: inline-flex; align-items: center; gap: var(--space-2); padding: var(--space-2) var(--space-3); border-radius: var(--radius-lg); border: 1px solid var(--color-border-light); background: #ffffff; color: var(--color-on-surface-secondary); font-size: var(--font-size-body-sm); font-weight: var(--font-weight-semibold); cursor: pointer; transition: all var(--transition-fast); font-family: inherit; height: 40px; }
      .filter-btn:hover { border-color: var(--color-primary-300); background: var(--color-primary-50); color: var(--color-primary-700); }
      .filter-btn--active { border-color: var(--color-primary-400); background: var(--color-primary-50); color: var(--color-primary-700); }
      .filter-btn mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
      .filter-btn-text { white-space: nowrap; max-width: 160px; overflow: hidden; text-overflow: ellipsis; }
      .filter-btn-arrow { font-size: 1rem; width: 1rem; height: 1rem; transition: transform var(--transition-fast); }
      .filter-btn--active .filter-btn-arrow { transform: rotate(180deg); }
      .filter-dropdown { position: absolute; top: calc(100% + 4px); left: 0; z-index: var(--z-dropdown); min-width: 220px; max-height: 260px; overflow-y: auto; background: #ffffff; border: 1px solid var(--color-border-light); border-radius: var(--radius-lg); box-shadow: var(--shadow-lg); padding: var(--space-1); }
      .filter-dropdown-item { display: block; width: 100%; padding: var(--space-2) var(--space-3); border: none; background: transparent; text-align: left; font-size: var(--font-size-body-sm); color: var(--color-on-surface); cursor: pointer; border-radius: var(--radius-md); transition: background-color var(--transition-fast); font-family: inherit; }
      .filter-dropdown-item:hover { background: var(--color-hover); }
      .filter-dropdown-item--active { background: var(--color-primary-50); color: var(--color-primary-700); font-weight: var(--font-weight-bold); }
      .filter-clear-btn { display: inline-flex; align-items: center; justify-content: center; width: 32px; height: 32px; border-radius: var(--radius-full); border: 1px solid var(--color-border-light); background: #ffffff; color: var(--color-on-surface-secondary); cursor: pointer; transition: all var(--transition-fast); padding: 0; }
      .filter-clear-btn:hover { background: var(--color-error-bg); border-color: var(--color-error-border); color: var(--color-error); }
      .filter-clear-btn mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
      .kpi-row { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--space-4); }
      .kpi-item { background: #ffffff; border: 1px solid var(--color-border-light); border-radius: var(--radius-xl); padding: var(--space-5); display: flex; flex-direction: column; gap: 4px; transition: box-shadow var(--transition-fast), transform var(--transition-fast); }
      .kpi-item:hover { box-shadow: var(--shadow-md); transform: translateY(-1px); }
      .kpi-num { font-size: 1.75rem; font-weight: var(--font-weight-extrabold); color: var(--color-on-surface); line-height: 1.2; }
      .kpi-lbl { font-size: var(--font-size-caption); color: var(--color-on-surface-secondary); font-weight: var(--font-weight-semibold); }
      .kpi-item--success { border-left: 4px solid var(--color-success); }
      .kpi-item--success .kpi-num { color: var(--color-success); }
      .kpi-item--warning { border-left: 4px solid var(--color-warning); }
      .kpi-item--warning .kpi-num { color: var(--color-warning); }
      .kpi-item--error { border-left: 4px solid var(--color-error); }
      .kpi-item--error .kpi-num { color: var(--color-error); }

      .accordion-list { display: flex; flex-direction: column; gap: var(--space-4); padding: var(--space-5) 0; }
      .accordion-card { border: 1px solid var(--color-border-light); border-radius: var(--radius-xl); background: #ffffff; overflow: hidden; transition: box-shadow var(--transition-fast), border-color var(--transition-fast); }
      .accordion-card:hover { box-shadow: var(--shadow-sm); }
      .accordion-card--expanded { box-shadow: var(--shadow-md); border-color: var(--color-primary-200); }
      .accordion-card--editing { border-color: var(--color-primary-300); box-shadow: var(--shadow-md); }
      .accordion-header { display: flex; align-items: center; gap: var(--space-4); padding: var(--space-4) var(--space-5); width: 100%; border: none; background: transparent; cursor: pointer; text-align: left; transition: background-color var(--transition-fast); }
      .accordion-header:hover { background: var(--color-hover); }
      .accordion-header-left { display: flex; align-items: center; gap: var(--space-3); flex: 1; min-width: 0; }
      .indicator-badge { display: inline-flex; align-items: center; justify-content: center; width: 32px; height: 32px; border-radius: var(--radius-full); font-size: var(--font-size-caption); font-weight: var(--font-weight-extrabold); flex-shrink: 0; }
      .accordion-header-info { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
      .accordion-title { font-size: var(--font-size-body); font-weight: var(--font-weight-bold); color: var(--color-on-surface); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
      .accordion-action { font-size: var(--font-size-caption); color: var(--color-on-surface-secondary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
      .accordion-header-center { display: flex; align-items: center; gap: var(--space-5); flex-shrink: 0; }
      .mini-compliance { display: flex; align-items: center; gap: var(--space-2); }
      .mini-compliance-bar { width: 80px; height: 6px; background: var(--color-surface-container-high); border-radius: var(--radius-full); overflow: hidden; }
      .mini-compliance-fill { height: 100%; border-radius: var(--radius-full); transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1); }
      .mini-compliance-pct { font-size: var(--font-size-caption); font-weight: var(--font-weight-extrabold); min-width: 32px; text-align: right; }
      .mini-months { display: flex; align-items: center; gap: 4px; font-size: var(--font-size-caption); color: var(--color-on-surface-secondary); font-weight: var(--font-weight-semibold); }
      .mini-months-icon { font-size: 1rem; width: 1rem; height: 1rem; color: var(--color-primary-500); }
      .accordion-chevron { color: var(--color-on-surface-secondary); transition: transform var(--transition-base); flex-shrink: 0; }
      .accordion-chevron--open { transform: rotate(180deg); }
      .accordion-body { padding: 0 var(--space-5) var(--space-5); border-top: 1px solid var(--color-border-light); }

      .record-summary-row { display: flex; gap: var(--space-3); padding: var(--space-4) 0; flex-wrap: wrap; }
      .summary-pill { display: flex; flex-direction: column; gap: 2px; padding: var(--space-2) var(--space-4); border-radius: var(--radius-lg); border: 1px solid var(--color-border-light); background: var(--color-surface-container-low); min-width: 100px; }
      .summary-pill--status { flex-direction: row; align-items: center; gap: var(--space-2); }
      .summary-pill-label { font-size: var(--font-size-label); color: var(--color-on-surface-secondary); font-weight: var(--font-weight-semibold); text-transform: uppercase; letter-spacing: var(--letter-spacing-label); }
      .summary-pill-value { font-size: var(--font-size-h5); font-weight: var(--font-weight-extrabold); color: var(--color-on-surface); }
      .status-dot { width: 8px; height: 8px; border-radius: var(--radius-full); flex-shrink: 0; }

      .month-timeline { display: grid; grid-template-columns: repeat(12, 1fr); gap: var(--space-1); padding: var(--space-3) 0; }
      .timeline-cell { display: flex; flex-direction: column; align-items: center; gap: 2px; padding: var(--space-2) var(--space-1); border-radius: var(--radius-md); background: var(--color-surface-container-low); border: 1px solid transparent; position: relative; transition: all var(--transition-fast); cursor: default; }
      .timeline-cell--filled { background: var(--color-primary-50); border-color: var(--color-primary-200); }
      .timeline-cell--has-detail::after { content: ''; position: absolute; top: 4px; right: 4px; width: 6px; height: 6px; border-radius: var(--radius-full); background: var(--color-primary-500); }
      .timeline-month { font-size: 0.6875rem; font-weight: var(--font-weight-bold); color: var(--color-on-surface-secondary); text-transform: uppercase; letter-spacing: 0.02em; }
      .timeline-value { font-size: var(--font-size-body-sm); font-weight: var(--font-weight-extrabold); color: var(--color-on-surface); }
      .timeline-cell:not(.timeline-cell--filled) .timeline-value { color: var(--color-on-surface-secondary); font-weight: var(--font-weight-regular); }
      .timeline-note-icon { font-size: 0.75rem; width: 0.75rem; height: 0.75rem; color: var(--color-primary-500); position: absolute; bottom: 4px; right: 4px; }

      .record-notes { display: flex; flex-direction: column; gap: 2px; margin-top: var(--space-3); padding: var(--space-3) var(--space-4); background: var(--color-surface-container-low); border-radius: var(--radius-lg); }
      .record-notes p { margin: 0; font-size: var(--font-size-caption); color: var(--color-on-surface-variant); line-height: 1.5; }
      .record-notes strong { color: var(--color-on-surface); font-weight: var(--font-weight-bold); }

      .accordion-actions { display: flex; justify-content: flex-end; padding: var(--space-3) 0 0; }

      .edit-form { display: flex; flex-direction: column; gap: var(--space-4); margin-top: var(--space-4); padding-top: var(--space-4); border-top: 2px solid var(--color-primary-100); }
      .months-editor-summary { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--space-3); padding: var(--space-4); background: var(--color-surface-container-low); border-radius: var(--radius-lg); }
      .summary-stat { display: flex; flex-direction: column; gap: 2px; text-align: center; }
      .summary-num { font-size: 1.25rem; font-weight: var(--font-weight-extrabold); color: var(--color-on-surface); }
      .summary-lbl { font-size: var(--font-size-label); color: var(--color-on-surface-secondary); font-weight: var(--font-weight-semibold); text-transform: uppercase; letter-spacing: var(--letter-spacing-label); }
      .form-section-label { font-size: var(--font-size-label); font-weight: var(--font-weight-bold); color: var(--color-primary-700); text-transform: uppercase; letter-spacing: var(--letter-spacing-label); margin: var(--space-2) 0; }

      .month-grid { display: flex; flex-direction: column; gap: var(--space-4); }
      .quarter-group { display: flex; flex-direction: column; gap: var(--space-2); }
      .quarter-label { font-size: var(--font-size-label); font-weight: var(--font-weight-extrabold); color: var(--color-primary-600); text-transform: uppercase; letter-spacing: 0.06em; }
      .quarter-months { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--space-3); }
      .month-cell { display: flex; flex-direction: column; gap: var(--space-2); padding: var(--space-3); border-radius: var(--radius-lg); border: 1px solid var(--color-border-light); background: #ffffff; transition: border-color var(--transition-fast), background-color var(--transition-fast); }
      .month-cell--filled { border-color: var(--color-primary-200); background: var(--color-primary-50); }
      .month-cell-header { display: flex; align-items: center; justify-content: space-between; }
      .month-cell-name { font-size: var(--font-size-body-sm); font-weight: var(--font-weight-bold); color: var(--color-on-surface); }
      .month-cell-check { display: flex; align-items: center; }
      .month-cell-check mat-icon { font-size: 1rem; width: 1rem; height: 1rem; color: var(--color-success); }
      .month-cell-number { width: 100%; }
      .month-cell-detail { width: 100%; }
      .edit-form-footer { display: flex; flex-direction: column; gap: var(--space-4); }
      .edit-form-actions { display: flex; justify-content: flex-end; gap: var(--space-3); padding-top: var(--space-2); }
      .form-full { width: 100%; }

      .detail-list { display: flex; flex-direction: column; gap: var(--space-4); padding: var(--space-5) 0; }
      .detail-card { border: 1px solid var(--color-border-light); border-radius: var(--radius-xl); background: #ffffff; overflow: hidden; transition: box-shadow var(--transition-fast); }
      .detail-card:hover { box-shadow: var(--shadow-sm); }
      .detail-card--editing { border-color: var(--color-primary-300); box-shadow: var(--shadow-md); }
      .detail-card-header { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-4) var(--space-5); background: var(--color-surface-container-low); border-bottom: 1px solid var(--color-border-light); }
      .detail-badge { display: inline-flex; align-items: center; justify-content: center; width: 32px; height: 32px; border-radius: var(--radius-full); background: var(--color-primary-100); color: var(--color-primary-700); font-size: var(--font-size-caption); font-weight: var(--font-weight-extrabold); flex-shrink: 0; }
      .detail-card-info { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
      .detail-card-title { font-size: var(--font-size-body); font-weight: var(--font-weight-bold); color: var(--color-on-surface); }
      .detail-card-subtitle { font-size: var(--font-size-caption); color: var(--color-on-surface-secondary); }
      .detail-card-body { padding: var(--space-5); display: flex; flex-direction: column; gap: var(--space-4); }
      .detail-description-box { display: flex; gap: var(--space-3); padding: var(--space-4); background: var(--color-surface-container-low); border-radius: var(--radius-lg); border-left: 3px solid var(--color-primary-300); }
      .detail-desc-icon { color: var(--color-primary-500); flex-shrink: 0; margin-top: 2px; }
      .detail-desc { margin: 0; font-size: var(--font-size-body-sm); color: var(--color-on-surface-secondary); line-height: 1.6; }
      .detail-value-row { display: flex; align-items: baseline; gap: var(--space-3); padding: var(--space-3) var(--space-4); background: var(--color-surface-container-low); border-radius: var(--radius-lg); }
      .detail-label { font-size: var(--font-size-caption); color: var(--color-on-surface-variant); font-weight: var(--font-weight-semibold); }
      .detail-num { font-size: 1.5rem; font-weight: var(--font-weight-extrabold); color: var(--color-primary-900); }

      .detail-months-section { display: flex; flex-direction: column; gap: var(--space-2); }
      .detail-months-label { font-size: var(--font-size-label); font-weight: var(--font-weight-bold); color: var(--color-on-surface-secondary); text-transform: uppercase; letter-spacing: var(--letter-spacing-label); }
      .detail-months-grid { display: flex; flex-wrap: wrap; gap: var(--space-1); }
      .detail-month-chip { display: inline-flex; align-items: center; justify-content: center; min-width: 40px; padding: var(--space-1) var(--space-2); border-radius: var(--radius-md); font-size: var(--font-size-caption); font-weight: var(--font-weight-semibold); background: var(--color-surface-container-high); color: var(--color-on-surface-secondary); border: 1px solid transparent; transition: all var(--transition-fast); }
      .detail-month-chip--active { background: var(--color-primary-50); color: var(--color-primary-700); border-color: var(--color-primary-200); font-weight: var(--font-weight-bold); }

      .month-toggle-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--space-2); }
      .month-toggle { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); padding: var(--space-2) var(--space-3); border-radius: var(--radius-lg); border: 1px solid var(--color-border-light); background: #ffffff; cursor: pointer; transition: all var(--transition-fast); font-family: inherit; }
      .month-toggle:hover { border-color: var(--color-primary-300); background: var(--color-primary-50); }
      .month-toggle--active { border-color: var(--color-primary-400); background: var(--color-primary-100); }
      .month-toggle-name { font-size: var(--font-size-body-sm); font-weight: var(--font-weight-semibold); color: var(--color-on-surface); }
      .month-toggle--active .month-toggle-name { color: var(--color-primary-800); }
      .month-toggle-check { font-size: 1rem; width: 1rem; height: 1rem; color: var(--color-primary-700); }
      .month-toggle-count { font-size: var(--font-size-caption); color: var(--color-on-surface-secondary); font-weight: var(--font-weight-semibold); }

      .ind-empty { display: flex; flex-direction: column; align-items: center; gap: var(--space-4); padding: var(--space-12) var(--space-6); text-align: center; }
      .ind-empty mat-icon { font-size: 3.5rem; width: 3.5rem; height: 3.5rem; color: var(--color-primary-200); }
      .ind-empty p { color: var(--color-on-surface-secondary); margin: 0; font-size: var(--font-size-body-sm); line-height: 1.6; }
      .ind-loading { display: flex; flex-direction: column; gap: var(--space-3); padding: var(--space-6) 0; }
      .ind-loading p { text-align: center; color: var(--color-on-surface-secondary); margin: 0; }

      @media (max-width: 1024px) {
        .kpi-row { grid-template-columns: repeat(2, 1fr); }
        .quarter-months { grid-template-columns: repeat(2, 1fr); }
        .month-timeline { grid-template-columns: repeat(6, 1fr); }
        .month-toggle-grid { grid-template-columns: repeat(3, 1fr); }
      }
      @media (max-width: 768px) {
        .filter-bar { padding: var(--space-3) var(--space-4); }
        .filter-bar-inner { flex-direction: column; align-items: flex-start; gap: var(--space-3); }
        .filter-buttons { width: 100%; }
        .filter-btn { flex: 1 1 auto; min-width: 0; }
        .filter-btn-text { max-width: 120px; }
        .accordion-header { flex-wrap: wrap; gap: var(--space-2); }
        .accordion-header-center { width: 100%; justify-content: flex-start; padding-left: 44px; }
        .quarter-months { grid-template-columns: 1fr; }
        .month-timeline { grid-template-columns: repeat(4, 1fr); }
        .month-toggle-grid { grid-template-columns: repeat(2, 1fr); }
        .record-summary-row { flex-direction: column; }
        .record-summary-row { flex-direction: column; }
      }
      @media (max-width: 480px) {
        .kpi-row { grid-template-columns: 1fr 1fr; }
        .months-editor-summary { grid-template-columns: 1fr; }
        .month-timeline { grid-template-columns: repeat(3, 1fr); }
        .month-toggle-grid { grid-template-columns: 1fr 1fr; }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IndicatorsHomeComponent {
  private readonly indicatorsApiService = inject(IndicatorsApiService);
  private readonly catalogsApiService = inject(CatalogsApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);

  readonly records = signal<IndicatorRecord[]>([]);
  readonly details = signal<IndicatorDetailRecord[]>([]);
  readonly departments = signal<DepartmentCatalogOption[]>([]);
  readonly monthOptions = signal<CatalogOption[]>([]);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly activeTab = signal(0);

  readonly editRecordId = signal<string | null>(null);
  readonly editDetailId = signal<string | null>(null);
  readonly expandedRecordId = signal<string | null>(null);
  readonly filterDropdown = signal<'dept' | 'year' | null>(null);

  readonly editForm = this.formBuilder.group({
    source: [''],
    generalObservations: ['']
  });

  readonly editDetailForm = this.formBuilder.group({
    source: [''],
    observations: [''],
    selectedMonthIds: [[] as number[]]
  });

  readonly filterDepartment = this.formBuilder.control<number | null>(null);
  readonly filterYear = this.formBuilder.control<number | null>(null);

  readonly provisionDepartment = this.formBuilder.control<number | null>(null);
  readonly provisionYear = this.formBuilder.control<number | null>(null);
  readonly yearOptions = signal<CatalogOption[]>([]);

  readonly monthRows = signal<IndicatorMonthlyProgress[]>([]);
  private editingTargetValue = 0;

  readonly monthsFilledCount = computed(
    () => this.monthRows().filter((r) => r.quantitativeAdvance !== null && r.quantitativeAdvance !== undefined).length
  );

  readonly currentValuePreview = computed(() => {
    const advances = this.monthRows()
      .map((r) => r.quantitativeAdvance)
      .filter((v): v is number => v !== null && v !== undefined);
    if (advances.length === 0) return 0;
    return this.editingTargetValue <= 1 ? Math.max(...advances) : advances.reduce((a, b) => a + b, 0);
  });

  readonly compliancePreview = computed(() =>
    this.editingTargetValue === 0 ? 0 : this.currentValuePreview() / this.editingTargetValue
  );

  readonly quarterGroups = computed((): IndicatorMonthlyProgress[][] => {
    const rows = this.monthRows();
    const groups: IndicatorMonthlyProgress[][] = [];
    for (let i = 0; i < rows.length; i += 3) {
      groups.push(rows.slice(i, i + 3));
    }
    return groups;
  });

  readonly availableYears = computed(() => {
    const years = new Set<number>();
    for (const r of this.records()) years.add(r.year);
    for (const d of this.details()) years.add(d.year);
    return Array.from(years).sort((a, b) => b - a);
  });

  readonly filteredRecords = computed(() => {
    return this.records().filter((r) => {
      const dept = this.filterDepartment.value;
      const year = this.filterYear.value;
      return (!dept || r.departmentId === dept) && (!year || r.year === year);
    });
  });

  readonly filteredDetails = computed(() => {
    return this.details().filter((d) => {
      const dept = this.filterDepartment.value;
      const year = this.filterYear.value;
      return (!dept || d.departmentId === dept) && (!year || d.year === year);
    });
  });

  readonly onTrackCount = computed(() =>
    this.filteredRecords().filter((r) => r.compliancePercentageCalculated >= 0.8).length
  );

  readonly atRiskCount = computed(() =>
    this.filteredRecords().filter(
      (r) => r.compliancePercentageCalculated >= 0.5 && r.compliancePercentageCalculated < 0.8
    ).length
  );

  readonly behindCount = computed(() =>
    this.filteredRecords().filter((r) => r.compliancePercentageCalculated < 0.5).length
  );

  constructor() {
    this.loadAll();
  }

  complianceColor(pct: number): string {
    return complianceColor(pct);
  }

  complianceBg(pct: number): string {
    return complianceBg(pct);
  }

  complianceLabel(pct: number): string {
    return complianceLabel(pct);
  }

  toggleRecord(id: string): void {
    if (this.editRecordId() === id) return;
    this.expandedRecordId.set(this.expandedRecordId() === id ? null : id);
  }

  toggleFilterDropdown(type: 'dept' | 'year'): void {
    this.filterDropdown.set(this.filterDropdown() === type ? null : type);
  }

  setDepartmentFilter(id: number | null): void {
    this.filterDepartment.setValue(id);
    this.filterDropdown.set(null);
  }

  setYearFilter(year: number | null): void {
    this.filterYear.setValue(year);
    this.filterDropdown.set(null);
  }

  globalMonthIndex(quarterIndex: number, indexInQuarter: number): number {
    return quarterIndex * 3 + indexInQuarter;
  }

  tooltipForProgress(progress: IndicatorMonthlyProgress): string {
    const parts: string[] = [progress.monthName];
    if (progress.quantitativeAdvance !== null) {
      parts.push(`Avance: ${progress.quantitativeAdvance}`);
    }
    if (progress.detail) {
      parts.push(progress.detail);
    }
    return parts.join(' — ');
  }

  isMonthSelected(monthId: number): boolean {
    const selected = this.editDetailForm.value.selectedMonthIds ?? [];
    return selected.includes(monthId);
  }

  toggleMonth(monthId: number): void {
    const current = this.editDetailForm.value.selectedMonthIds ?? [];
    const idx = current.indexOf(monthId);
    if (idx >= 0) {
      this.editDetailForm.patchValue({ selectedMonthIds: current.filter((id) => id !== monthId) });
    } else {
      this.editDetailForm.patchValue({ selectedMonthIds: [...current, monthId].sort((a, b) => a - b) });
    }
  }

  clearFilters(): void {
    this.filterDepartment.setValue(null);
    this.filterYear.setValue(null);
  }

  activeFilterCount(): number {
    return (this.filterDepartment.value ? 1 : 0) + (this.filterYear.value ? 1 : 0);
  }

  selectedDepartmentName(): string {
    const id = this.filterDepartment.value;
    return this.departments().find((d) => d.id === id)?.name ?? '';
  }

  editRecord(record: IndicatorRecord): void {
    this.editRecordId.set(record.id);
    this.editDetailId.set(null);
    this.expandedRecordId.set(record.id);
    this.editingTargetValue = record.targetValue;
    this.editForm.patchValue({
      source: record.source,
      generalObservations: record.generalObservations
    });
    this.monthRows.set(record.monthlyProgresses.map((m) => ({ ...m })));
  }

  editDetail(detail: IndicatorDetailRecord): void {
    this.editDetailId.set(detail.id);
    this.editRecordId.set(null);
    this.editDetailForm.patchValue({
      source: detail.source,
      observations: detail.observations,
      selectedMonthIds: detail.selectedMonthIds
    });
  }

  cancelEdit(): void {
    this.editRecordId.set(null);
    this.editDetailId.set(null);
  }

  saveRecord(id: string): void {
    this.saving.set(true);
    this.indicatorsApiService
      .updateRecord(id, {
        source: this.editForm.value.source ?? null,
        generalObservations: this.editForm.value.generalObservations ?? null,
        monthlyProgresses: this.monthRows().map((r) => ({
          quantitativeAdvance: r.quantitativeAdvance,
          detail: r.detail?.trim() ? r.detail.trim() : null
        }))
      })
      .subscribe({
        next: (record) => {
          this.records.update((arr) => arr.map((r) => (r.id === record.id ? record : r)));
          this.saving.set(false);
          this.cancelEdit();
          this.snackBar.open('Indicador actualizado', 'Cerrar', { duration: 3000 });
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.snackBar.open(extractErrorMessage(err, 'Error al guardar'), 'Cerrar', { duration: 5000 });
        }
      });
  }

  saveDetail(id: string): void {
    this.saving.set(true);
    this.indicatorsApiService
      .updateDetail(id, {
        source: this.editDetailForm.value.source ?? null,
        observations: this.editDetailForm.value.observations ?? null,
        selectedMonthIds: this.editDetailForm.value.selectedMonthIds ?? []
      })
      .subscribe({
        next: (detail) => {
          this.details.update((arr) => arr.map((d) => (d.id === detail.id ? detail : d)));
          this.saving.set(false);
          this.cancelEdit();
          this.snackBar.open('Detalle actualizado', 'Cerrar', { duration: 3000 });
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.snackBar.open(extractErrorMessage(err, 'Error al guardar'), 'Cerrar', { duration: 5000 });
        }
      });
  }

  monthNameById(id: number): string {
    return this.monthOptions().find((m) => m.id === id)?.name ?? '';
  }

  startWorksheet(): void {
    const departmentId = this.provisionDepartment.value;
    const year = this.provisionYear.value;
    if (!departmentId || !year) {
      return;
    }

    this.saving.set(true);
    this.indicatorsApiService.provisionWorksheet(departmentId, year).subscribe({
      next: (worksheet) => {
        this.mergeWorksheet(worksheet);
        this.filterDepartment.setValue(departmentId);
        this.filterYear.setValue(year);
        this.activeTab.set(0);
        this.saving.set(false);
        this.snackBar.open('Hoja de indicadores lista para diligenciar', 'Cerrar', { duration: 3000 });
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.snackBar.open(extractErrorMessage(err, 'Error al preparar la hoja de indicadores'), 'Cerrar', { duration: 5000 });
      }
    });
  }

  private mergeWorksheet(worksheet: IndicatorWorksheet): void {
    const recordIds = new Set(worksheet.records.map((r) => r.id));
    this.records.update((arr) => [...arr.filter((r) => !recordIds.has(r.id)), ...worksheet.records]);

    const detailIds = new Set(worksheet.details.map((d) => d.id));
    this.details.update((arr) => [...arr.filter((d) => !detailIds.has(d.id)), ...worksheet.details]);

    this.loading.set(false);
  }

  private loadAll(): void {
    this.indicatorsApiService.getRecords().subscribe({
      next: (data) => this.records.set(data),
      complete: () => this.checkLoading()
    });

    this.indicatorsApiService.getDetails().subscribe({
      next: (data) => this.details.set(data),
      complete: () => this.checkLoading()
    });

    this.catalogsApiService.getLookup('months').subscribe({
      next: (data) => this.monthOptions.set(data)
    });

    this.catalogsApiService.getDepartments().subscribe({
      next: (data) => this.departments.set(data)
    });

    this.catalogsApiService.getLookup('years').subscribe({
      next: (data) => this.yearOptions.set(data)
    });
  }

  private checkLoading(): void {
    if (this.records().length > 0 || this.details().length > 0) {
      this.loading.set(false);
    }
    setTimeout(() => this.loading.set(false), 2000);
  }

  onMonthQuantitativeInput(index: number, event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const value = raw === '' ? null : Number(raw);
    this.monthRows.update((rows) => rows.map((r, i) => (i === index ? { ...r, quantitativeAdvance: value } : r)));
  }

  onMonthDetailInput(index: number, event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.monthRows.update((rows) => rows.map((r, i) => (i === index ? { ...r, detail: value } : r)));
  }

  filledMonths(record: IndicatorRecord): IndicatorMonthlyProgress[] {
    return record.monthlyProgresses.filter((p) => p.quantitativeAdvance !== null || !!p.detail);
  }

  completedMonthsCount(record: IndicatorRecord): number {
    return record.monthlyProgresses.filter((p) => p.quantitativeAdvance !== null).length;
  }
}
