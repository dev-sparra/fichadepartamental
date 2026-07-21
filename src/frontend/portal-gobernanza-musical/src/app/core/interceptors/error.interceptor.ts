import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { EMPTY, catchError, throwError } from 'rxjs';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AuthTokenService } from '../services/auth-token.service';

// RFC 7807 Problem Details mínimo devuelto por la API.
interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  type?: string;
  instance?: string;
}

function extractProblemMessage(error: HttpErrorResponse): string | null {
  const body = error.error as ProblemDetails | undefined;
  if (!body || typeof body !== 'object') return null;
  return body.detail?.trim() || body.title?.trim() || null;
}

// Centraliza el manejo de errores HTTP para todo el flujo DB → API → Front:
//  - 401: sesión expirada → limpia el token y redirige a /auth/login.
//  - 0 (red/caída del servidor): un único aviso claro en lugar de varios toasts.
//  - Otros códigos (400/403/404/409/5xx): se relanza con el mensaje RFC 7807
//    adjunto (error.error.detalleResumen) para que los manejadores por componente
//    puedan mostrar un texto más útil sin replicar lógica.
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authTokenService = inject(AuthTokenService);
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Token expirado o inválido: no tiene sentido quedarse en la página.
      // Se limpia la sesión y se redirige al login, swallowing el error para
      // no apilar toasts genéricos del componente que ya está saliendo.
      // El endpoint de login mismo devuelve 401 por credenciales inválidas: en
      // ese caso se relanza para que el formulario muestre su propio mensaje.
      const isLoginRequest = req.url.includes('/auth/login');
      if (error.status === 401 && !isLoginRequest) {
        authTokenService.clear();
        snackBar.open('Tu sesión ha expirado. Inicia sesión de nuevo.', 'Cerrar', { duration: 5000 });
        void router.navigate(['/auth', 'login']);
        return EMPTY;
      }

      // status 0: el servidor no responde (caído) o no hay red. Como varias
      // peticiones simultáneas fallarían a la vez, mostramos un solo aviso.
      if (error.status === 0) {
        snackBar.open(
          'No se puede conectar con el servidor. Verifica tu conexión o que la API esté disponible.',
          'Cerrar',
          { duration: 6000 }
        );
        return EMPTY;
      }

      // Para el resto, adjuntamos el mensaje legible del Problem Details RFC 7807
      // (devuelto por ApiExceptionHandler) y relanzamos para que cada componente
      // decida su enfoque y mantenga sus flags (saving/loading) coherentes.
      const detalleResumen = extractProblemMessage(error);
      if (detalleResumen && error.error && typeof error.error === 'object') {
        (error.error as ProblemDetails & { detalleResumen?: string }).detalleResumen = detalleResumen;
      }
      return throwError(() => error);
    })
  );
}