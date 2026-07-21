import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { filter, map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AppRoles } from '../core/constants/app-roles.constant';
import { AuthTokenService } from '../core/services/auth-token.service';
import { ConfirmDialogService } from '../shared/services/confirm-dialog.service';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  /** Roles autorizados para ver el ítem. Vacío o ausente = visible para cualquier rol autenticado. */
  roles?: string[];
}

const NAV_ITEMS: NavItem[] = [
  { path: '/dashboard', label: 'Panel', icon: 'dashboard' },
  {
    path: '/governance',
    label: 'Gobernanza',
    icon: 'account_balance',
    roles: [AppRoles.Administrador, AppRoles.LiderGobernanza, AppRoles.GestorDepartamental]
  },
  {
    path: '/indicators',
    label: 'Indicadores',
    icon: 'bar_chart',
    roles: [AppRoles.Administrador, AppRoles.LiderGobernanza]
  },
  {
    path: '/imports',
    label: 'Importaciones',
    icon: 'upload_file',
    roles: [AppRoles.Administrador, AppRoles.LiderGobernanza, AppRoles.GestorDepartamental]
  },
  {
    path: '/reports',
    label: 'Reportes',
    icon: 'description',
    roles: [AppRoles.Administrador, AppRoles.LiderGobernanza]
  },
  {
    path: '/administration',
    label: 'Administración',
    icon: 'settings',
    roles: [AppRoles.Administrador]
  }
];

const COLLAPSED_KEY = 'gobernanza_sidebar_collapsed';

