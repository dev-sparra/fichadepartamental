import { HttpErrorResponse } from '@angular/common/http';

// Extrae el mensaje de error para mostrar al usuario, priorizando el detalle
// RFC 7807 que adjunta el interceptor (error.error.detalleResumen / detail /
// title) y, si no existe, usa el mensaje genérico 'fallback' del componente.
export function extractErrorMessage(error: HttpErrorResponse, fallback: string): string {
  const body = error?.error as Record<string, unknown> | undefined;
  if (body && typeof body === 'object') {
    const detalleResumen = (body['detalleResumen'] as string | undefined)?.trim();
    if (detalleResumen) return detalleResumen;

    const detail = (body['detail'] as string | undefined)?.trim();
    if (detail) return detail;

    const title = (body['title'] as string | undefined)?.trim();
    if (title) return title;
  }
  return fallback;
}