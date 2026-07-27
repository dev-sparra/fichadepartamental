import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { MatTooltipModule } from '@angular/material/tooltip';

import { CatalogsApiService } from '../administration/services/catalogs-api.service';
import { GovernanceApiService } from '../../core/services/governance-api.service';
import { WorkflowApiService } from '../../core/services/workflow-api.service';
import { ExportsApiService } from '../../core/services/exports-api.service';
import { AuthTokenService } from '../../core/services/auth-token.service';
import { AppRoles } from '../../core/constants/app-roles.constant';
import { ConfirmDialogService } from '../../shared/services/confirm-dialog.service';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';
import { formatCop, parseIsoDate, toIsoDate } from '../../shared/utils/date-format.util';
import {
  MOBILE_PHONE_DIGITS,
  copAmountValidator,
  dateRangeValidator,
  dependentPairValidator,
  describeFieldError,
  emailFormatValidator,
  isRowFilled,
  mobilePhoneValidator,
  requiredTextValidator,
  requiredWhenRowFilledValidator
} from '../../shared/validators/portal-validators';
import {
  CatalogOption,
  DepartmentCatalogOption,
  EcosystemRoleCatalogOption,
  MunicipalityCatalogOption,
  PnmcComponentCatalogOption
} from '../../shared/models/catalog.models';
import {
  GovernanceActor,
  GovernanceDiagnostic,
  GovernanceFichaDetail,
  GovernanceFichaSummary,
  GovernanceOpportunity,
  GovernancePnmcAxis
} from '../../shared/models/governance.models';
import { ApprovalRecord, FichaApprovalStatus } from '../../shared/models/workflow.models';

/** Etiqueta de cada campo, igual a la de la Ficha Departamental, para nombrarlo en los errores. */
const FIELD_LABELS: Record<string, string> = {
  fechaLevantamiento: 'Fecha de levantamiento',
  departmentId: 'Departamento',
  municipalityId: 'Ciudad',
  responsableRegistro: 'Responsable del registro (Gestor)',
  regionOcadOptionId: 'Región OCAD',
  informationSourceIds: 'Fuente de información',
  observaciones: 'Observaciones',
  nombreAgente: 'Nombre del agente (creyente)',
  agentTypeId: 'Tipo de agente (categoría)',
  ecosystemRoleIds: 'Rol en el ecosistema',
  territorialLevelOptionIds: 'Nivel territorial',
  numeroContacto: 'Número de contacto',
  correoElectronico: 'Correo electrónico',
  descripcionHallazgo: 'Descripción hallazgo',
  pnmcAxisId: 'Eje PNMC',
  pnmcComponentId: 'Componente PNMC',
  valorPropuestaCop: 'Valor de la propuesta (COP)',
  situacionIdentificada: 'Situación identificada',
  priorityLevelOptionId: 'Nivel de impacto'
};

interface FormAlert {
  title: string;
  items: { label: string; message: string }[];
}

