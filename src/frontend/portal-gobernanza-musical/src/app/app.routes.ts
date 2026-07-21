import { Routes } from '@angular/router';
import { AppRoles } from './core/constants/app-roles.constant';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { AppShellLayoutComponent } from './layouts/app-shell-layout.component';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES)
  },
  {
    path: '',
    component: AppShellLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        data: { breadcrumb: 'Panel principal' },
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES)
      },
      {
        path: 'governance',
        canActivate: [roleGuard],
        data: {
          breadcrumb: 'Gobernanza',
          roles: [AppRoles.Administrador, AppRoles.LiderGobernanza, AppRoles.GestorDepartamental]
        },
        loadChildren: () => import('./features/governance/governance.routes').then((m) => m.GOVERNANCE_ROUTES)
      },
      {
        path: 'indicators',
        canActivate: [roleGuard],
        data: {
          breadcrumb: 'Indicadores',
          roles: [AppRoles.Administrador, AppRoles.LiderGobernanza]
        },
        loadChildren: () => import('./features/indicators/indicators.routes').then((m) => m.INDICATORS_ROUTES)
      },
      {
        path: 'imports',
        canActivate: [roleGuard],
        data: {
          breadcrumb: 'Importaciones',
          roles: [AppRoles.Administrador, AppRoles.LiderGobernanza, AppRoles.GestorDepartamental]
        },
        loadChildren: () => import('./features/imports/imports.routes').then((m) => m.IMPORTS_ROUTES)
      },
      {
        path: 'reports',
        canActivate: [roleGuard],
        data: {
          breadcrumb: 'Reportes',
          roles: [AppRoles.Administrador, AppRoles.LiderGobernanza]
        },
        loadChildren: () => import('./features/reports/reports.routes').then((m) => m.REPORTS_ROUTES)
      },
      {
        path: 'administration',
        canActivate: [roleGuard],
        data: {
          breadcrumb: 'Administración',
          roles: [AppRoles.Administrador]
        },
        loadChildren: () => import('./features/administration/administration.routes').then((m) => m.ADMINISTRATION_ROUTES)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
