import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';

import { LoginPageComponent } from './login-page.component';
import { ChangePasswordPageComponent } from './change-password-page.component';
import { AuthTokenService } from '../../core/services/auth-token.service';

const changePasswordGuard: CanActivateFn = () => {
  const router = inject(Router);
  const authTokenService = inject(AuthTokenService);

  if (!authTokenService.hasToken()) {
    return router.createUrlTree(['/auth/login']);
  }

  if (!authTokenService.mustChangePassword()) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    component: LoginPageComponent
  },
  {
    path: 'change-password',
    component: ChangePasswordPageComponent,
    canActivate: [changePasswordGuard]
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  }
];