@Component({
  selector: 'app-governance-home',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSnackBarModule,
    MatStepperModule,
    MatTooltipModule
  ],
  template: `
    <section class="governance-page">
      <header class="gov-header">
        <div>
          <h1 class="gov-title">Gobernanza</h1>
          <p class="gov-subtitle">Fichas departamentales del ecosistema musical</p>
        </div>
        @if (saving()) {
          <mat-progress-bar mode="indeterminate" class="gov-progress" />
        }
      </header>

      <mat-card class="gov-fichas-card">
        <mat-card-header>
          <mat-card-title>Fichas registradas</mat-card-title>
          <button mat-flat-button color="primary" type="button" (click)="startCreate()" [disabled]="isDraft() || saving() || !canEdit()">
            <mat-icon>add</mat-icon>
            Nueva ficha
          </button>
        </mat-card-header>
        <mat-divider />
        <mat-card-content>
          @if (fichas().length > 0) {
            <div class="fichas-grid">
              @for (ficha of fichas(); track ficha.id) {
                <button
                  class="ficha-card"
                  [class.ficha-card--active]="selectedFicha()?.id === ficha.id"
                  (click)="selectFicha(ficha)"
                  [attr.aria-label]="'Seleccionar ficha de ' + ficha.departmentName"
                >
                  <div class="ficha-card-main">
                    <mat-icon class="ficha-icon">map</mat-icon>
                    <div class="ficha-info">
                      <strong class="ficha-dept">{{ ficha.departmentName }}</strong>
                      <span class="ficha-date">{{ ficha.fechaLevantamiento | date: 'mediumDate' }}</span>
                      <span class="ficha-resp">{{ ficha.responsableRegistro }}</span>
                    </div>
                    <span
                      class="ficha-status"
                      [style.--status-color]="statusColor(ficha.approvalStatus)"
                      [matTooltip]="
                        ficha.approvalStatus === 'Borrador'
                          ? 'Ficha en borrador: aún no la ha revisado el Líder de Gobernanza'
                          : 'Estado de revisión de la ficha'
                      "
                    >
                      {{ statusLabel(ficha.approvalStatus) }}
                    </span>
                  </div>
                  <div class="ficha-stats">
                    <span class="ficha-stat">
                      <mat-icon>lightbulb</mat-icon>{{ ficha.opportunitiesCount }}
                    </span>
                    <span class="ficha-stat">
                      <mat-icon>layers</mat-icon>{{ ficha.axesCount }}
                    </span>
                    <span class="ficha-stat">
                      <mat-icon>people</mat-icon>{{ ficha.actorsCount }}
                    </span>
                  </div>
                </button>
              }
            </div>
          } @else if (!isDraft()) {
            <div class="gov-empty">
              <mat-icon>description_off</mat-icon>
              <p>No hay fichas registradas. Crea una nueva o importa datos desde el módulo de importaciones.</p>
              <button mat-flat-button color="primary" type="button" (click)="startCreate()">Crear ficha</button>
            </div>
          }
        </mat-card-content>
      </mat-card>

      @if (isDraft() || selectedFicha()) {
        <mat-card class="gov-form-card">
          <mat-card-header>
            <mat-card-title>
              @if (selectedFicha(); as ficha) {
                {{ ficha.departmentName }}
                <span class="gov-ficha-badge">{{ ficha.fechaLevantamiento | date: 'mediumDate' }}</span>
              } @else {
                Nueva ficha departamental
                <span class="gov-ficha-badge gov-ficha-badge--draft">Borrador</span>
              }
            </mat-card-title>
          </mat-card-header>
          <mat-divider />

          @if (selectedFicha(); as ficha) {
            <div class="gov-workflow-bar">
              <div class="workflow-status">
                <span class="workflow-badge" [style.--wf-color]="workflowStatusColor()">
                  {{ workflowStatusLabel() }}
                </span>
                @if (approvalStatus()?.reviewedByName) {
                  <span class="workflow-meta">
                    Revisado por {{ approvalStatus()?.reviewedByName }}
                    @if (approvalStatus()?.reviewedAtUtc) {
                      &middot; {{ approvalStatus()?.reviewedAtUtc | date: 'mediumDate' }}
                    }
                  </span>
                }
              </div>
              <div class="workflow-actions">
                <button mat-stroked-button type="button" (click)="toggleHistory()">
                  <mat-icon>history</mat-icon>
                  Historial
                </button>
                <button mat-stroked-button type="button" (click)="downloadExcel(ficha)">
                  <mat-icon>file_download</mat-icon>
                  Descargar Excel
                </button>
                @if (canReview()) {
                  <button mat-stroked-button type="button" class="workflow-reject-btn" [disabled]="workflowSaving()" (click)="rejectFicha(ficha)">
                    <mat-icon>close</mat-icon>
                    Rechazar
                  </button>
                  <button mat-flat-button color="primary" type="button" [disabled]="workflowSaving()" (click)="approveFicha(ficha)">
                    <mat-icon>check</mat-icon>
                    Aprobar
                  </button>
                }
                @if (canDelete()) {
                  <button mat-stroked-button type="button" class="workflow-delete-btn" [disabled]="workflowSaving()" (click)="deleteFicha(ficha)">
                    <mat-icon>delete_forever</mat-icon>
                    Eliminar ficha
                  </button>
                }
              </div>
            </div>

            @if (historyOpen()) {
              <div class="workflow-history">
                @if (approvalHistory().length > 0) {
                  @for (record of approvalHistory(); track record.id) {
                    <div class="history-item">
                      <mat-icon class="history-icon">assignment_turned_in</mat-icon>
                      <div class="history-info">
                        <strong>{{ record.actorName }}</strong>
                        <span>{{ record.action }} &middot; {{ record.timestampUtc | date: 'medium' }}</span>
                        @if (record.comment) {
                          <p>{{ record.comment }}</p>
                        }
                      </div>
                    </div>
                  }
                } @else {
                  <p class="history-empty">Sin historial de aprobación registrado para esta ficha.</p>
                }
              </div>
            }
          }

          <mat-card-content class="gov-stepper-wrapper">
            @if (selectedFicha() && !canEdit()) {
              <div class="gov-readonly-banner">
                <mat-icon>visibility</mat-icon>
                <p>Modo solo lectura: tu rol permite revisar y aprobar, pero no editar el diligenciamiento de la ficha.</p>
              </div>
            }

            @if (formAlert(); as alert) {
              <div class="gov-form-alert" role="alert">
                <mat-icon class="gov-form-alert-icon">error_outline</mat-icon>
                <div class="gov-form-alert-body">
                  <strong>{{ alert.title }}</strong>
                  @if (alert.items.length > 0) {
                    <ul>
                      @for (item of alert.items; track item.label) {
                        <li><strong>{{ item.label }}:</strong> {{ item.message }}</li>
                      }
                    </ul>
                  }
                </div>
                <button
                  mat-icon-button
                  type="button"
                  class="gov-form-alert-close"
                  aria-label="Cerrar el aviso"
                  (click)="formAlert.set(null)"
                >
                  <mat-icon>close</mat-icon>
                </button>
              </div>
            }
            <mat-stepper orientation="vertical" [linear]="false">
              <!-- Step 1: Identification -->
              <mat-step [stepControl]="identificationForm" label="Identificación">
                <form [formGroup]="identificationForm" (ngSubmit)="onIdentificationSubmit()" class="gov-form" novalidate>
                  <div class="form-grid form-grid--2col">
                    <mat-form-field appearance="outline">
                      <mat-label>Fecha de levantamiento</mat-label>
                      <input
                        matInput
                        [matDatepicker]="fechaLevantamientoPicker"
                        [min]="minCaptureDate"
                        [max]="maxCaptureDate"
                        formControlName="fechaLevantamiento"
                        placeholder="dd/mm/aaaa"
                        required
                      />
                      <mat-datepicker-toggle matIconSuffix [for]="fechaLevantamientoPicker" />
                      <mat-datepicker #fechaLevantamientoPicker />
                      <mat-hint>Escríbela como dd/mm/aaaa o elígela en el calendario</mat-hint>
                      @if (fieldError(identificationForm.controls.fechaLevantamiento, 'La fecha de levantamiento'); as error) {
                        <mat-error>{{ error }}</mat-error>
                      }
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>Responsable del registro (Gestor)</mat-label>
                      <input matInput type="text" maxlength="200" formControlName="responsableRegistro" required />
                      <mat-hint>Persona que diligencia la ficha</mat-hint>
                      @if (fieldError(identificationForm.controls.responsableRegistro, 'El responsable del registro'); as error) {
                        <mat-error>{{ error }}</mat-error>
                      }
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>Departamento</mat-label>
                      <mat-select formControlName="departmentId" required (selectionChange)="loadMunicipalities()">
                        @for (dept of departments(); track dept.id) {
                          <mat-option [value]="dept.id">{{ dept.name }}</mat-option>
                        }
                      </mat-select>
                      @if (fieldError(identificationForm.controls.departmentId, 'El departamento'); as error) {
                        <mat-error>{{ error }}</mat-error>
                      }
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>Ciudad</mat-label>
                      <mat-select formControlName="municipalityId">
                        <mat-option [value]="null">Sin municipio</mat-option>
                        @for (municipality of municipalities(); track municipality.id) {
                          <mat-option [value]="municipality.id">{{ municipality.name }}</mat-option>
                        }
                      </mat-select>
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>Región OCAD</mat-label>
                      <mat-select formControlName="regionOcadOptionId">
                        <mat-option [value]="null">Sin región</mat-option>
                        @for (option of regionOcadOptions(); track option.id) {
                          <mat-option [value]="option.id">{{ option.name }}</mat-option>
                        }
                      </mat-select>
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>Fuente de información</mat-label>
                      <mat-select formControlName="informationSourceIds" multiple>
                        @for (option of informationSourceOptions(); track option.id) {
                          <mat-option [value]="option.id">{{ option.name }}</mat-option>
                        }
                      </mat-select>
                    </mat-form-field>

                    <mat-form-field appearance="outline" class="form-full">
                      <mat-label>Observaciones</mat-label>
                      <textarea matInput formControlName="observaciones" rows="3"></textarea>
                    </mat-form-field>
                  </div>

                  <div class="step-actions">
                    @if (isDraft()) {
                      <button mat-button type="button" (click)="cancelDraft()">Cancelar</button>
                    }
                    <!-- El botón permanece habilitado: al pulsarlo se indica exactamente qué campo
                         falta, en lugar de dejar al usuario sin saber por qué no puede continuar. -->
                    <button mat-flat-button color="primary" type="submit" [disabled]="saving() || !canEdit()">
                      <mat-icon>save</mat-icon>
                      {{ isDraft() ? 'Crear ficha' : 'Guardar identificación' }}
                    </button>
                    @if (!isDraft()) {
                      <button mat-stroked-button type="button" matStepperNext>Continuar</button>
                    }
                  </div>
                </form>
              </mat-step>

              <!-- Step 2: Diagnostic -->
              <mat-step [stepControl]="diagnosticForm" label="Diagnóstico del ecosistema">
                @if (isDraft()) {
                  <div class="step-locked">
                    <mat-icon>lock</mat-icon>
                    <p>Primero crea la ficha en el paso de Identificación para habilitar este paso.</p>
                  </div>
                } @else {
                  <form [formGroup]="diagnosticForm" (ngSubmit)="saveDiagnostic()" class="gov-form" novalidate>
                    <div class="form-grid form-grid--2col">
                      <mat-form-field appearance="outline" class="form-full">
                        <mat-label>Caracterización general del ecosistema musical</mat-label>
                        <textarea matInput formControlName="caracterizacionGeneral" rows="3"></textarea>
                      </mat-form-field>

                      <mat-form-field appearance="outline" class="form-full">
                        <mat-label>Fortalezas identificadas</mat-label>
                        <textarea matInput formControlName="fortalezasIdentificadas" rows="3"></textarea>
                      </mat-form-field>

                      <mat-form-field appearance="outline" class="form-full">
                        <mat-label>Debilidades identificadas</mat-label>
                        <textarea matInput formControlName="debilidadesIdentificadas" rows="3"></textarea>
                      </mat-form-field>

                      <mat-form-field appearance="outline" class="form-full">
                        <mat-label>Políticas priorizadas</mat-label>
                        <textarea matInput formControlName="politicasPriorizadas" rows="3"></textarea>
                      </mat-form-field>

                      <mat-form-field appearance="outline" class="form-full">
                        <mat-label>Tensiones o conflictos</mat-label>
                        <textarea matInput formControlName="tensionesOConflictos" rows="2"></textarea>
                      </mat-form-field>
                    </div>

                    <p class="form-section-label">Gobernanza institucional</p>
                    <div class="form-grid form-grid--2col">
                      <mat-form-field appearance="outline">
                        <mat-label>CODEMUS – Comité Dptal de Música</mat-label>
                        <mat-select formControlName="committeeStatusOptionId">
                          <mat-option [value]="null">Sin estado</mat-option>
                          @for (option of committeeStatusOptions(); track option.id) {
                            <mat-option [value]="option.id">{{ option.name }}</mat-option>
                          }
                        </mat-select>
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Consejo Departamental de Cultura</mat-label>
                        <mat-select formControlName="consejoDepartamentalCultura">
                          @for (option of councilCultureOptions; track option) {
                            <mat-option [value]="option">{{ option }}</mat-option>
                          }
                        </mat-select>
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Plan Departamental de Cultura</mat-label>
                        <mat-select formControlName="planDepartamentalCulturaStatusId">
                          <mat-option [value]="null">Sin estado</mat-option>
                          @for (option of planStatusOptions(); track option.id) {
                            <mat-option [value]="option.id">{{ option.name }}</mat-option>
                          }
                        </mat-select>
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Plan Departamental de Música</mat-label>
                        <mat-select formControlName="planDepartamentalMusicaStatusId">
                          <mat-option [value]="null">Sin estado</mat-option>
                          @for (option of planStatusOptions(); track option.id) {
                            <mat-option [value]="option.id">{{ option.name }}</mat-option>
                          }
                        </mat-select>
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Ordenanzas Culturales</mat-label>
                        <mat-select formControlName="ordenanzasCulturales">
                          @for (option of ordinanceOptions; track option) {
                            <mat-option [value]="option">{{ option }}</mat-option>
                          }
                        </mat-select>
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Consejo Departamental de Música</mat-label>
                        <input matInput type="text" maxlength="200" formControlName="consejoDepartamentalMusica" />
                      </mat-form-field>

                      <mat-form-field appearance="outline">
                        <mat-label>Mesa sectorial o territorial identificada</mat-label>
                        <input matInput type="text" maxlength="200" formControlName="mesaSectorialTerritorial" />
                      </mat-form-field>

                      <mat-form-field appearance="outline" class="form-full">
                        <mat-label>Observaciones</mat-label>
                        <textarea matInput formControlName="observaciones" rows="2"></textarea>
                      </mat-form-field>
                    </div>

                    <div class="step-actions">
                      <button mat-stroked-button type="button" matStepperPrevious>Anterior</button>
                      <button mat-flat-button color="primary" type="submit" [disabled]="saving() || !canEdit()">
                        <mat-icon>save</mat-icon>
                        Guardar diagnóstico
                      </button>
                      <button mat-stroked-button type="button" matStepperNext>Continuar</button>
                    </div>
                  </form>
                }
              </mat-step>

              <!-- Step 3: Opportunities -->
              <mat-step label="Oportunidades de cambio">
                @if (isDraft()) {
                  <div class="step-locked">
                    <mat-icon>lock</mat-icon>
                    <p>Primero crea la ficha en el paso de Identificación para habilitar este paso.</p>
                  </div>
                } @else {
                  <div class="gov-form">
                    <div class="array-header">
                      <p class="array-desc">Registra las oportunidades de cambio identificadas en el territorio.</p>
                      <button mat-stroked-button type="button" [disabled]="!canEdit()" (click)="addOpportunity()">
                        <mat-icon>add</mat-icon>
                        Agregar oportunidad
                      </button>
                    </div>

                    <form [formGroup]="opportunitiesForm" (ngSubmit)="saveOpportunities()">
                      <div formArrayName="items" class="array-list">
                        @for (group of opportunityGroups.controls; track $index) {
                          <mat-card class="array-card" [formGroupName]="$index">
                            <mat-card-header>
                              <mat-card-title>Oportunidad {{ $index + 1 }}</mat-card-title>
                              <button
                                mat-icon-button
                                type="button"
                                class="array-remove"
                                [attr.aria-label]="'Eliminar oportunidad ' + ($index + 1)"
                                (click)="removeOpportunity($index)"
                              >
                                <mat-icon>delete_outline</mat-icon>
                              </button>
                            </mat-card-header>
                            <mat-card-content class="form-grid form-grid--2col">
                              <mat-form-field appearance="outline" class="form-full">
                                <mat-label>Situación identificada</mat-label>
                                <textarea matInput formControlName="situacionIdentificada" rows="2"></textarea>
                              </mat-form-field>
                              <mat-form-field appearance="outline" class="form-full">
                                <mat-label>Componente PNMC - Otras dependencias / Entidades</mat-label>
                                <textarea matInput formControlName="componenteOtrasDependenciasEntidades" rows="2"></textarea>
                              </mat-form-field>
                              <mat-form-field appearance="outline" class="form-full">
                                <mat-label>Aliados y creyentes</mat-label>
                                <textarea matInput formControlName="aliadosYCreyentes" rows="2"></textarea>
                              </mat-form-field>
                              <mat-form-field appearance="outline" class="form-full">
                                <mat-label>Territorio de influencia</mat-label>
                                <textarea matInput formControlName="territorioInfluencia" rows="2"></textarea>
                              </mat-form-field>
                              <mat-form-field appearance="outline">
                                <mat-label>Nivel de impacto</mat-label>
                                <mat-select formControlName="priorityLevelOptionId">
                                  <mat-option [value]="null">Sin nivel</mat-option>
                                  @for (option of priorityLevelOptions(); track option.id) {
                                    <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                  }
                                </mat-select>
                              </mat-form-field>
                              <mat-form-field appearance="outline" class="form-full">
                                <mat-label>Descripción adicional</mat-label>
                                <textarea matInput formControlName="descripcionAdicional" rows="2"></textarea>
                              </mat-form-field>
                            </mat-card-content>
                          </mat-card>
                        }
                      </div>

                      <div class="step-actions">
                        <button mat-stroked-button type="button" matStepperPrevious>Anterior</button>
                        <button mat-flat-button color="primary" type="submit" [disabled]="saving() || !canEdit()">
                          <mat-icon>save</mat-icon>
                          Guardar oportunidades
                        </button>
                        <button mat-stroked-button type="button" matStepperNext>Continuar</button>
                      </div>
                    </form>
                  </div>
                }
              </mat-step>

              <!-- Step 4: PNMC Axes -->
              <mat-step label="Ejes PNMC">
                @if (isDraft()) {
                  <div class="step-locked">
                    <mat-icon>lock</mat-icon>
                    <p>Primero crea la ficha en el paso de Identificación para habilitar este paso.</p>
                  </div>
                } @else {
                <div class="gov-form">
                  <div class="array-header">
                    <p class="array-desc">Registra los ejes del Plan Nacional de Música para la Convivencia.</p>
                    <button mat-stroked-button type="button" [disabled]="!canEdit()" (click)="addPnmcAxis()">
                      <mat-icon>add</mat-icon>
                      Agregar eje PNMC
                    </button>
                  </div>

                  <form [formGroup]="pnmcAxesForm" (ngSubmit)="savePnmcAxes()">
                    <div formArrayName="items" class="array-list">
                      @for (group of pnmcAxisGroups.controls; track $index) {
                        <mat-card class="array-card" [formGroupName]="$index">
                          <mat-card-header>
                            <mat-card-title>Eje PNMC {{ $index + 1 }}</mat-card-title>
                            <button
                              mat-icon-button
                              type="button"
                              class="array-remove"
                              [attr.aria-label]="'Eliminar eje PNMC ' + ($index + 1)"
                              (click)="removePnmcAxis($index)"
                            >
                              <mat-icon>delete_outline</mat-icon>
                            </button>
                          </mat-card-header>
                          <mat-card-content class="form-grid form-grid--2col">
                            <mat-form-field appearance="outline" class="form-full">
                              <mat-label>Descripción hallazgo</mat-label>
                              <textarea matInput formControlName="descripcionHallazgo" rows="2"></textarea>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Eje PNMC</mat-label>
                              <mat-select formControlName="pnmcAxisId" (selectionChange)="onPnmcAxisChange($index)">
                                <mat-option [value]="null">Sin eje</mat-option>
                                @for (option of pnmcAxisOptions(); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                              @if (fieldError(group.get('pnmcAxisId'), 'El eje PNMC'); as error) {
                                <mat-error>{{ error }}</mat-error>
                              }
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Componente PNMC</mat-label>
                              <mat-select formControlName="pnmcComponentId">
                                @for (option of componentsFor(group.get('pnmcAxisId')?.value); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                              <mat-hint>
                                @if (group.get('pnmcAxisId')?.value) {
                                  Componentes del eje seleccionado
                                } @else {
                                  Selecciona primero el eje PNMC
                                }
                              </mat-hint>
                              @if (fieldError(group.get('pnmcComponentId'), 'El componente PNMC'); as error) {
                                <mat-error>{{ error }}</mat-error>
                              }
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Acción Estratégica</mat-label>
                              <textarea matInput formControlName="accionEstrategica" rows="2"></textarea>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Política priorizada</mat-label>
                              <textarea matInput formControlName="politicaPriorizada" rows="2"></textarea>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Armonización PNC</mat-label>
                              <textarea matInput formControlName="armonizacionPnc" rows="2"></textarea>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Armonización PND</mat-label>
                              <textarea matInput formControlName="armonizacionPnd" rows="2"></textarea>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Armonización Internacional</mat-label>
                              <textarea matInput formControlName="armonizacionInternacional" rows="2"></textarea>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Nivel prioridad</mat-label>
                              <mat-select formControlName="priorityLevelOptionId">
                                <mat-option [value]="null">Sin nivel</mat-option>
                                @for (option of priorityLevelOptions(); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Aliados / Responsables</mat-label>
                              <textarea matInput formControlName="aliadosResponsables" rows="2"></textarea>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Fuentes de financiación</mat-label>
                              <textarea matInput formControlName="fuentesFinanciacion" rows="2"></textarea>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Valor de la propuesta (COP)</mat-label>
                              <span matTextPrefix>$&nbsp;</span>
                              <input
                                matInput
                                type="number"
                                inputmode="numeric"
                                min="0"
                                step="1000"
                                formControlName="valorPropuestaCop"
                                placeholder="0"
                              />
                              <mat-hint>
                                @if (formatCopValue(group.get('valorPropuestaCop')?.value); as formatted) {
                                  {{ formatted }}
                                } @else {
                                  Valor en pesos colombianos, sin puntos ni comas
                                }
                              </mat-hint>
                              @if (fieldError(group.get('valorPropuestaCop'), 'El valor de la propuesta'); as error) {
                                <mat-error>{{ error }}</mat-error>
                              }
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Enfoques</mat-label>
                              <mat-select formControlName="approachOptionIds" multiple>
                                @for (option of approachOptions(); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Cronograma</mat-label>
                              <mat-select formControlName="scheduleOptionId">
                                <mat-option [value]="null">Sin definir</mat-option>
                                @for (option of scheduleOptions(); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Estado</mat-label>
                              <mat-select formControlName="proposalStatusOptionId">
                                <mat-option [value]="null">Sin estado</mat-option>
                                @for (option of proposalStatusOptions(); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                            </mat-form-field>
                            <mat-form-field appearance="outline" class="form-full">
                              <mat-label>Observaciones</mat-label>
                              <textarea matInput formControlName="observaciones" rows="2"></textarea>
                            </mat-form-field>
                          </mat-card-content>
                        </mat-card>
                      }
                    </div>

                    <div class="step-actions">
                      <button mat-stroked-button type="button" matStepperPrevious>Anterior</button>
                      <button mat-flat-button color="primary" type="submit" [disabled]="saving() || !canEdit()">
                        <mat-icon>save</mat-icon>
                        Guardar ejes PNMC
                      </button>
                      <button mat-stroked-button type="button" matStepperNext>Continuar</button>
                    </div>
                  </form>
                </div>
                }
              </mat-step>

              <!-- Step 5: Actors -->
              <mat-step label="Actores">
                @if (isDraft()) {
                  <div class="step-locked">
                    <mat-icon>lock</mat-icon>
                    <p>Primero crea la ficha en el paso de Identificación para habilitar este paso.</p>
                  </div>
                } @else {
                <div class="gov-form">
                  <div class="array-header">
                    <p class="array-desc">Registra los actores del ecosistema musical en el territorio.</p>
                    <button mat-stroked-button type="button" [disabled]="!canEdit()" (click)="addActor()">
                      <mat-icon>add</mat-icon>
                      Agregar actor
                    </button>
                  </div>

                  <form [formGroup]="actorsForm" (ngSubmit)="saveActors()">
                    <div formArrayName="items" class="array-list">
                      @for (group of actorGroups.controls; track $index) {
                        <mat-card class="array-card" [formGroupName]="$index">
                          <mat-card-header>
                            <mat-card-title>Actor {{ $index + 1 }}</mat-card-title>
                            <button
                              mat-icon-button
                              type="button"
                              class="array-remove"
                              [attr.aria-label]="'Eliminar actor ' + ($index + 1)"
                              (click)="removeActor($index)"
                            >
                              <mat-icon>delete_outline</mat-icon>
                            </button>
                          </mat-card-header>
                          <mat-card-content class="form-grid form-grid--2col">
                            <mat-form-field appearance="outline" class="form-full">
                              <mat-label>Nombre del agente (creyente)</mat-label>
                              <input matInput type="text" maxlength="200" formControlName="nombreAgente" required />
                              @if (fieldError(group.get('nombreAgente'), 'El nombre del agente'); as error) {
                                <mat-error>{{ error }}</mat-error>
                              }
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Tipo de agente (categoría)</mat-label>
                              <mat-select formControlName="agentTypeId" (selectionChange)="onAgentTypeChange($index)">
                                <mat-option [value]="null">Sin tipo de agente</mat-option>
                                @for (option of agentTypeOptions(); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                              @if (fieldError(group.get('agentTypeId'), 'El tipo de agente'); as error) {
                                <mat-error>{{ error }}</mat-error>
                              }
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Rol en el ecosistema</mat-label>
                              <mat-select formControlName="ecosystemRoleIds" multiple>
                                @for (option of rolesFor(group.get('agentTypeId')?.value); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                              <mat-hint>
                                @if (group.get('agentTypeId')?.value) {
                                  Roles del tipo de agente seleccionado
                                } @else {
                                  Selecciona primero el tipo de agente
                                }
                              </mat-hint>
                              @if (fieldError(group.get('ecosystemRoleIds'), 'El rol en el ecosistema'); as error) {
                                <mat-error>{{ error }}</mat-error>
                              }
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Nivel territorial</mat-label>
                              <mat-select formControlName="territorialLevelOptionIds" multiple>
                                @for (option of territorialLevelOptions(); track option.id) {
                                  <mat-option [value]="option.id">{{ option.name }}</mat-option>
                                }
                              </mat-select>
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Número de contacto</mat-label>
                              <input
                                matInput
                                type="tel"
                                inputmode="numeric"
                                autocomplete="tel"
                                [maxlength]="mobilePhoneDigits"
                                formControlName="numeroContacto"
                                placeholder="3001234567"
                                (keypress)="allowDigitsOnly($event)"
                              />
                              <mat-hint>Celular de {{ mobilePhoneDigits }} dígitos, sin espacios ni guiones</mat-hint>
                              @if (fieldError(group.get('numeroContacto'), 'El número de contacto'); as error) {
                                <mat-error>{{ error }}</mat-error>
                              }
                            </mat-form-field>
                            <mat-form-field appearance="outline">
                              <mat-label>Correo electrónico</mat-label>
                              <input
                                matInput
                                type="email"
                                inputmode="email"
                                autocomplete="email"
                                maxlength="200"
                                formControlName="correoElectronico"
                                placeholder="usuario@dominio.com"
                              />
                              <mat-hint>Formato usuario&#64;dominio.com</mat-hint>
                              @if (fieldError(group.get('correoElectronico'), 'El correo electrónico'); as error) {
                                <mat-error>{{ error }}</mat-error>
                              }
                            </mat-form-field>
                            <mat-form-field appearance="outline" class="form-full">
                              <mat-label>Observaciones</mat-label>
                              <textarea matInput formControlName="observaciones" rows="2"></textarea>
                            </mat-form-field>
                          </mat-card-content>
                        </mat-card>
                      }
                    </div>

                    <div class="step-actions">
                      <button mat-stroked-button type="button" matStepperPrevious>Anterior</button>
                      <button mat-flat-button color="primary" type="submit" [disabled]="saving() || !canEdit()">
                        <mat-icon>save</mat-icon>
                        Guardar actores
                      </button>
                    </div>
                  </form>
                </div>
                }
              </mat-step>
            </mat-stepper>
          </mat-card-content>
        </mat-card>
      }
    </section>
  `,
  styles: [
    `
      :host { display: block; }
      .governance-page { display: flex; flex-direction: column; gap: var(--space-7); }
      .gov-header { display: flex; flex-direction: column; gap: var(--space-2); }
      .gov-title { font-size: 1.75rem; font-weight: var(--font-weight-extrabold); margin: 0; color: var(--color-on-surface); letter-spacing: -0.02em; }
      .gov-subtitle { margin: 0; color: var(--color-on-surface-secondary); font-size: var(--font-size-body); }
      .gov-progress { margin-top: var(--space-3); }
      .gov-fichas-card { border-radius: var(--radius-xl); border: 1px solid var(--color-border-light); box-shadow: var(--shadow-sm); }
      ::ng-deep .gov-fichas-card > .mat-mdc-card-header { padding: var(--space-5) var(--space-6); display: flex; align-items: center; justify-content: space-between; }
      ::ng-deep .gov-fichas-card > .mdc-card__content, ::ng-deep .gov-fichas-card > mat-card-content { padding: var(--space-5) var(--space-6) var(--space-6); }
      .fichas-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: var(--space-4); padding-top: var(--space-3); }
      .ficha-card { display: flex; flex-direction: column; gap: var(--space-3); padding: var(--space-5); border: 1px solid var(--color-border-light); border-radius: var(--radius-xl); background: #ffffff; cursor: pointer; text-align: left; transition: border-color var(--transition-fast), box-shadow var(--transition-fast), background var(--transition-fast), transform var(--transition-fast); }
      .ficha-card:hover { border-color: var(--color-primary-300); box-shadow: var(--shadow-md); transform: translateY(-2px); }
      .ficha-card--active { border-color: var(--color-primary-500); background: var(--color-primary-50); box-shadow: var(--shadow-lg); }
      .ficha-card-main { display: flex; align-items: flex-start; gap: var(--space-4); }
      .ficha-status { margin-left: auto; padding: 2px var(--space-3); border-radius: var(--radius-full); font-size: var(--font-size-label); font-weight: var(--font-weight-bold); white-space: nowrap; color: var(--status-color); background: color-mix(in srgb, var(--status-color) 12%, transparent); border: 1px solid color-mix(in srgb, var(--status-color) 28%, transparent); }
      .ficha-icon { color: var(--color-primary-700); flex-shrink: 0; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; background: var(--color-primary-50); border-radius: var(--radius-lg); font-size: 20px; }
      .ficha-info { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
      .ficha-dept { font-size: var(--font-size-body); font-weight: var(--font-weight-bold); color: var(--color-on-surface); }
      .ficha-date { font-size: var(--font-size-caption); color: var(--color-on-surface-secondary); }
      .ficha-resp { font-size: var(--font-size-caption); color: var(--color-on-surface-variant); }
      .ficha-stats { display: flex; gap: var(--space-4); padding-top: var(--space-3); border-top: 1px solid var(--color-border-light); }
      .ficha-stat { display: flex; align-items: center; gap: 4px; font-size: var(--font-size-caption); color: var(--color-on-surface-secondary); font-weight: var(--font-weight-semibold); }
      .ficha-stat .mat-icon { font-size: 1rem; width: 1rem; height: 1rem; }
      .gov-empty { display: flex; flex-direction: column; align-items: center; gap: var(--space-4); padding: var(--space-12) var(--space-6); text-align: center; }
      .gov-empty mat-icon { font-size: 3.5rem; width: 3.5rem; height: 3.5rem; color: var(--color-primary-200); }
      .gov-empty p { color: var(--color-on-surface-secondary); max-width: 400px; margin: 0; font-size: var(--font-size-body-sm); line-height: 1.6; }
      .gov-form-card { border-radius: var(--radius-xl); border: 1px solid var(--color-border-light); box-shadow: var(--shadow-sm); }
      ::ng-deep .gov-form-card > .mat-mdc-card-header { padding: var(--space-5) var(--space-6); }
      .gov-ficha-badge { display: inline-block; margin-left: var(--space-3); padding: 3px var(--space-3); font-size: var(--font-size-label); font-weight: var(--font-weight-semibold); color: var(--color-primary-700); background: var(--color-primary-100); border-radius: var(--radius-full); vertical-align: middle; }
      .gov-ficha-badge--draft { color: var(--color-warning-text); background: var(--color-warning-bg); }
      .gov-readonly-banner { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-4) var(--space-5); margin-bottom: var(--space-4); background: var(--color-surface-container-low); border: 1px solid var(--color-border-light); border-radius: var(--radius-lg); }
      .gov-readonly-banner mat-icon { color: var(--color-on-surface-secondary); flex-shrink: 0; }
      .gov-readonly-banner p { margin: 0; font-size: var(--font-size-body-sm); color: var(--color-on-surface-secondary); }
      .gov-form-alert { display: flex; align-items: flex-start; gap: var(--space-3); padding: var(--space-4) var(--space-5); margin-bottom: var(--space-5); background: var(--color-error-bg); border: 1px solid var(--color-error-border); border-left: 4px solid var(--color-error); border-radius: var(--radius-lg); color: var(--color-error-text); }
      .gov-form-alert-icon { flex-shrink: 0; color: var(--color-error); }
      .gov-form-alert-body { flex: 1; font-size: var(--font-size-body-sm); line-height: 1.6; }
      .gov-form-alert-body ul { margin: var(--space-2) 0 0; padding-left: var(--space-5); display: flex; flex-direction: column; gap: 2px; }
      .gov-form-alert-close { flex-shrink: 0; color: var(--color-error-text); }
      /* Campo con error: además del borde rojo de Material, un fondo suave para ubicarlo rápido
         (mat-form-field-invalid la aplica Material cuando el control es inválido y fue tocado). */
      .gov-form .mat-mdc-form-field.mat-form-field-invalid .mat-mdc-text-field-wrapper { background: color-mix(in srgb, var(--color-error) 6%, transparent); }
      .gov-form .mat-mdc-form-field.mat-form-field-invalid .mat-mdc-form-field-error { font-weight: var(--font-weight-semibold); }
      .step-locked { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-6) var(--space-5); background: var(--color-surface-container-low); border-radius: var(--radius-lg); }
      .step-locked mat-icon { color: var(--color-outline); flex-shrink: 0; }
      .step-locked p { margin: 0; font-size: var(--font-size-body-sm); color: var(--color-on-surface-secondary); }
      .gov-workflow-bar { display: flex; align-items: center; justify-content: space-between; gap: var(--space-4); flex-wrap: wrap; padding: var(--space-4) var(--space-6); background: var(--color-surface-container-low); border-bottom: 1px solid var(--color-border-light); }
      .workflow-status { display: flex; align-items: center; gap: var(--space-3); flex-wrap: wrap; }
      .workflow-badge { display: inline-block; padding: 4px var(--space-3); border-radius: var(--radius-full); font-size: var(--font-size-label); font-weight: var(--font-weight-bold); text-transform: uppercase; letter-spacing: var(--letter-spacing-label); color: var(--wf-color); background: color-mix(in srgb, var(--wf-color) 14%, transparent); border: 1px solid color-mix(in srgb, var(--wf-color) 30%, transparent); }
      .workflow-meta { font-size: var(--font-size-body-sm); color: var(--color-on-surface-secondary); }
      .workflow-actions { display: flex; align-items: center; gap: var(--space-2); flex-wrap: wrap; }
      .workflow-reject-btn { color: var(--color-error) !important; border-color: var(--color-error-border) !important; }
      .workflow-delete-btn { color: var(--color-error) !important; border-color: var(--color-error-border) !important; }
      .workflow-history { display: flex; flex-direction: column; gap: var(--space-3); padding: var(--space-5) var(--space-6); background: var(--color-surface-container-low); border-bottom: 1px solid var(--color-border-light); }
      .history-item { display: flex; align-items: flex-start; gap: var(--space-3); }
      .history-icon { color: var(--color-primary-700); flex-shrink: 0; }
      .history-info { display: flex; flex-direction: column; gap: 2px; font-size: var(--font-size-body-sm); }
      .history-info strong { color: var(--color-on-surface); }
      .history-info span { color: var(--color-on-surface-secondary); font-size: var(--font-size-caption); }
      .history-info p { margin: var(--space-1) 0 0; color: var(--color-on-surface-variant); }
      .history-empty { margin: 0; font-size: var(--font-size-body-sm); color: var(--color-on-surface-secondary); }
      .gov-stepper-wrapper { padding: var(--space-6) var(--space-2) 0; }
      .gov-form { display: flex; flex-direction: column; gap: var(--space-5); }
      /* row-gap mayor que column-gap: deja aire para el label flotante de la fila siguiente. */
      .form-grid { display: grid; row-gap: var(--space-5); column-gap: var(--space-4); padding-top: var(--space-2); }
      .form-grid--2col { grid-template-columns: repeat(2, 1fr); }
      .form-full { grid-column: 1 / -1; }
      .form-grid .mat-mdc-form-field { width: 100%; }
      .form-section-label { font-size: var(--font-size-label); font-weight: var(--font-weight-bold); color: var(--color-primary-700); text-transform: uppercase; letter-spacing: var(--letter-spacing-label); margin: var(--space-4) 0 var(--space-2); padding: var(--space-3) 0 var(--space-2); border-top: 2px solid var(--color-primary-100); }
      .step-actions { display: flex; align-items: center; gap: var(--space-3); padding-top: var(--space-5); margin-top: var(--space-2); border-top: 1px solid var(--color-border-light); }
      .step-actions button:last-child { margin-left: auto; }
      .array-header { display: flex; align-items: center; justify-content: space-between; gap: var(--space-4); margin-bottom: var(--space-4); padding: var(--space-3) var(--space-4); background: var(--color-surface-container-low); border-radius: var(--radius-lg); }
      .array-desc { margin: 0; font-size: var(--font-size-body-sm); color: var(--color-on-surface-secondary); }
      .array-list { display: flex; flex-direction: column; gap: var(--space-5); margin-bottom: var(--space-4); }
      .array-card { border-radius: var(--radius-xl); border: 1px solid var(--color-border-light); transition: border-color var(--transition-fast), box-shadow var(--transition-fast); }
      .array-card:hover { border-color: var(--color-primary-200); box-shadow: var(--shadow-sm); }
      ::ng-deep .array-card > .mat-mdc-card-header { padding: var(--space-4) var(--space-5); }
      ::ng-deep .array-card > .mdc-card__content, ::ng-deep .array-card > mat-card-content { padding: 0 var(--space-5) var(--space-5); }
      .array-remove { color: var(--color-on-surface-variant); transition: color var(--transition-fast), background-color var(--transition-fast); }
      .array-remove:hover { color: var(--color-error); }
      ::ng-deep .mat-stepper-vertical { background: transparent; }
      /* El padding superior evita que el label flotante del primer campo quede recortado por el
         contenedor del paso (que usa overflow:hidden para animar el colapso). */
      ::ng-deep .mat-vertical-content { padding: var(--space-3) 0 var(--space-6) var(--space-8); }
      ::ng-deep .mat-vertical-content-container { margin-left: 0; }
      ::ng-deep .mat-vertical-content-container.mat-vertical-content-container-active > .mat-vertical-stepper-content { overflow: visible; }
      ::ng-deep .mat-step-header { border-radius: var(--radius-lg); padding: var(--space-3) var(--space-4); min-height: 56px; }
      ::ng-deep .mat-step-header:hover { background: var(--color-hover); }
      ::ng-deep .mat-step-label { font-weight: var(--font-weight-semibold) !important; font-size: var(--font-size-body-sm) !important; }
      ::ng-deep .mat-step-label-selected { font-weight: var(--font-weight-bold) !important; color: var(--color-primary-900) !important; }
      @media (max-width: 768px) {
        .form-grid--2col { grid-template-columns: 1fr; }
        .fichas-grid { grid-template-columns: 1fr; }
        .step-actions { flex-wrap: wrap; }
        .step-actions button:last-child { margin-left: 0; }
        ::ng-deep .mat-vertical-content { padding-left: var(--space-4); }
      }
      @media (max-width: 480px) {
        .array-header { flex-direction: column; align-items: flex-start; }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GovernanceHomeComponent {
  private readonly governanceApiService = inject(GovernanceApiService);
  private readonly catalogsApiService = inject(CatalogsApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly workflowApiService = inject(WorkflowApiService);
  private readonly exportsApiService = inject(ExportsApiService);
  private readonly authTokenService = inject(AuthTokenService);
  private readonly hostRef = inject<ElementRef<HTMLElement>>(ElementRef);

  /** RBAC: el Gestor y el Administrador diligencian las hojas de la ficha. */
  readonly canEdit = computed(() =>
    this.authTokenService.hasAnyRole([AppRoles.GestorDepartamental, AppRoles.Administrador])
  );

  /** RBAC: el Líder y el Administrador revisan (aprobar / rechazar-devolver). */
  readonly canReview = computed(() =>
    this.authTokenService.hasAnyRole([AppRoles.LiderGobernanza, AppRoles.Administrador])
  );

  /** RBAC: solo el Administrador puede eliminar fichas. */
  readonly canDelete = computed(() =>
    this.authTokenService.hasAnyRole([AppRoles.Administrador])
  );

  /**
   * Estados que devuelve el backend (Borrador / Aprobado / Devuelto) traducidos a la etiqueta que
   * ve el usuario. Una ficha recién creada o recién importada está en Borrador: aún no la revisa
   * el Líder de Gobernanza.
   */
  private readonly workflowStatusLabels: Record<string, string> = {
    Borrador: 'Borrador · sin revisar',
    Aprobado: 'Aprobada',
    Devuelto: 'Devuelta para ajustes'
  };

  readonly approvalStatus = signal<FichaApprovalStatus | null>(null);
  readonly approvalHistory = signal<ApprovalRecord[]>([]);
  readonly historyOpen = signal(false);
  readonly workflowSaving = signal(false);

  readonly fichas = signal<GovernanceFichaSummary[]>([]);
  readonly selectedFicha = signal<(GovernanceFichaDetail & { departmentName?: string }) | null>(null);
  readonly departments = signal<DepartmentCatalogOption[]>([]);
  readonly municipalities = signal<MunicipalityCatalogOption[]>([]);
  readonly regionOcadOptions = signal<CatalogOption[]>([]);
  readonly informationSourceOptions = signal<CatalogOption[]>([]);
  readonly committeeStatusOptions = signal<CatalogOption[]>([]);
  readonly planStatusOptions = signal<CatalogOption[]>([]);
  readonly priorityLevelOptions = signal<CatalogOption[]>([]);
  readonly pnmcAxisOptions = signal<CatalogOption[]>([]);
  readonly approachOptions = signal<CatalogOption[]>([]);
  readonly scheduleOptions = signal<CatalogOption[]>([]);
  readonly proposalStatusOptions = signal<CatalogOption[]>([]);
  readonly agentTypeOptions = signal<CatalogOption[]>([]);
  readonly territorialLevelOptions = signal<CatalogOption[]>([]);
  /**
   * Listas dependientes indexadas por el identificador del padre (eje PNMC y tipo de agente), no
   * por la posición de la fila: así sobreviven a agregar o eliminar filas y se comparten entre
   * las que eligen el mismo padre.
   */
  readonly pnmcComponentOptions = signal<Record<number, PnmcComponentCatalogOption[]>>({});
  readonly ecosystemRoleOptions = signal<Record<number, EcosystemRoleCatalogOption[]>>({});

  /** Referencia estable para las filas sin padre elegido (evita recrear el arreglo en cada ciclo). */
  private readonly noOptions: CatalogOption[] = [];

  readonly saving = signal(false);
  readonly isDraft = signal(false);

  /** Campos por corregir de la sección que se intentó guardar (del formulario o del backend). */
  readonly formAlert = signal<FormAlert | null>(null);
  readonly councilCultureOptions = ['Existe', 'No existe', 'Por renovar'];
  readonly ordinanceOptions = ['Existe', 'No existe', 'Por activar'];

  /** Rango de captura de fechas: el mismo que valida el archivo oficial y el backend. */
  readonly minCaptureDate = new Date(2000, 0, 1);
  readonly maxCaptureDate = new Date(2100, 11, 31);
  readonly mobilePhoneDigits = MOBILE_PHONE_DIGITS;

  readonly identificationForm = this.formBuilder.group({
    fechaLevantamiento: [
      null as Date | null,
      [Validators.required, dateRangeValidator(this.minCaptureDate, this.maxCaptureDate)]
    ],
    departmentId: [null as number | null, [Validators.required]],
    municipalityId: [null as number | null],
    responsableRegistro: ['', [requiredTextValidator(), Validators.maxLength(200)]],
    regionOcadOptionId: [null as number | null],
    observaciones: [''],
    informationSourceIds: [[] as number[]]
  });

  readonly diagnosticForm = this.formBuilder.group({
    caracterizacionGeneral: [''],
    fortalezasIdentificadas: [''],
    politicasPriorizadas: [''],
    debilidadesIdentificadas: [''],
    tensionesOConflictos: [''],
    committeeStatusOptionId: [null as number | null],
    planDepartamentalCulturaStatusId: [null as number | null],
    consejoDepartamentalCultura: [''],
    planDepartamentalMusicaStatusId: [null as number | null],
    ordenanzasCulturales: [''],
    consejoDepartamentalMusica: [''],
    mesaSectorialTerritorial: [''],
    observaciones: ['']
  });

  readonly opportunitiesForm = this.formBuilder.group({ items: this.formBuilder.array([]) });
  readonly pnmcAxesForm = this.formBuilder.group({ items: this.formBuilder.array([]) });
  readonly actorsForm = this.formBuilder.group({ items: this.formBuilder.array([]) });

  get opportunityGroups(): FormArray {
    return this.opportunitiesForm.get('items') as FormArray;
  }

  get pnmcAxisGroups(): FormArray {
    return this.pnmcAxesForm.get('items') as FormArray;
  }

  get actorGroups(): FormArray {
    return this.actorsForm.get('items') as FormArray;
  }

  constructor() {
    this.refreshFichas();
    this.loadCatalogs();

    // Solo lectura para quien no puede editar (p. ej. el Líder, que revisa y aprueba).
    if (!this.canEdit()) {
      this.identificationForm.disable();
      this.diagnosticForm.disable();
      this.opportunitiesForm.disable();
      this.pnmcAxesForm.disable();
      this.actorsForm.disable();
    }
  }

  selectFicha(summary: GovernanceFichaSummary): void {
    this.isDraft.set(false);
    this.saving.set(true);
    // Se limpia antes de cargar: si la ficha no tiene alguna sección, no debe quedar la anterior.
    this.resetSectionForms();
    this.governanceApiService.getFicha(summary.id).subscribe({
      next: (detail) => {
        const enriched = { ...detail, departmentName: summary.departmentName };
        this.selectedFicha.set(enriched);
        this.identificationForm.patchValue({
          fechaLevantamiento: parseIsoDate(detail.fechaLevantamiento),
          departmentId: detail.departmentId,
          municipalityId: detail.municipalityId,
          responsableRegistro: detail.responsableRegistro,
          regionOcadOptionId: detail.regionOcadOptionId,
          observaciones: detail.observaciones,
          informationSourceIds: detail.informationSourceIds
        });
        this.loadMunicipalities();
        this.loadSections(detail.id);
        this.historyOpen.set(false);
        this.approvalHistory.set([]);
        this.loadWorkflowStatus(detail.id);
        this.saving.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.handleSaveError(err, 'Error al cargar la ficha');
      }
    });
  }

  loadMunicipalities(): void {
    const departmentId = this.identificationForm.get('departmentId')?.value;
    if (!departmentId) {
      this.municipalities.set([]);
      return;
    }
    this.catalogsApiService.getMunicipalities(departmentId).subscribe({
      next: (data) => this.municipalities.set(data)
    });
  }

  workflowStatusLabel(): string {
    return this.statusLabel(this.approvalStatus()?.status);
  }

  workflowStatusColor(): string {
    return this.statusColor(this.approvalStatus()?.status);
  }

  /** Etiqueta de estado de una ficha del listado (incluidas las que llegan por importación). */
  statusLabel(status: string | null | undefined): string {
    if (!status) return this.workflowStatusLabels['Borrador'];
    return this.workflowStatusLabels[status] ?? status;
  }

  statusColor(status: string | null | undefined): string {
    if (status === 'Aprobado') return 'var(--color-success)';
    if (status === 'Devuelto') return 'var(--color-error)';
    return 'var(--color-warning)';
  }

  approveFicha(ficha: GovernanceFichaDetail & { departmentName?: string }): void {
    this.confirmDialog
      .confirm({
        title: 'Aprobar ficha',
        message: `¿Deseas aprobar la ficha de ${ficha.departmentName}?`,
        confirmLabel: 'Aprobar',
        icon: 'check_circle'
      })
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.workflowSaving.set(true);
        this.workflowApiService.approve(ficha.id, null).subscribe({
          next: () => {
            this.workflowSaving.set(false);
            this.snackBar.open('Ficha aprobada correctamente', 'Cerrar', { duration: 3000 });
            this.loadWorkflowStatus(ficha.id);
          },
          error: () => {
            this.workflowSaving.set(false);
            this.snackBar.open('Error al aprobar la ficha', 'Cerrar', { duration: 5000 });
          }
        });
      });
  }

  rejectFicha(ficha: GovernanceFichaDetail & { departmentName?: string }): void {
    this.confirmDialog
      .confirmWithComment({
        title: 'Rechazar ficha',
        message: `¿Deseas rechazar la ficha de ${ficha.departmentName}? Indica el motivo del rechazo.`,
        confirmLabel: 'Rechazar',
        icon: 'cancel',
        variant: 'danger',
        commentLabel: 'Motivo del rechazo'
      })
      .subscribe(({ confirmed, comment }) => {
        if (!confirmed) return;
        this.workflowSaving.set(true);
        this.workflowApiService.reject(ficha.id, comment).subscribe({
          next: () => {
            this.workflowSaving.set(false);
            this.snackBar.open('Ficha rechazada', 'Cerrar', { duration: 3000 });
            this.loadWorkflowStatus(ficha.id);
          },
          error: () => {
            this.workflowSaving.set(false);
            this.snackBar.open('Error al rechazar la ficha', 'Cerrar', { duration: 5000 });
          }
        });
      });
  }

  deleteFicha(ficha: GovernanceFichaDetail & { departmentName?: string }): void {
    this.confirmDialog
      .confirm({
        title: 'Eliminar ficha',
        message: `¿Estás seguro de que deseas eliminar la ficha de ${ficha.departmentName}? Esta acción no se puede deshacer.`,
        confirmLabel: 'Eliminar',
        icon: 'delete_forever',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.workflowSaving.set(true);
        this.governanceApiService.deleteFicha(ficha.id).subscribe({
          next: () => {
            this.workflowSaving.set(false);
            this.snackBar.open('Ficha eliminada correctamente', 'Cerrar', { duration: 3000 });
            this.selectedFicha.set(null);
            this.refreshFichas();
          },
          error: (err) => {
            this.workflowSaving.set(false);
            const message = err.status === 403
              ? 'Solo los administradores pueden eliminar fichas'
              : 'Error al eliminar la ficha';
            this.snackBar.open(message, 'Cerrar', { duration: 5000 });
          }
        });
      });
  }

  toggleHistory(): void {
    this.historyOpen.update((open) => !open);
    const ficha = this.selectedFicha();
    if (this.historyOpen() && ficha) {
      this.workflowApiService.getHistory(ficha.id).subscribe({
        next: (records) => this.approvalHistory.set(records),
        error: () => this.snackBar.open('Error al cargar el historial de aprobación', 'Cerrar', { duration: 5000 })
      });
    }
  }

  downloadExcel(ficha: GovernanceFichaDetail & { departmentName?: string }): void {
    this.exportsApiService.exportFichaExcel(ficha.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `ficha_${ficha.departmentName ?? 'gobernanza'}_${ficha.fechaLevantamiento}.xlsm`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => this.snackBar.open('Error al descargar el archivo Excel', 'Cerrar', { duration: 5000 })
    });
  }

  private loadWorkflowStatus(fichaId: string): void {
    this.workflowApiService.getStatus(fichaId).subscribe({
      next: (status) => this.approvalStatus.set(status),
      error: () => this.approvalStatus.set(null)
    });
  }

  startCreate(): void {
    this.selectedFicha.set(null);
    this.isDraft.set(true);
    this.municipalities.set([]);
    this.approvalStatus.set(null);
    this.approvalHistory.set([]);
    this.historyOpen.set(false);
    this.identificationForm.reset({
      fechaLevantamiento: new Date(),
      departmentId: null,
      municipalityId: null,
      responsableRegistro: '',
      regionOcadOptionId: null,
      observaciones: '',
      informationSourceIds: []
    });

    // Sin esto, los pasos 2 a 5 seguirían mostrando lo diligenciado en la ficha anterior.
    this.resetSectionForms();
  }

  cancelDraft(): void {
    this.isDraft.set(false);
    this.identificationForm.reset();
    this.resetSectionForms();
  }

  /**
   * Deja el diagnóstico y las secciones de colección en blanco. Se llama al empezar una ficha
   * nueva y antes de cargar otra, porque el API no devuelve secciones vacías (no hay nada que
   * traer) y los formularios conservarían los datos de la ficha anterior.
   */
  private resetSectionForms(): void {
    this.diagnosticForm.reset();

    for (const array of [this.opportunityGroups, this.pnmcAxisGroups, this.actorGroups]) {
      while (array.length) {
        array.removeAt(0);
      }
    }

    this.opportunityGroups.push(this.createOpportunityGroup());
    this.pnmcAxisGroups.push(this.createPnmcAxisGroup());
    this.actorGroups.push(this.createActorGroup());

    // Las listas dependientes están cacheadas por índice de fila: también deben limpiarse.
    this.pnmcComponentOptions.set({});
    this.ecosystemRoleOptions.set({});
    this.formAlert.set(null);
  }

  onIdentificationSubmit(): void {
    if (this.isDraft()) {
      this.createFromIdentification();
    } else {
      this.saveIdentification();
    }
  }

  createFromIdentification(): void {
    if (!this.canSave(this.identificationForm, 'Para crear la ficha faltan datos por diligenciar:')) {
      return;
    }

    this.saving.set(true);
    const raw = this.identificationForm.getRawValue();
    this.governanceApiService
      .createFicha({
        fechaLevantamiento: toIsoDate(raw.fechaLevantamiento),
        departmentId: raw.departmentId ?? 0,
        municipalityId: raw.municipalityId,
        responsableRegistro: raw.responsableRegistro ?? '',
        regionOcadOptionId: raw.regionOcadOptionId,
        observaciones: raw.observaciones ?? null,
        informationSourceIds: raw.informationSourceIds ?? []
      })
      .subscribe({
        next: (detail) => {
          this.saving.set(false);
          this.isDraft.set(false);
          const deptName = this.departments().find((d) => d.id === raw.departmentId)?.name ?? 'Nuevo';
          this.selectedFicha.set({ ...detail, departmentName: deptName });
          this.snackBar.open('Ficha creada correctamente', 'Cerrar', { duration: 3000 });
          this.refreshFichas();
          this.loadSections(detail.id);
          this.loadWorkflowStatus(detail.id);
        },
error: (err: HttpErrorResponse) => {
          this.handleSaveError(err, 'Error al crear la ficha');
        }
      });
  }

  saveIdentification(): void {
    const ficha = this.selectedFicha();
    if (!ficha) return;

    if (!this.canSave(this.identificationForm, 'Para guardar la identificación faltan datos por diligenciar:')) {
      return;
    }

    this.saving.set(true);
    const raw = this.identificationForm.getRawValue();
    this.governanceApiService
      .updateFicha(ficha.id, {
        fechaLevantamiento: toIsoDate(raw.fechaLevantamiento),
        departmentId: raw.departmentId ?? 0,
        municipalityId: raw.municipalityId,
        responsableRegistro: raw.responsableRegistro ?? '',
        regionOcadOptionId: raw.regionOcadOptionId,
        observaciones: raw.observaciones ?? null,
        informationSourceIds: raw.informationSourceIds ?? []
      })
      .subscribe({
        next: (detail) => {
          this.selectedFicha.set({ ...detail, departmentName: ficha.departmentName });
          this.refreshFichas();
          this.saving.set(false);
          this.snackBar.open('Identificación guardada', 'Cerrar', { duration: 3000 });
        },
        error: (err: HttpErrorResponse) => {
          this.handleSaveError(err, 'Error al guardar');
        }
      });
  }

  saveDiagnostic(): void {
    const ficha = this.selectedFicha();
    if (!ficha) return;

    this.saving.set(true);
    const raw = this.diagnosticForm.getRawValue();
    this.governanceApiService.updateDiagnostic(ficha.id, { id: null, ...raw }).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Diagnóstico guardado', 'Cerrar', { duration: 3000 });
      },
      error: (err: HttpErrorResponse) => {
        this.handleSaveError(err, 'Error al guardar');
      }
    });
  }

  addOpportunity(): void {
    this.opportunityGroups.push(this.createOpportunityGroup());
  }

  removeOpportunity(index: number): void {
    this.confirmDialog
      .confirm({
        title: 'Eliminar oportunidad',
        message: `¿Deseas eliminar la oportunidad ${index + 1}? Esta acción no se puede deshacer.`,
        confirmLabel: 'Eliminar',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (confirmed) {
          this.opportunityGroups.removeAt(index);
        }
      });
  }

  saveOpportunities(): void {
    const ficha = this.selectedFicha();
    if (!ficha) return;

    this.saving.set(true);
    this.governanceApiService
      .replaceOpportunities(ficha.id, this.filledRows<GovernanceOpportunity>(this.opportunityGroups))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.snackBar.open('Oportunidades guardadas', 'Cerrar', { duration: 3000 });
        },
        error: (err: HttpErrorResponse) => {
          this.handleSaveError(err, 'Error al guardar');
        }
      });
  }

  addPnmcAxis(): void {
    this.pnmcAxisGroups.push(this.createPnmcAxisGroup());
  }

  removePnmcAxis(index: number): void {
    this.confirmDialog
      .confirm({
        title: 'Eliminar eje PNMC',
        message: `¿Deseas eliminar el eje PNMC ${index + 1}? Esta acción no se puede deshacer.`,
        confirmLabel: 'Eliminar',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (confirmed) {
          this.pnmcAxisGroups.removeAt(index);
        }
      });
  }

  savePnmcAxes(): void {
    const ficha = this.selectedFicha();
    if (!ficha) return;

    if (!this.canSave(this.pnmcAxesForm, 'Para guardar los ejes PNMC hay datos por corregir:', 'Eje PNMC')) {
      return;
    }

    this.saving.set(true);
    this.governanceApiService
      .replacePnmcAxes(ficha.id, this.filledRows<GovernancePnmcAxis>(this.pnmcAxisGroups))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.snackBar.open('Ejes PNMC guardados', 'Cerrar', { duration: 3000 });
        },
        error: (err: HttpErrorResponse) => {
          this.handleSaveError(err, 'Error al guardar');
        }
      });
  }

  addActor(): void {
    this.actorGroups.push(this.createActorGroup());
  }

  removeActor(index: number): void {
    this.confirmDialog
      .confirm({
        title: 'Eliminar actor',
        message: `¿Deseas eliminar el actor ${index + 1}? Esta acción no se puede deshacer.`,
        confirmLabel: 'Eliminar',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (confirmed) {
          this.actorGroups.removeAt(index);
        }
      });
  }

  saveActors(): void {
    const ficha = this.selectedFicha();
    if (!ficha) return;

    if (!this.canSave(this.actorsForm, 'Para guardar los actores hay datos por corregir:', 'Actor')) {
      return;
    }

    this.saving.set(true);
    this.governanceApiService
      .replaceActors(ficha.id, this.filledRows<GovernanceActor>(this.actorGroups))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.snackBar.open('Actores guardados', 'Cerrar', { duration: 3000 });
        },
        error: (err: HttpErrorResponse) => {
          this.handleSaveError(err, 'Error al guardar');
        }
      });
  }

  /**
   * Mensaje de error del campo, tomado del catálogo compartido de validaciones para que la misma
   * regla se explique igual en todo el portal. Devuelve `null` cuando no hay error visible.
   */
  fieldError(control: AbstractControl | null, label: string): string | null {
    return describeFieldError(control, label);
  }

  /**
   * Filas de la sección que sí se envían: las que están completamente en blanco se descartan, para
   * que la fila vacía que el formulario muestra por comodidad no se guarde como un registro.
   */
  private filledRows<T>(array: FormArray): T[] {
    return array.controls.filter((group) => isRowFilled(group)).map((group) => group.getRawValue() as T);
  }

  /**
   * Comprueba una sección antes de guardar. Si hay campos por corregir: marca todo como tocado
   * (para que se resalten en rojo), arma el listado de campos con su nombre y motivo, y lleva la
   * vista al primer campo con problema. Devuelve `true` cuando se puede guardar.
   */
  private canSave(form: FormGroup, title: string, itemLabel = 'Registro'): boolean {
    if (form.valid) {
      this.formAlert.set(null);
      return true;
    }

    form.markAllAsTouched();
    this.formAlert.set({ title, items: this.collectInvalidFields(form, itemLabel) });
    this.scrollToFirstInvalidField();
    return false;
  }

  /**
   * Recorre el formulario y devuelve cada campo inválido con su nombre y el motivo. En las
   * secciones de colección antepone el número del registro ("Actor 2 · Correo electrónico").
   */
  private collectInvalidFields(control: AbstractControl, itemLabel: string, prefix = ''): FormAlert['items'] {
    const items: FormAlert['items'] = [];

    if (control instanceof FormArray) {
      control.controls.forEach((child, index) => {
        if (child.invalid) {
          items.push(...this.collectInvalidFields(child, itemLabel, `${itemLabel} ${index + 1} · `));
        }
      });

      return items;
    }

    if (control instanceof FormGroup) {
      for (const [name, child] of Object.entries(control.controls)) {
        if (child.valid) continue;

        if (child instanceof FormArray || child instanceof FormGroup) {
          items.push(...this.collectInvalidFields(child, itemLabel, prefix));
        } else {
          const label = `${prefix}${FIELD_LABELS[name] ?? name}`;
          items.push({ label, message: describeFieldError(child, label) ?? 'Revisa el valor ingresado.' });
        }
      }
    }

    return items;
  }

  /** Lleva la vista al primer campo marcado en rojo para que el usuario lo vea de inmediato. */
  private scrollToFirstInvalidField(): void {
    setTimeout(() => {
      const invalid = this.hostRef.nativeElement.querySelector<HTMLElement>(
        '.mat-mdc-form-field .ng-invalid.ng-touched'
      );
      invalid?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      invalid?.focus?.({ preventScroll: true });
    });
  }

  /**
   * Traslada los errores de validación del backend (mismos campos y reglas) al aviso de la
   * sección, para que el usuario vea qué corregir aunque el problema se detecte en el servidor.
   */
  private showServerValidationErrors(error: HttpErrorResponse, fallbackTitle: string): boolean {
    const body = error?.error as
      | { detalleResumen?: string; errores?: { etiqueta?: string; mensaje?: string }[] }
      | undefined;
    const errores = body?.errores;

    if (!Array.isArray(errores) || errores.length === 0) {
      return false;
    }

    this.formAlert.set({
      title: body?.detalleResumen?.trim() || fallbackTitle,
      items: errores.map((item) => ({
        label: item.etiqueta ?? 'Campo',
        message: item.mensaje ?? 'Revisa el valor ingresado.'
      }))
    });

    return true;
  }

  /** Error de guardado: se muestra el detalle por campo cuando el backend lo envía. */
  private handleSaveError(error: HttpErrorResponse, fallback: string): void {
    this.saving.set(false);
    if (!this.showServerValidationErrors(error, fallback)) {
      this.formAlert.set(null);
    }
    this.snackBar.open(extractErrorMessage(error, fallback), 'Cerrar', { duration: 6000 });
  }

  /** Valor en pesos formateado, para mostrarlo como ayuda debajo del campo. */
  formatCopValue(value: unknown): string {
    return formatCop(value as number | string | null);
  }

  /** Restringe la captura a dígitos (celulares y campos numéricos). */
  allowDigitsOnly(event: KeyboardEvent): void {
    const isControlKey = event.key.length > 1 || event.ctrlKey || event.metaKey;
    if (!isControlKey && !/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }

  /** Componentes disponibles para el eje elegido en la fila. */
  componentsFor(axisId: unknown): PnmcComponentCatalogOption[] {
    return typeof axisId === 'number' ? this.pnmcComponentOptions()[axisId] ?? this.noOptions : this.noOptions;
  }

  /** Roles disponibles para el tipo de agente elegido en la fila. */
  rolesFor(agentTypeId: unknown): EcosystemRoleCatalogOption[] {
    return typeof agentTypeId === 'number'
      ? this.ecosystemRoleOptions()[agentTypeId] ?? this.noOptions
      : this.noOptions;
  }

  /**
   * Al cambiar el eje se limpia el componente: el que estuviera elegido pertenecía al eje anterior
   * y ya no aparece en la lista.
   */
  onPnmcAxisChange(index: number): void {
    const group = this.pnmcAxisGroups.at(index);
    if (!group) return;

    group.get('pnmcComponentId')?.setValue(null);
    this.loadPnmcComponents(group.get('pnmcAxisId')?.value as number | null);
  }

  /** Al cambiar el tipo de agente se limpian los roles, que dependían del tipo anterior. */
  onAgentTypeChange(index: number): void {
    const group = this.actorGroups.at(index);
    if (!group) return;

    group.get('ecosystemRoleIds')?.setValue([]);
    this.loadActorRoles(group.get('agentTypeId')?.value as number | null);
  }

  private loadPnmcComponents(axisId: number | null): void {
    if (!axisId || this.pnmcComponentOptions()[axisId]) return;
    this.catalogsApiService.getPnmcComponents(axisId).subscribe({
      next: (options) =>
        this.pnmcComponentOptions.update((state) => ({ ...state, [axisId]: options }))
    });
  }

  private loadActorRoles(agentTypeId: number | null): void {
    if (!agentTypeId || this.ecosystemRoleOptions()[agentTypeId]) return;
    this.catalogsApiService.getEcosystemRoles(agentTypeId).subscribe({
      next: (options) =>
        this.ecosystemRoleOptions.update((state) => ({ ...state, [agentTypeId]: options }))
    });
  }

  /**
   * Carga las listas dependientes de las filas ya diligenciadas. Sin esto, al abrir una ficha
   * guardada los campos Componente PNMC y Rol en el ecosistema aparecían vacíos: el valor estaba
   * en el formulario, pero la lista de opciones donde buscarlo todavía no se había pedido.
   */
  private loadDependentOptionsForLoadedRows(): void {
    for (const group of this.pnmcAxisGroups.controls) {
      this.loadPnmcComponents(group.get('pnmcAxisId')?.value as number | null);
    }

    for (const group of this.actorGroups.controls) {
      this.loadActorRoles(group.get('agentTypeId')?.value as number | null);
    }
  }

  private loadSections(fichaId: string): void {
    this.governanceApiService
      .getDiagnostic(fichaId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => (data ? this.diagnosticForm.patchValue(data) : this.diagnosticForm.reset()),
        // 404 = la ficha aún no tiene diagnóstico diligenciado: el formulario queda en blanco.
        error: () => this.diagnosticForm.reset()
      });

    this.governanceApiService
      .getOpportunities(fichaId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => this.replaceArray(this.opportunityGroups, items, () => this.createOpportunityGroup())
      });

    this.governanceApiService
      .getPnmcAxes(fichaId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.replaceArray(this.pnmcAxisGroups, items, () => this.createPnmcAxisGroup());
          this.loadDependentOptionsForLoadedRows();
        }
      });

    this.governanceApiService
      .getActors(fichaId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.replaceArray(this.actorGroups, items, () => this.createActorGroup());
          this.loadDependentOptionsForLoadedRows();
        }
      });
  }

  private replaceArray(
    array: FormArray,
    items: unknown[],
    factory: () => ReturnType<FormBuilder['group']>
  ): void {
    while (array.length) {
      array.removeAt(0);
    }
    if (items.length === 0) {
      array.push(factory());
      return;
    }
    items.forEach((item) => {
      const group = factory();
      group.patchValue(item as object);
      array.push(group);
    });
  }

  private createOpportunityGroup() {
    return this.formBuilder.group({
      id: [null as string | null],
      situacionIdentificada: [''],
      componenteOtrasDependenciasEntidades: [''],
      aliadosYCreyentes: [''],
      territorioInfluencia: [''],
      priorityLevelOptionId: [null as number | null],
      descripcionAdicional: ['']
    });
  }

  private createPnmcAxisGroup() {
    return this.formBuilder.group(
      {
        id: [null as string | null],
        descripcionHallazgo: [''],
        pnmcAxisId: [null as number | null],
        pnmcComponentId: [null as number | null],
        accionEstrategica: [''],
        politicaPriorizada: [''],
        armonizacionPnc: [''],
        armonizacionPnd: [''],
        armonizacionInternacional: [''],
        priorityLevelOptionId: [null as number | null],
        aliadosResponsables: [''],
        fuentesFinanciacion: [''],
        valorPropuestaCop: [null as number | null, [copAmountValidator()]],
        approachOptionIds: [[] as number[]],
        descripcion: [''],
        scheduleOptionId: [null as number | null],
        proposalStatusOptionId: [null as number | null],
        observaciones: ['']
      },
      {
        // El componente se filtra por el eje: no se puede guardar uno sin el otro.
        validators: [
          dependentPairValidator('pnmcAxisId', 'pnmcComponentId', {
            parent: 'Selecciona el eje PNMC: de él depende la lista de componentes.',
            child: 'Selecciona el componente de la lista, que se filtra según el eje PNMC elegido.'
          })
        ]
      }
    );
  }

  private createActorGroup() {
    return this.formBuilder.group(
      {
        id: [null as string | null],
        // El nombre es obligatorio solo si la fila trae algo: una fila en blanco no se guarda.
        nombreAgente: ['', [Validators.maxLength(200)]],
        agentTypeId: [null as number | null],
        ecosystemRoleIds: [[] as number[]],
        territorialLevelOptionIds: [[] as number[]],
        // Celular: solo dígitos y exactamente 10 (misma regla en el backend).
        numeroContacto: ['', [mobilePhoneValidator()]],
        correoElectronico: ['', [emailFormatValidator(), Validators.maxLength(200)]],
        observaciones: ['']
      },
      {
        // El rol en el ecosistema se filtra por el tipo de agente: van siempre juntos.
        validators: [
          requiredWhenRowFilledValidator('nombreAgente', 'Escribe el nombre del agente.'),
          dependentPairValidator('agentTypeId', 'ecosystemRoleIds', {
            parent: 'Selecciona el tipo de agente: de él depende la lista de roles del ecosistema.',
            child: 'Selecciona al menos un rol de la lista, que se filtra según el tipo de agente elegido.'
          })
        ]
      }
    );
  }

  private loadCatalogs(): void {
    this.catalogsApiService.getDepartments().subscribe({ next: (data) => this.departments.set(data) });
    this.catalogsApiService.getLookup('region-ocad').subscribe({ next: (data) => this.regionOcadOptions.set(data) });
    this.catalogsApiService.getLookup('information-sources').subscribe({ next: (data) => this.informationSourceOptions.set(data) });
    this.catalogsApiService.getLookup('committee-statuses').subscribe({ next: (data) => this.committeeStatusOptions.set(data) });
    this.catalogsApiService.getLookup('plan-statuses').subscribe({ next: (data) => this.planStatusOptions.set(data) });
    this.catalogsApiService.getLookup('priority-levels').subscribe({ next: (data) => this.priorityLevelOptions.set(data) });
    this.catalogsApiService.getLookup('pnmc-axes').subscribe({ next: (data) => this.pnmcAxisOptions.set(data) });
    this.catalogsApiService.getLookup('approaches').subscribe({ next: (data) => this.approachOptions.set(data) });
    this.catalogsApiService.getLookup('schedule-options').subscribe({ next: (data) => this.scheduleOptions.set(data) });
    this.catalogsApiService.getLookup('proposal-statuses').subscribe({ next: (data) => this.proposalStatusOptions.set(data) });
    this.catalogsApiService.getLookup('agent-types').subscribe({ next: (data) => this.agentTypeOptions.set(data) });
    this.catalogsApiService.getLookup('territorial-levels').subscribe({ next: (data) => this.territorialLevelOptions.set(data) });
  }

  private refreshFichas(): void {
    this.governanceApiService.getFichas().subscribe({ next: (fichas) => this.fichas.set(fichas) });
  }
}
