import { DATE_PIPE_DEFAULT_OPTIONS } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { routes } from './app.routes';
import { providePortalDateAdapter } from './shared/utils/portal-date-adapter';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    // Selector de fecha con formato colombiano (dd/mm/aaaa), igual que el archivo oficial:
    // la fecha se puede elegir en el calendario o escribir directamente en el campo.
    providePortalDateAdapter(),
    // Colombia no observa horario de verano: todas las fechas se muestran en hora de Bogotá (UTC-5)
    // sin importar la zona horaria del navegador del usuario.
    { provide: DATE_PIPE_DEFAULT_OPTIONS, useValue: { timezone: '-0500' } }
  ]
};
