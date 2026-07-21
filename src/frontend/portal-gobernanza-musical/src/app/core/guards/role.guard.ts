import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthTokenService } from '../services/auth-token.service';

export const roleGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const authTokenService = inject(AuthTokenService);

  const allowedRoles = (route.data['roles'] as string[] | undefined) ?? [];
  if (authTokenService.hasAnyRole(allowedRoles)) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
