import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';

import { AuthApiService } from '../../core/services/auth-api.service';
import { AuthTokenService } from '../../core/services/auth-token.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  template: `
    <main class="login-layout">
      <section class="login-brand" aria-label="Identidad institucional">
        <!-- SVG background waves representing music staves and soundwaves -->
        <div class="login-brand-bg" aria-hidden="true">
          <svg class="brand-waves" viewBox="0 0 500 500" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M-50 120 C 120 220, 180 80, 320 180 C 420 250, 480 150, 550 210" stroke="rgba(255, 255, 255, 0.08)" stroke-width="1.5" fill="none"/>
            <path d="M-50 160 C 120 260, 180 120, 320 220 C 420 290, 480 190, 550 250" stroke="rgba(255, 255, 255, 0.06)" stroke-width="1.5" fill="none"/>
            <path d="M-50 200 C 120 300, 180 160, 320 260 C 420 330, 480 230, 550 290" stroke="rgba(255, 255, 255, 0.08)" stroke-width="1.5" fill="none"/>
            <path d="M-50 240 C 120 340, 180 200, 320 300 C 420 370, 480 270, 550 330" stroke="rgba(255, 255, 255, 0.05)" stroke-width="1.5" fill="none"/>
            <path d="M-50 280 C 120 380, 180 240, 320 340 C 420 410, 480 310, 550 370" stroke="rgba(255, 255, 255, 0.07)" stroke-width="1.5" fill="none"/>
            <path d="M-100 420 C 150 320, 220 470, 420 370 C 520 320, 580 420, 680 340" stroke="rgba(255, 255, 255, 0.04)" stroke-width="2.5" fill="none"/>
          </svg>
        </div>

        <div class="login-brand-content">
          <header class="login-logo-container">
            <div class="login-logo">
              <img src="img/logo-pnmc.png" alt="Logo PNMC" class="login-logo-img" />
            </div>
            <div class="login-logo-sub">
              <span>Plan Nacional de Música para la Convivencia</span>
            </div>
          </header>

          <div class="login-brand-text">
            <h1 class="login-app-name">Componente de Gobernanza</h1>
            <p class="login-app-desc">
              Plataforma de gestión del Plan Nacional de Música para la Convivencia
              y el fortalecimiento del ecosistema musical colombiano.
            </p>
          </div>

          <footer class="login-footer-info">
            <span>Ministerio de las Culturas, las Artes y los Saberes</span>
            <span>&copy; {{ currentYear }} Componente de Gobernanza PNMC</span>
          </footer>
        </div>
      </section>

      <section class="login-form-section" aria-label="Formulario de acceso">
        <!-- Top header visible only in mobile/tablets where brand is hidden -->
        <header class="mobile-brand-header">
          <div class="mobile-logo">
            <img src="img/logo-pnmc.png" alt="Logo PNMC" class="mobile-logo-img" />
          </div>
          <span class="mobile-app-title">Componente de Gobernanza</span>
        </header>

        <div class="login-form-wrapper">
          <div class="login-form-header">
            <h2 class="login-title">Iniciar sesión</h2>
            <p class="login-subtitle">Ingresa con tu cuenta</p>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()" class="login-form" novalidate>
            <mat-form-field appearance="outline" class="login-field">
              <mat-label>Correo electrónico</mat-label>
              <input
                matInput
                type="email"
                formControlName="email"
                autocomplete="email"
                placeholder="usuario&#64;mincultura.gov.co"
              />
              <mat-icon matPrefix>mail_outline</mat-icon>
              @if (form.controls.email.invalid && form.controls.email.touched) {
                <mat-error>
                  @if (form.controls.email.errors?.['required']) {
                    El correo es obligatorio
                  } @else if (form.controls.email.errors?.['email']) {
                    Ingresa un correo válido
                  }
                </mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="login-field">
              <mat-label>Contraseña</mat-label>
              <input
                matInput
                [type]="showPassword() ? 'text' : 'password'"
                formControlName="password"
                autocomplete="current-password"
                placeholder="Ingresa tu contraseña"
              />
              <mat-icon matPrefix>lock_outline</mat-icon>
              <button
                matSuffix
                mat-icon-button
                type="button"
                [attr.aria-label]="showPassword() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                (click)="showPassword.set(!showPassword())"
              >
                <mat-icon>{{ showPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
              @if (form.controls.password.invalid && form.controls.password.touched) {
                <mat-error>La contraseña es obligatoria</mat-error>
              }
            </mat-form-field>

            <div class="login-form-options">
              <mat-checkbox class="login-remember" formControlName="rememberMe">
                Recordar sesión
              </mat-checkbox>
              <a class="login-forgot" href="#" (click)="openForgotPassword($event)">
                ¿Olvidaste tu contraseña?
              </a>
            </div>

            @if (errorMessage()) {
              <div class="login-alert login-alert--error" role="alert">
                <mat-icon class="login-alert-icon">error_outline</mat-icon>
                <span class="login-alert-text">{{ errorMessage() }}</span>
              </div>
            }

            <div class="login-submit-container">
              <button
                mat-flat-button
                color="primary"
                type="submit"
                class="login-submit"
                [disabled]="form.invalid || loading()"
              >
                @if (loading()) {
                  <span class="login-loading-content">
                    <mat-progress-spinner diameter="18" mode="indeterminate" class="login-spinner" />
                    <span>Ingresando...</span>
                  </span>
                } @else {
                  <span>Ingresar</span>
                }
              </button>
            </div>
          </form>

          <p class="login-support">
            ¿Necesitas ayuda? Contacta a
            <a href="mailto:simus@mincultura.gov.co">simus&#64;mincultura.gov.co</a>
          </p>
        </div>
      </section>
    </main>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        background: var(--color-background);
      }

      .login-layout {
        display: grid;
        grid-template-columns: 1.2fr 0.8fr;
        min-height: 100vh;
      }

      /* ── Brand panel ── */
      .login-brand {
        background: var(--color-primary-900);
        color: white;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: var(--space-12) var(--space-16);
        position: relative;
        overflow: hidden;
      }

      .login-brand-bg {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        z-index: 0;
        pointer-events: none;
      }

      .brand-waves {
        width: 100%;
        height: 100%;
        object-fit: cover;
        opacity: 0.85;
      }

      .login-brand-content {
        position: relative;
        z-index: 1;
        width: 100%;
        max-width: 480px;
        height: 100%;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        gap: var(--space-12);
      }

      .login-logo-container {
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }

      .login-logo {
        display: inline-flex;
        align-items: center;
        height: 48px;
      }

      .login-logo-img {
        height: 100%;
        width: auto;
        object-fit: contain;
      }

      .login-logo-sub {
        font-size: 0.7rem;
        font-weight: var(--font-weight-bold);
        letter-spacing: 0.15em;
        color: rgba(255, 255, 255, 0.6);
        text-transform: uppercase;
        margin-top: var(--space-1);
      }

      .login-brand-text {
        display: flex;
        flex-direction: column;
        gap: var(--space-4);
      }

      .login-app-name {
        font-size: 2.75rem;
        font-weight: var(--font-weight-extrabold);
        letter-spacing: -0.02em;
        line-height: 1.1;
        color: white;
        margin: 0;
      }

      .login-app-desc {
        font-size: var(--font-size-body);
        line-height: 1.7;
        color: rgba(255, 255, 255, 0.85);
        margin: 0;
      }

      .login-footer-info {
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
        font-size: var(--font-size-label);
        color: rgba(255, 255, 255, 0.45);
        border-top: 1px solid rgba(255, 255, 255, 0.1);
        padding-top: var(--space-4);
      }

      /* ── Form panel ── */
      .login-form-section {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: var(--space-12) var(--space-8);
        background: #ffffff;
        position: relative;
      }

      .mobile-brand-header {
        display: none;
      }

      .login-form-wrapper {
        width: 100%;
        max-width: 380px;
        display: flex;
        flex-direction: column;
        gap: var(--space-8);
      }

      .login-form-header {
        text-align: left;
      }

      .login-title {
        font-size: var(--font-size-h2);
        font-weight: var(--font-weight-extrabold);
        color: var(--color-on-surface);
        letter-spacing: -0.01em;
        margin: 0 0 var(--space-2);
      }

      .login-subtitle {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        margin: 0;
      }

      .login-form {
        display: flex;
        flex-direction: column;
        gap: var(--space-4);
      }

      .login-field {
        width: 100%;
      }

      ::ng-deep .login-field .mat-mdc-form-field-subscript-wrapper {
        margin-bottom: var(--space-1);
      }

      .login-form-options {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-4);
        margin: var(--space-1) 0;
      }

      .login-remember {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
      }

      ::ng-deep .login-remember .mdc-label {
        font-family: var(--font-family) !important;
        font-size: var(--font-size-body-sm) !important;
        color: var(--color-on-surface-secondary) !important;
      }

      .login-forgot {
        font-size: var(--font-size-body-sm);
        color: var(--color-primary-600);
        font-weight: var(--font-weight-semibold);
        text-decoration: none;
        transition: color var(--transition-fast);
      }

      .login-forgot:hover {
        color: var(--color-primary-800);
        text-decoration: underline;
      }

      /* ── Alert ── */
      .login-alert {
        display: flex;
        align-items: flex-start;
        gap: var(--space-3);
        padding: var(--space-4);
        border-radius: var(--radius-lg);
        font-size: var(--font-size-body-sm);
        line-height: var(--line-height-body-sm);
        animation: fadeIn 0.2s cubic-bezier(0.16, 1, 0.3, 1);
      }

      .login-alert--error {
        background: var(--color-error-bg);
        color: var(--color-error-text);
        border: 1px solid var(--color-error-border);
      }

      .login-alert-icon {
        flex-shrink: 0;
        font-size: 1.25rem;
        width: 1.25rem;
        height: 1.25rem;
        color: var(--color-error);
      }

      .login-alert-text {
        font-weight: var(--font-weight-medium);
      }

      /* ── Submit button ── */
      .login-submit-container {
        margin-top: var(--space-2);
      }

      .login-submit {
        width: 100%;
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-bold) !important;
        letter-spacing: 0.02em;
        min-height: 48px;
        border-radius: var(--radius-lg) !important;
        background-color: var(--color-primary-900) !important;
        color: white !important;
        transition: background-color var(--transition-fast), transform var(--transition-fast);
      }

      .login-submit:hover:not([disabled]) {
        background-color: var(--color-primary-800) !important;
      }

      .login-submit:active:not([disabled]) {
        background-color: var(--color-primary-900) !important;
        transform: scale(0.985);
      }

      .login-submit[disabled] {
        background-color: var(--color-disabled-bg) !important;
        color: var(--color-disabled-text) !important;
      }

      .login-loading-content {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: var(--space-3);
      }

      .login-spinner {
        display: inline-flex;
      }

      ::ng-deep .login-spinner .mdc-circular-progress__circle-path {
        stroke: currentColor !important;
      }

      /* ── Support ── */
      .login-support {
        text-align: center;
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        margin: var(--space-4) 0 0;
      }

      .login-support a {
        color: var(--color-primary-600);
        font-weight: var(--font-weight-semibold);
      }

      .login-support a:hover {
        color: var(--color-primary-800);
      }

      @keyframes fadeIn {
        from {
          opacity: 0;
          transform: translateY(-4px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      /* ── Responsive ── */
      @media (max-width: 1024px) {
        .login-layout {
          grid-template-columns: 1fr 1fr;
        }
        .login-brand {
          padding: var(--space-8) var(--space-8);
        }
      }

      @media (max-width: 768px) {
        .login-layout {
          grid-template-columns: 1fr;
        }

        .login-brand {
          display: none; /* Hide brand panel entirely to prevent scrolling */
        }

        .login-form-section {
          padding: var(--space-12) var(--space-6);
        }

        .mobile-brand-header {
          display: flex;
          align-items: center;
          gap: var(--space-3);
          margin-bottom: var(--space-8);
          align-self: flex-start;
          width: 100%;
          max-width: 380px;
          margin-left: auto;
          margin-right: auto;
        }

        .mobile-logo {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          width: 44px;
          height: 44px;
          border-radius: var(--radius-lg);
          background: var(--color-primary-900);
          padding: 8px;
          box-sizing: border-box;
          flex-shrink: 0;
        }

        .mobile-logo-img {
          width: 100%;
          height: 100%;
          object-fit: contain;
        }

        .mobile-app-title {
          font-size: var(--font-size-h4);
          font-weight: var(--font-weight-extrabold);
          color: var(--color-primary-900);
          letter-spacing: -0.01em;
        }

        .login-form-options {
          flex-direction: row;
          align-items: center;
        }
      }

      @media (max-width: 480px) {
        .login-form-section {
          padding: var(--space-8) var(--space-4);
        }

        .login-form-options {
          flex-direction: column;
          align-items: flex-start;
          gap: var(--space-2);
        }

        .login-forgot {
          align-self: flex-end;
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authApiService = inject(AuthApiService);
  private readonly authTokenService = inject(AuthTokenService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly showPassword = signal(false);
  readonly currentYear = new Date().getFullYear();

  readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [false]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    const { email, password, rememberMe } = this.form.getRawValue();

    this.authApiService
      .login({ email: email ?? '', password: password ?? '' })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.authTokenService.setSession(
            response.accessToken,
            response.expiresAtUtc,
            {
              email: response.email,
              displayName: response.displayName,
              roles: response.roles,
              mustChangePassword: response.mustChangePassword
            },
            rememberMe ?? false
          );
          void this.router.navigateByUrl(response.mustChangePassword ? '/auth/change-password' : '/dashboard');
        },
        error: () => {
          this.authTokenService.clear();
          this.errorMessage.set('Credenciales inválidas. Verifica tu correo y contraseña e intenta de nuevo.');
        }
      });
  }

  openForgotPassword(event: Event): void {
    event.preventDefault();
    this.snackBar.open(
      'Contacta al administrador del sistema para restablecer tu contraseña.',
      'Cerrar',
      { duration: 8000, panelClass: 'forgot-password-snackbar' }
    );
  }
}

