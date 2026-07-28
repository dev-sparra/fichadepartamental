import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { finalize } from 'rxjs';

import { UsersApiService, UserDto } from './services/users-api.service';
import { UserFormDialogComponent, UserFormDialogResult } from './user-form-dialog.component';
import { ResetPasswordDialogComponent } from './reset-password-dialog.component';
import { CatalogManageDialogComponent } from './catalog-manage-dialog.component';
import { CatalogsApiService } from './services/catalogs-api.service';
import { AuditLogPanelComponent } from './audit-log-panel.component';
import { CatalogDefinition } from '../../shared/models/catalog.models';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';
import { ConfirmDialogService } from '../../shared/services/confirm-dialog.service';

interface CatalogEntry {
  definition: CatalogDefinition;
  childDefinition?: CatalogDefinition;
  itemCount: number;
}

@Component({
  selector: 'app-administration-home',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatTabsModule,
    AuditLogPanelComponent
  ],
  template: `
    <section class="admin-page">
      <header class="adm-header">
        <h1 class="adm-title">Administración</h1>
        <p class="adm-subtitle">Gestión de usuarios y catálogos del sistema</p>
      </header>

      <mat-tab-group>
        <!-- Users tab -->
        <mat-tab label="Usuarios">
          <div class="tab-content">
            <mat-card class="table-card">
              @if (loading() || saving()) {
                <mat-progress-bar mode="indeterminate" class="card-progress" />
              }

              <mat-card-content class="toolbar">
                <mat-form-field appearance="outline" class="search-field">
                  <mat-label>Buscar usuario</mat-label>
                  <input matInput [formControl]="userSearch" placeholder="Nombre o correo" />
                  <mat-icon matPrefix>search</mat-icon>
                </mat-form-field>

                <span class="user-count">{{ filteredUsers().length }} de {{ users().length }} usuarios</span>

                <button mat-flat-button color="primary" type="button" (click)="openCreate()" [disabled]="saving()">
                  <mat-icon>person_add</mat-icon>
                  Nuevo usuario
                </button>
              </mat-card-content>

              <mat-divider />

              <div class="table-responsive">
                <table class="data-table">
                  <thead>
                    <tr>
                      <th>Usuario</th>
                      <th>Roles</th>
                      <th>Estado</th>
                      <th class="col-action">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (user of filteredUsers(); track user.id) {
                      <tr [class.row--inactive]="!user.isActive">
                        <td>
                          <div class="user-cell">
                            <div class="user-avatar-sm">{{ (user.displayName ?? user.email).charAt(0).toUpperCase() }}</div>
                            <div class="user-info">
                              <strong>{{ user.displayName ?? 'Sin nombre' }}</strong>
                              <span>{{ user.email }}</span>
                            </div>
                          </div>
                        </td>
                        <td>
                          <div class="role-list">
                            @for (role of user.roles; track role) {
                              <span class="role-badge">{{ role }}</span>
                            }
                          </div>
                        </td>
                        <td>
                          <span class="status-indicator">
                            <span class="status-dot" [class.status-dot--active]="user.isActive"></span>
                            {{ user.isActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </td>
<td class="col-action">
                           <button
                             mat-icon-button type="button"
                             [attr.aria-label]="'Editar ' + (user.displayName ?? user.email)"
                             (click)="openEdit(user)">
                             <mat-icon>edit</mat-icon>
                           </button>
                           <button
                             mat-icon-button type="button"
                             [attr.aria-label]="'Restablecer contraseña de ' + (user.displayName ?? user.email)"
                             [disabled]="!user.isActive"
                             (click)="resetPassword(user)">
                             <mat-icon>key</mat-icon>
                           </button>
                         </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>

              @if (users().length === 0 && !loading()) {
                <mat-card-content class="empty-state">
                  <div class="empty-icon-box">
                    <mat-icon>people_off</mat-icon>
                  </div>
                  <h3 class="empty-title">Sin usuarios registrados</h3>
                  <p class="empty-desc">Crea el primer usuario para habilitar el acceso al portal.</p>
                  <div class="empty-actions">
                    <button mat-flat-button color="primary" type="button" (click)="openCreate()">
                      <mat-icon>person_add</mat-icon>
                      <span>Crear usuario</span>
                    </button>
                  </div>
                </mat-card-content>
              }
            </mat-card>
          </div>
        </mat-tab>

        <!-- Catalogs tab -->
        <mat-tab label="Catálogos">
          <div class="tab-content">
            <div class="catalog-readonly-banner">
              <mat-icon>info_outline</mat-icon>
              <p>Estos catálogos alimentan los formularios de Gobernanza e Indicadores y la plantilla de importación. Cada cambio se refleja de inmediato en la plantilla descargable de <strong>Importaciones</strong>.</p>
            </div>

            <div class="catalog-toolbar">
              <mat-form-field appearance="outline" class="search-field">
                <mat-label>Buscar catálogo</mat-label>
                <input matInput [formControl]="catalogSearch" placeholder="Nombre del catálogo" />
                <mat-icon matPrefix>search</mat-icon>
              </mat-form-field>
              <span class="catalog-count-summary">{{ filteredCatalogs().length }} catálogos</span>
            </div>

            @if (catalogsLoading()) {
              <mat-progress-bar mode="indeterminate" />
            }

            @if (filteredCatalogs().length > 0) {
              <div class="catalog-grid">
                @for (catalog of filteredCatalogs(); track catalog.definition.key) {
                  <mat-card class="catalog-card">
                    <mat-card-header>
                      <div class="catalog-icon-box">
                        <mat-icon>list_alt</mat-icon>
                      </div>
                      <mat-card-title>{{ catalog.definition.displayName }}</mat-card-title>
                      <mat-card-subtitle>{{ catalog.itemCount }} elementos</mat-card-subtitle>
                    </mat-card-header>
                    <mat-card-actions class="catalog-card-actions">
                      <button mat-flat-button color="primary" type="button" (click)="manageCatalog(catalog)">
                        <mat-icon>edit</mat-icon>
                        Gestionar
                      </button>
                    </mat-card-actions>
                  </mat-card>
                }
              </div>
            } @else if (!catalogsLoading()) {
              <div class="empty-state">
                <div class="empty-icon-box">
                  <mat-icon>search_off</mat-icon>
                </div>
                <h3 class="empty-title">Sin resultados</h3>
                <p class="empty-desc">No se encontraron catálogos con ese nombre. Intenta con otro término.</p>
              </div>
            }
          </div>
        </mat-tab>

        <!-- Audit tab -->
        <mat-tab label="Auditoría">
          <div class="tab-content">
            <p class="tab-intro">
              Todo lo que se hace en el portal queda registrado: quién, cuándo, en qué módulo, sobre
              qué objeto y qué cambió exactamente. Selecciona una fila para ver el detalle.
            </p>
            <app-audit-log-panel />
          </div>
        </mat-tab>
      </mat-tab-group>
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .admin-page {
        display: flex;
        flex-direction: column;
        gap: var(--space-7);
      }

      .adm-header {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .adm-title {
        font-size: 1.75rem;
        font-weight: var(--font-weight-extrabold);
        margin: 0;
        color: var(--color-on-surface);
        letter-spacing: -0.02em;
      }

      .adm-subtitle {
        margin: 0;
        color: var(--color-on-surface-secondary);
        font-size: var(--font-size-body);
      }

      .tab-content {
        padding: var(--space-4) 0;
        display: flex;
        flex-direction: column;
        gap: var(--space-5);
      }

      /* ── Toolbar ── */
      .table-card {
        border-radius: var(--radius-xl);
        border: 1px solid var(--color-border-light);
        box-shadow: var(--shadow-sm);
        overflow: hidden;
      }

      .card-progress {
        border-radius: 0;
      }

      .toolbar {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        flex-wrap: wrap;
        padding: var(--space-5) var(--space-6) !important;
      }

      .search-field {
        flex: 1;
        min-width: 220px;
        max-width: 360px;
      }

      .user-count {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-semibold);
        white-space: nowrap;
        margin-left: auto;
        margin-right: var(--space-2);
      }

      /* ── Table ── */
      .table-responsive {
        overflow-x: auto;
      }

      .data-table {
        width: 100%;
        border-collapse: collapse;
        font-size: var(--font-size-body-sm);
      }

      .data-table thead {
        background: var(--color-surface-container-low);
      }

      .data-table th {
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

      .data-table td {
        padding: var(--space-3) var(--space-4);
        border-bottom: 1px solid var(--color-border-light);
        color: var(--color-on-surface);
        vertical-align: middle;
      }

      .data-table tbody tr:hover {
        background: var(--color-hover);
      }

      .data-table tbody tr:last-child td {
        border-bottom: none;
      }

      .row--inactive td {
        color: var(--color-on-surface-secondary);
        opacity: 0.7;
      }

      .col-action {
        width: 104px;
        text-align: center;
        white-space: nowrap;
      }

      /* ── User cell ── */
      .user-cell {
        display: flex;
        align-items: center;
        gap: var(--space-3);
      }

      .user-avatar-sm {
        width: 40px;
        height: 40px;
        border-radius: var(--radius-full);
        background: var(--color-primary-600);
        color: white;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-extrabold);
        flex-shrink: 0;
        text-transform: uppercase;
        box-shadow: 0 2px 4px rgba(40, 30, 82, 0.12);
      }

      .user-info {
        display: flex;
        flex-direction: column;
        gap: 1px;
      }

      .user-info strong {
        color: var(--color-on-surface);
      }

      .user-info span {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
      }

      /* ── Roles ── */
      .role-list {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-1);
      }

      .role-badge {
        display: inline-block;
        padding: 3px var(--space-3);
        border-radius: var(--radius-md);
        font-size: var(--font-size-label);
        font-weight: var(--font-weight-semibold);
        color: var(--color-primary-700);
        background: var(--color-primary-50);
        border: 1px solid var(--color-primary-200);
        white-space: nowrap;
      }

      /* ── Status ── */
      .status-indicator {
        display: flex;
        align-items: center;
        gap: var(--space-2);
      }

      .status-dot {
        display: inline-block;
        width: 10px;
        height: 10px;
        border-radius: var(--radius-full);
        background: var(--color-border);
        flex-shrink: 0;
        box-shadow: 0 0 0 2px #ffffff;
      }

      .status-dot--active {
        background: var(--color-success);
        box-shadow: 0 0 0 2px #ffffff, 0 0 0 3px var(--color-success);
      }

      /* ── Empty state ── */
      .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-12) var(--space-6);
        text-align: center;
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

      .empty-state .empty-icon-box mat-icon,
      .empty-state > mat-icon {
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
        margin: 0;
        max-width: 360px;
        line-height: 1.5;
        font-weight: var(--font-weight-medium);
      }

      .empty-actions {
        margin-top: var(--space-2);
      }

      ::ng-deep .empty-actions .mat-mdc-unelevated-button {
        background-color: var(--color-primary-900) !important;
        color: #ffffff !important;
        font-weight: var(--font-weight-bold) !important;
        padding: 0 var(--space-6) !important;
        height: 44px !important;
        border-radius: var(--radius-lg) !important;
      }

      /* ── Catalog grid ── */
      .catalog-readonly-banner {
        display: flex;
        align-items: flex-start;
        gap: var(--space-3);
        padding: var(--space-4);
        background: var(--color-info-bg);
        border: 1px solid var(--color-info-border);
        border-radius: var(--radius-lg);
      }

      .catalog-readonly-banner mat-icon {
        color: var(--color-info);
        flex-shrink: 0;
      }

      .catalog-readonly-banner p {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-info-text);
        line-height: 1.6;
      }

      .catalog-toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-4);
        flex-wrap: wrap;
      }

      .catalog-count-summary {
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-semibold);
        white-space: nowrap;
      }

      .catalog-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: var(--space-4);
      }

      .catalog-card {
        border-radius: var(--radius-xl);
        border: 1px solid var(--color-border-light);
        box-shadow: var(--shadow-sm);
        display: flex;
        flex-direction: column;
        transition: box-shadow var(--transition-fast), border-color var(--transition-fast), transform var(--transition-fast);
      }

      .catalog-card:hover {
        box-shadow: var(--shadow-md);
        border-color: var(--color-primary-300);
        transform: translateY(-2px);
      }

      ::ng-deep .catalog-card > .mat-mdc-card-header {
        align-items: center;
        gap: var(--space-3);
      }

      ::ng-deep .catalog-card .mat-mdc-card-header-text {
        margin: 0 !important;
      }

      .catalog-icon-box {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 44px;
        height: 44px;
        border-radius: var(--radius-lg);
        background: var(--color-primary-50);
        color: var(--color-primary-700);
        flex-shrink: 0;
      }

      ::ng-deep .catalog-card .mat-mdc-card-title {
        font-size: var(--font-size-body) !important;
        font-weight: var(--font-weight-bold) !important;
      }

      ::ng-deep .catalog-card .mat-mdc-card-subtitle {
        font-size: var(--font-size-caption) !important;
        color: var(--color-on-surface-secondary) !important;
      }

      .catalog-card-actions {
        padding: 0 var(--space-4) var(--space-3) !important;
        margin-top: auto;
      }

      ::ng-deep .catalog-card-actions .mat-mdc-button {
        color: var(--color-primary-700) !important;
      }

      /* ── Audit ── */
      .tab-intro {
        margin: 0 0 var(--space-4);
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        line-height: 1.6;
        max-width: 760px;
      }

      /* ── Responsive ── */
      @media (max-width: 768px) {
        .form-grid--2col {
          grid-template-columns: 1fr;
        }

        .toolbar {
          flex-direction: column;
          align-items: stretch;
        }

        .search-field {
          max-width: none;
        }

        .catalog-grid {
          grid-template-columns: 1fr;
        }

        .data-table th:nth-child(2),
        .data-table td:nth-child(2) {
          display: none;
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdministrationHomeComponent {
  private readonly usersApi = inject(UsersApiService);
  private readonly catalogsApi = inject(CatalogsApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly userSearch = this.fb.control('');
  readonly catalogSearch = this.fb.control('');

  readonly users = signal<UserDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);

  readonly availableRoles = ['Administrador', 'Líder de Gobernanza', 'Gestor Departamental'];
  readonly catalogs = signal<CatalogEntry[]>([]);
  readonly catalogsLoading = signal(true);

  readonly filteredUsers = signal<UserDto[]>([]);
  readonly filteredCatalogs = signal<CatalogEntry[]>([]);

  constructor() {
    this.loadUsers();
    this.loadCatalogs();

    this.userSearch.valueChanges.subscribe((t) => this.applyUserFilter(t ?? ''));
    this.catalogSearch.valueChanges.subscribe((t) => this.applyCatalogFilter(t ?? ''));
  }

  openCreate(): void {
    this.openUserDialog(null);
  }

  openEdit(user: UserDto): void {
    this.openUserDialog(user);
  }

  resetPassword(user: UserDto): void {
    this.confirmDialog
      .confirm({
        title: 'Restablecer contraseña',
        message: `Se generará una nueva contraseña temporal para ${user.email}. El usuario deberá cambiarla en su próximo inicio de sesión.`,
        confirmLabel: 'Restablecer',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (!confirmed) return;

        this.saving.set(true);
        this.usersApi
          .resetPassword(user.id)
          .pipe(finalize(() => this.saving.set(false)))
          .subscribe({
            next: (result) => {
              this.dialog.open(ResetPasswordDialogComponent, {
                data: result,
                width: '480px',
                autoFocus: false,
                restoreFocus: true,
                panelClass: 'reset-password-dialog-panel'
              });
            },
            error: (err: HttpErrorResponse) =>
              this.snackBar.open(extractErrorMessage(err, 'Error al restablecer la contraseña'), 'Cerrar', { duration: 5000 })
          });
      });
  }

  manageCatalog(catalog: CatalogEntry): void {
    this.dialog
      .open(CatalogManageDialogComponent, {
        data: { definition: catalog.definition, childDefinition: catalog.childDefinition },
        width: '600px',
        autoFocus: false,
        restoreFocus: true,
        panelClass: 'catalog-manage-dialog-panel'
      })
      .afterClosed()
      .subscribe(() => this.loadCatalogs());
  }

  private openUserDialog(user: UserDto | null): void {
    const dialogRef = this.dialog.open(UserFormDialogComponent, {
      data: { user, availableRoles: this.availableRoles },
      width: '640px',
      autoFocus: false,
      restoreFocus: true,
      panelClass: 'user-form-dialog-panel'
    });

    dialogRef.afterClosed().subscribe((result?: UserFormDialogResult) => {
      if (!result) return;

      this.saving.set(true);
      const request$ =
        result.mode === 'create' ? this.usersApi.create(result.request) : this.usersApi.update(result.id, result.request);

      request$.pipe(finalize(() => this.saving.set(false))).subscribe({
        next: () => {
          this.snackBar.open(result.mode === 'create' ? 'Usuario creado' : 'Usuario actualizado', 'Cerrar', { duration: 3000 });
          this.loadUsers();
        },
        error: (err: HttpErrorResponse) =>
          this.snackBar.open(
            extractErrorMessage(err, result.mode === 'create' ? 'Error al crear el usuario' : 'Error al actualizar el usuario'),
            'Cerrar',
            { duration: 5000 }
          )
      });
    });
  }

  private loadUsers(): void {
    this.loading.set(true);
    this.usersApi
      .getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (users) => {
          this.users.set(users);
          this.applyUserFilter(this.userSearch.value ?? '');
        },
        error: (err: HttpErrorResponse) =>
          this.snackBar.open(extractErrorMessage(err, 'Error al cargar usuarios'), 'Cerrar', { duration: 5000 })
      });
  }

  private loadCatalogs(): void {
    this.catalogsLoading.set(true);
    this.catalogsApi.getCatalogDefinitions().subscribe({
      next: (definitions) => {
        const topLevelDefinitions = definitions.filter((d) => !d.hasParent);
        const childByParentKey = new Map(definitions.filter((d) => d.hasParent).map((d) => [d.parentKey, d]));

        const entries: CatalogEntry[] = topLevelDefinitions.map((definition) => ({
          definition,
          childDefinition: childByParentKey.get(definition.key),
          itemCount: 0
        }));

        this.catalogs.set(entries);
        this.applyCatalogFilter(this.catalogSearch.value ?? '');

        entries.forEach((entry) => {
          this.catalogsApi.getCatalogItems(entry.definition.key).subscribe({
            next: (items) => {
              entry.itemCount = items.length;
              this.catalogs.set([...this.catalogs()]);
              this.applyCatalogFilter(this.catalogSearch.value ?? '');
            },
            error: () => {
              entry.itemCount = 0;
              this.catalogs.set([...this.catalogs()]);
            }
          });
        });

        this.catalogsLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.catalogsLoading.set(false);
        this.snackBar.open(extractErrorMessage(err, 'Error al cargar los catálogos'), 'Cerrar', { duration: 5000 });
      }
    });
  }

  private applyUserFilter(term: string): void {
    const q = term.toLowerCase();
    this.filteredUsers.set(
      this.users().filter(
        (u) => u.email.toLowerCase().includes(q) || (u.displayName ?? '').toLowerCase().includes(q)
      )
    );
  }

  private applyCatalogFilter(term: string): void {
    const q = term.toLowerCase();
    this.filteredCatalogs.set(this.catalogs().filter((c) => c.definition.displayName.toLowerCase().includes(q)));
  }
}
