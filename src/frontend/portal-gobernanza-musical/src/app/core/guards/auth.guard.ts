import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthTokenService } from '../services/auth-token.service';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const authTokenService = inject(AuthTokenService);

  if (!authTokenService.hasToken()) {
    return router.createUrlTree(['/auth/login']);
  }

  if (authTokenService.mustChangePassword()) {
    return router.createUrlTree(['/auth/change-password']);
  }

  return true;
};