@Component({
  selector: 'app-app-shell-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    MatMenuModule,
    MatSidenavModule,
    MatToolbarModule,
    MatTooltipModule
  ],
  template: `
    <mat-sidenav-container class="shell-container" [class.shell--collapsed]="collapsed()">
      <mat-sidenav
        #drawer
        class="shell-sidenav"
        [mode]="isMobile() ? 'over' : 'side'"
        [opened]="!isMobile()"
        [class.shell-sidenav--collapsed]="collapsed()"
      >
        <div class="sidenav-inner">
          <header class="sidenav-brand">
            <div class="brand-icon">
              <img src="img/logo-pnmc.png" alt="Logo PNMC" class="brand-logo-img" />
            </div>
            <span class="brand-text" [class.brand-text--hidden]="collapsed()">
              Gobernanza PNMC
            </span>
          </header>

          <nav class="sidenav-nav">
            @for (item of navItems(); track item.path) {
              <a
                class="nav-link"
                [routerLink]="item.path"
                routerLinkActive="nav-link--active"
                [routerLinkActiveOptions]="{ exact: true }"
                [matTooltip]="collapsed() ? item.label : ''"
                [matTooltipPosition]="'right'"
                [attr.aria-label]="item.label"
              >
                <mat-icon class="nav-icon" [class.nav-icon--active]="isRouteActive(item.path)">
                  {{ item.icon }}
                </mat-icon>
                <span class="nav-label" [class.nav-label--hidden]="collapsed()">
                  {{ item.label }}
                </span>
              </a>
            }
          </nav>

          <div class="sidenav-spacer"></div>

          <button
            class="sidenav-collapse-btn"
            (click)="toggleCollapse()"
            [attr.aria-label]="collapsed() ? 'Expandir menú' : 'Colapsar menú'"
          >
            <mat-icon class="collapse-icon" [class.collapse-icon--rotated]="collapsed()">
              chevron_left
            </mat-icon>
            <span class="collapse-label" [class.collapse-label--hidden]="collapsed()">
              Colapsar menú
            </span>
          </button>
        </div>
      </mat-sidenav>

      <mat-sidenav-content class="shell-content">
        <mat-toolbar class="shell-header">
          <div class="header-left">
            @if (isMobile()) {
              <button
                mat-icon-button
                class="header-menu-btn"
                (click)="drawer.toggle()"
                aria-label="Abrir menú de navegación"
              >
                <mat-icon>menu</mat-icon>
              </button>
            }

            <nav class="breadcrumb" aria-label="Navegación jerárquica">
              @for (crumb of breadcrumbs(); track crumb.path; let last = $last) {
                @if (!last) {
                  <a class="breadcrumb-link" [routerLink]="crumb.path">{{ crumb.label }}</a>
                  <mat-icon class="breadcrumb-sep">chevron_right</mat-icon>
                } @else {
                  <span class="breadcrumb-current">{{ crumb.label }}</span>
                }
              }
            </nav>
          </div>

          <div class="header-right">
            <!-- Quick Actions / Search Bar -->
            @if (!isMobile()) {
              <div class="header-search">
                <mat-icon class="search-icon">search</mat-icon>
                <input
                  #searchInput
                  type="text"
                  placeholder="Buscar un módulo..."
                  class="search-input"
                  aria-label="Búsqueda rápida"
                  [value]="searchQuery()"
                  (input)="onSearchInput($event)"
                  (focus)="searchOpen.set(true)"
                  (keydown.escape)="closeSearch()"
                  (keydown.enter)="goToFirstResult()"
                />
                @if (searchQuery()) {
                  <button mat-icon-button class="search-clear" type="button" (click)="clearSearch()" aria-label="Limpiar búsqueda">
                    <mat-icon>close</mat-icon>
                  </button>
                } @else {
                  <span class="search-shortcut">Ctrl K</span>
                }

                @if (searchOpen() && searchQuery()) {
                  <div class="search-results">
                    @if (searchResults().length > 0) {
                      @for (item of searchResults(); track item.path) {
                        <a class="search-result-item" [routerLink]="item.path" (click)="closeSearch()">
                          <mat-icon>{{ item.icon }}</mat-icon>
                          <span>{{ item.label }}</span>
                        </a>
                      }
                    } @else {
                      <div class="search-no-results">
                        <mat-icon>search_off</mat-icon>
                        <span>Sin resultados para "{{ searchQuery() }}"</span>
                      </div>
                    }
                  </div>
                }
              </div>
            }

            <button
              mat-icon-button
              class="header-action notification-btn"
              aria-label="Notificaciones"
              matTooltip="Notificaciones"
              [matMenuTriggerFor]="notificationsMenu"
            >
              <mat-icon>notifications_none</mat-icon>
            </button>

            <mat-menu #notificationsMenu="matMenu" class="notifications-menu-panel" xPosition="before">
              <div class="notif-menu-header">
                <strong>Notificaciones</strong>
              </div>
              <mat-divider />
              <div class="notif-empty">
                <mat-icon>notifications_none</mat-icon>
                <p>No tienes notificaciones nuevas</p>
              </div>
            </mat-menu>

            <button
              class="header-user"
              [matMenuTriggerFor]="userMenu"
              aria-label="Menú de usuario"
            >
              <div class="user-avatar">
                {{ authService.userInitials }}
              </div>
              @if (!isMobile()) {
                <span class="user-name">{{ authService.userDisplayName }}</span>
              }
              @if (!isMobile()) {
                <mat-icon class="user-arrow">arrow_drop_down</mat-icon>
              }
            </button>

            <mat-menu #userMenu="matMenu" class="user-menu-panel" xPosition="before">
              <div class="user-menu-header">
                <div class="user-menu-avatar">{{ authService.userInitials }}</div>
                <div class="user-menu-info">
                  <strong>{{ authService.userDisplayName }}</strong>
                  <span>{{ authService.userSession()?.email }}</span>
                </div>
              </div>
              <mat-divider />
              <button mat-menu-item (click)="logout()">
                <mat-icon>logout</mat-icon>
                <span>Cerrar sesión</span>
              </button>
            </mat-menu>
          </div>
        </mat-toolbar>

        <main class="shell-main">
          <router-outlet />
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100vh;
        overflow: hidden;
      }
      .shell-container {
        height: 100vh;
        background: var(--color-background);
      }
      .shell-sidenav {
        width: var(--sidebar-width);
        background: #ffffff;
        border-right: 1px solid var(--color-border-light);
        border-radius: 0;
        transition: width var(--transition-base);
        overflow: hidden;
      }
      .shell-sidenav--collapsed {
        width: var(--sidebar-collapsed-width);
      }
      .sidenav-inner {
        display: flex;
        flex-direction: column;
        height: 100%;
        padding: var(--space-6) var(--space-4);
        transition: padding var(--transition-base);
      }
      .shell-sidenav--collapsed .sidenav-inner {
        padding: var(--space-6) var(--space-2);
      }
      .sidenav-brand {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-1) var(--space-2) var(--space-5);
        min-height: 56px;
        overflow: hidden;
        transition: padding var(--transition-base), justify-content var(--transition-base);
      }
      .shell-sidenav--collapsed .sidenav-brand {
        padding-left: 0;
        padding-right: 0;
        justify-content: center;
      }
      .brand-icon {
        flex-shrink: 0;
        display: flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        border-radius: var(--radius-lg);
        background: var(--color-primary-900);
        padding: 7px;
        box-sizing: border-box;
      }
      .brand-logo-img {
        width: 100%;
        height: 100%;
        object-fit: contain;
      }
      .brand-text {
        font-size: 1.1rem;
        font-weight: var(--font-weight-extrabold);
        color: var(--color-primary-900);
        white-space: nowrap;
        opacity: 1;
        max-width: 200px;
        transition: opacity var(--transition-base), max-width var(--transition-base);
        letter-spacing: -0.01em;
      }
      .brand-text--hidden {
        opacity: 0;
        max-width: 0;
        pointer-events: none;
      }
      .sidenav-nav {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
        padding: var(--space-4) 0;
        flex: 1;
        overflow-y: auto;
        overflow-x: hidden;
      }
      .nav-link {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-3) var(--space-4);
        height: 48px;
        border-radius: var(--radius-xl);
        color: var(--color-on-surface-variant);
        text-decoration: none;
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-semibold);
        cursor: pointer;
        transition: background-color var(--transition-fast), color var(--transition-fast), padding var(--transition-base), justify-content var(--transition-base);
        position: relative;
        white-space: nowrap;
        overflow: hidden;
      }
      .shell-sidenav--collapsed .nav-link {
        justify-content: center;
        padding: 0 !important;
      }
      .nav-link:hover {
        background: var(--color-hover);
        color: var(--color-on-surface);
      }
      .nav-link--active {
        background: var(--color-primary-50);
        color: var(--color-primary-900);
      }
      .nav-link--active::before {
        content: '';
        position: absolute;
        left: 0;
        top: 50%;
        transform: translateY(-50%);
        width: 4px;
        height: 24px;
        background: var(--color-primary-700);
        border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
      }
      .nav-icon {
        flex-shrink: 0;
        transition: color var(--transition-fast);
      }
      .nav-label {
        opacity: 1;
        max-width: 200px;
        transition: opacity var(--transition-base), max-width var(--transition-base);
        overflow: hidden;
        text-overflow: ellipsis;
      }
      .nav-label--hidden {
        opacity: 0;
        max-width: 0;
        pointer-events: none;
      }
      .sidenav-spacer {
        flex: 1;
      }
      .sidenav-collapse-btn {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        padding: var(--space-3) var(--space-4);
        height: 48px;
        border-radius: var(--radius-xl);
        color: var(--color-on-surface-variant);
        font-size: var(--font-size-body-sm);
        cursor: pointer;
        background: none;
        border: none;
        width: 100%;
        text-align: left;
        transition: background-color var(--transition-fast), color var(--transition-fast), padding var(--transition-base), justify-content var(--transition-base);
        overflow: hidden;
      }
      .shell-sidenav--collapsed .sidenav-collapse-btn {
        justify-content: center;
        padding: 0 !important;
      }
      .sidenav-collapse-btn:hover {
        background: var(--color-hover);
        color: var(--color-on-surface);
      }
      .collapse-icon {
        flex-shrink: 0;
        transition: transform var(--transition-base);
      }
      .collapse-icon--rotated {
        transform: rotate(180deg);
      }
      .collapse-label {
        opacity: 1;
        max-width: 200px;
        transition: opacity var(--transition-base), max-width var(--transition-base);
        font-weight: var(--font-weight-semibold);
      }
      .collapse-label--hidden {
        opacity: 0;
        max-width: 0;
        pointer-events: none;
      }
      .shell-header {
        display: flex;
        align-items: center;
        height: var(--header-height);
        padding: 0 var(--space-8);
        background: #ffffff;
        border-bottom: 1px solid var(--color-border-light);
        position: sticky;
        top: 0;
        z-index: var(--z-sticky);
      }
      .header-left {
        display: flex;
        align-items: center;
        gap: var(--space-4);
      }
      .header-menu-btn {
        margin-right: var(--space-2);
      }
      .header-right {
        display: flex;
        align-items: center;
        gap: var(--space-4);
        margin-left: auto;
      }
      .header-search {
        position: relative;
        display: flex;
        align-items: center;
        gap: var(--space-2);
        background: var(--color-surface-container-low);
        border: 1px solid var(--color-border-light);
        border-radius: var(--radius-lg);
        padding: var(--space-1) var(--space-3);
        width: 280px;
        height: 36px;
        transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
      }
      .header-search:focus-within {
        border-color: var(--color-primary-500);
        box-shadow: 0 0 0 2px var(--color-primary-100);
        background: #ffffff;
      }
      .search-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
        color: var(--color-outline);
        flex-shrink: 0;
      }
      .search-input {
        border: none;
        background: transparent;
        font-family: var(--font-family);
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
        outline: none;
        width: 100%;
      }
      .search-shortcut {
        font-size: 10px;
        font-weight: var(--font-weight-bold);
        background: var(--color-surface-container-high);
        color: var(--color-outline);
        border: 1px solid var(--color-border);
        padding: 2px 6px;
        border-radius: var(--radius-sm);
        white-space: nowrap;
      }
      .search-clear.mat-mdc-icon-button {
        width: 24px;
        height: 24px;
        padding: 0;
        flex-shrink: 0;
        color: var(--color-outline);
      }
      ::ng-deep .search-clear.mat-mdc-icon-button .mat-mdc-button-touch-target {
        width: 24px;
        height: 24px;
      }
      .search-clear mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
      }
      .search-results {
        position: absolute;
        top: calc(100% + var(--space-2));
        left: 0;
        right: 0;
        background: #ffffff;
        border: 1px solid var(--color-border-light);
        border-radius: var(--radius-lg);
        box-shadow: var(--shadow-lg);
        padding: var(--space-2);
        z-index: var(--z-dropdown);
        max-height: 320px;
        overflow-y: auto;
      }
      .search-result-item {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        padding: var(--space-2) var(--space-3);
        border-radius: var(--radius-md);
        color: var(--color-on-surface);
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-semibold);
        text-decoration: none;
        transition: background-color var(--transition-fast);
      }
      .search-result-item:hover {
        background: var(--color-hover);
        text-decoration: none;
      }
      .search-result-item mat-icon {
        color: var(--color-primary-700);
        flex-shrink: 0;
      }
      .search-no-results {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        padding: var(--space-3);
        color: var(--color-on-surface-secondary);
        font-size: var(--font-size-body-sm);
      }
      .breadcrumb {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        font-size: var(--font-size-body-sm);
        flex-wrap: wrap;
      }
      .breadcrumb-link {
        color: var(--color-on-surface-variant);
        text-decoration: none;
        font-weight: var(--font-weight-semibold);
        &:hover {
          color: var(--color-primary-700);
          text-decoration: underline;
        }
      }
      .breadcrumb-sep {
        font-size: 1rem;
        width: 1rem;
        height: 1rem;
        color: var(--color-border);
      }
      .breadcrumb-current {
        color: var(--color-on-surface);
        font-weight: var(--font-weight-bold);
      }
      .header-action {
        color: var(--color-on-surface-variant);
        transition: color var(--transition-fast);
      }
      .header-action:hover {
        color: var(--color-on-surface);
      }
      .notification-btn {
        position: relative;
      }
      ::ng-deep .notifications-menu-panel.mat-mdc-menu-panel {
        background-color: #ffffff !important;
        border: 1px solid var(--color-border-light) !important;
        box-shadow: var(--shadow-lg) !important;
        border-radius: var(--radius-xl) !important;
        min-width: 280px;
        overflow: hidden;
      }
      .notif-menu-header {
        padding: var(--space-4) var(--space-5);
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface);
      }
      .notif-empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--space-2);
        padding: var(--space-8) var(--space-5);
        text-align: center;
      }
      .notif-empty mat-icon {
        font-size: 2rem;
        width: 2rem;
        height: 2rem;
        color: var(--color-border);
      }
      .notif-empty p {
        margin: 0;
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
      }
      .header-user {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        padding: 4px var(--space-4) 4px 4px;
        border-radius: var(--radius-full);
        color: var(--color-primary-900);
        height: 40px;
        border: 1px solid var(--color-border-light);
        background: var(--color-surface-container-lowest);
        transition: border-color var(--transition-fast), background-color var(--transition-fast), box-shadow var(--transition-fast);
        cursor: pointer;
        outline: none;
        box-sizing: border-box;
      }
      .header-user:hover {
        border-color: var(--color-primary-300);
        background-color: var(--color-primary-50);
        box-shadow: var(--shadow-xs);
      }
      .user-avatar {
        width: 32px;
        height: 32px;
        border-radius: var(--radius-full);
        background: var(--color-primary-900);
        color: white;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: var(--font-size-caption);
        font-weight: 800;
        flex-shrink: 0;
        box-shadow: 0 2px 4px rgba(40, 30, 82, 0.15);
        line-height: 1;
      }
      .user-name {
        font-size: var(--font-size-body-sm);
        font-weight: 700;
        color: var(--color-primary-900);
        max-width: 140px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        line-height: 1;
      }
      .user-arrow {
        font-size: 18px;
        width: 18px;
        height: 18px;
        color: var(--color-outline);
        display: flex;
        align-items: center;
        justify-content: center;
        margin: 0;
        opacity: 0.8;
      }
      ::ng-deep .user-menu-panel.mat-mdc-menu-panel {
        background-color: #ffffff !important;
        border: 1px solid var(--color-border-light) !important;
        box-shadow: var(--shadow-lg) !important;
        border-radius: var(--radius-xl) !important;
        overflow: hidden;
      }
      .user-menu-header {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        padding: var(--space-4) var(--space-5);
        background: var(--color-surface-container-low);
      }
      .user-menu-avatar {
        width: 44px;
        height: 44px;
        border-radius: var(--radius-full);
        background: var(--color-primary-900);
        color: white;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: var(--font-weight-extrabold);
        font-size: var(--font-size-body);
        flex-shrink: 0;
      }
      .user-menu-info {
        display: flex;
        flex-direction: column;
        font-size: var(--font-size-body-sm);
        line-height: 1.4;
      }
      .user-menu-info strong {
        color: var(--color-on-surface);
        font-weight: var(--font-weight-bold);
      }
      .user-menu-info span {
        color: var(--color-on-surface-secondary);
        font-size: var(--font-size-caption);
      }
      ::ng-deep .user-menu-panel .mat-mdc-menu-item {
        padding: var(--space-3) var(--space-5) !important;
        height: auto !important;
        min-height: 48px !important;
      }
      ::ng-deep .user-menu-panel .mat-mdc-menu-item .mat-icon {
        color: var(--color-outline) !important;
        margin-right: var(--space-3) !important;
      }
      ::ng-deep .user-menu-panel .mat-mdc-menu-item:hover {
        background-color: var(--color-hover) !important;
      }
      .shell-main {
        padding: var(--space-8);
        min-height: calc(100vh - var(--header-height));
        max-width: var(--content-max-width);
        margin: 0 auto;
      }
      ::ng-deep .shell-header.mat-toolbar {
        --mat-toolbar-container-background-color: #ffffff;
        --mat-toolbar-container-text-color: var(--color-on-surface);
      }
      ::ng-deep .shell-sidenav .mat-drawer-inner-container {
        overflow-x: hidden;
      }
      @media (max-width: 768px) {
        .shell-sidenav {
          width: var(--sidebar-width);
          border-radius: 0 var(--radius-xl) var(--radius-xl) 0;
        }
        .shell-header {
          padding: 0 var(--space-4);
        }
        .shell-main {
          padding: var(--space-6);
        }
      }
      @media (max-width: 480px) {
        .shell-header {
          padding: 0 var(--space-4);
        }
        .shell-main {
          padding: var(--space-4);
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppShellLayoutComponent implements OnInit {
  readonly authService = inject(AuthTokenService);
  private readonly router = inject(Router);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly route = inject(ActivatedRoute);
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('searchInput') private readonly searchInputRef?: ElementRef<HTMLInputElement>;

  readonly collapsed = signal(this.loadCollapsed());
  readonly isMobile = signal(false);
  readonly breadcrumbs = signal<{ label: string; path: string }[]>([]);

  readonly navItems = computed<NavItem[]>(() =>
    NAV_ITEMS.filter((item) => !item.roles || this.authService.hasAnyRole(item.roles))
  );

  readonly searchQuery = signal('');
  readonly searchOpen = signal(false);

  readonly searchResults = computed<NavItem[]>(() => {
    const query = this.searchQuery().trim().toLowerCase();
    if (!query) return [];
    return this.navItems().filter((item) => item.label.toLowerCase().includes(query));
  });

  ngOnInit(): void {
    this.breakpointObserver
      .observe([Breakpoints.XSmall, Breakpoints.Small])
      .pipe(
        map((result) => result.matches),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((mobile) => {
        this.isMobile.set(mobile);
        if (mobile) {
          this.collapsed.set(false);
        }
      });

    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.buildBreadcrumbs();
        this.searchQuery.set('');
        this.searchOpen.set(false);
      });

    this.buildBreadcrumbs();
  }

  toggleCollapse(): void {
    this.collapsed.update((v) => {
      const next = !v;
      localStorage.setItem(COLLAPSED_KEY, String(next));
      return next;
    });
  }

  logout(): void {
    this.confirmDialog
      .confirm({
        title: 'Cerrar sesión',
        message: '¿Seguro que deseas cerrar tu sesión actual?',
        confirmLabel: 'Cerrar sesión',
        icon: 'logout',
        variant: 'danger'
      })
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.authService.clear();
        void this.router.navigateByUrl('/auth/login');
      });
  }

  isRouteActive(path: string): boolean {
    return this.router.isActive(path, {
      paths: 'exact',
      queryParams: 'ignored',
      fragment: 'ignored',
      matrixParams: 'ignored'
    });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    this.searchOpen.set(true);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.searchInputRef?.nativeElement.focus();
  }

  closeSearch(): void {
    this.searchOpen.set(false);
  }

  goToFirstResult(): void {
    const [firstResult] = this.searchResults();
    if (!firstResult) return;
    void this.router.navigateByUrl(firstResult.path);
    this.searchQuery.set('');
    this.closeSearch();
  }

  @HostListener('document:mousedown', ['$event'])
  onDocumentMouseDown(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.header-search')) {
      this.searchOpen.set(false);
    }
  }

  @HostListener('document:keydown', ['$event'])
  onGlobalKeydown(event: KeyboardEvent): void {
    const isShortcut = (event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k';
    if (!isShortcut) return;
    event.preventDefault();
    this.searchOpen.set(true);
    this.searchInputRef?.nativeElement.focus();
  }

  private buildBreadcrumbs(): void {
    const crumbs: { label: string; path: string }[] = [];
    let route: ActivatedRoute | null = this.route.firstChild;
    let url = '';

    while (route) {
      const data = route.snapshot.data;
      if (data['breadcrumb']) {
        url += '/' + route.snapshot.url.map((seg) => seg.path).join('/');
        crumbs.push({ label: data['breadcrumb'], path: url || '/' });
      }
      route = route.firstChild;
    }

    this.breadcrumbs.set(crumbs);
  }

  private loadCollapsed(): boolean {
    try {
      return localStorage.getItem(COLLAPSED_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
