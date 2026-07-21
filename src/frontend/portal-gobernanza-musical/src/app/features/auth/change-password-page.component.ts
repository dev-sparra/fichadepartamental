import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthApiService } from '../../core/services/auth-api.service';
import { AuthTokenService } from '../../core/services/auth-token.service';
import { extractErrorMessage } from '../../shared/utils/extract-error-message.util';

interface PasswordRequirement {
  label: string;
  met: boolean;
}

@Component({
  selector: 'app-change-password-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  template: `
    <main class="login-layout">
      <section class="login-brand" aria-label="Identidad institucional">
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

      <section class="login-form-section" aria-label="Cambio de contraseña">
        <header class="mobile-brand-header">
          <div class="mobile-logo">
            <img src="img/logo-pnmc.png" alt="Logo PNMC" class="mobile-logo-img" />
          </div>
          <span class="mobile-app-title">Componente de Gobernanza</span>
        </header>

        <div class="login-form-wrapper">
          <div class="login-form-header">
            <div class="change-password-icon">
              <mat-icon>lock_reset</mat-icon>
            </div>
            <h2 class="login-title">Cambio de contraseña</h2>
            <p class="login-subtitle">
              Por seguridad, define una contraseña nueva antes de continuar
              con tu sesión en el portal.
            </p>
          </div>

          @if (errorMessage()) {
            <div class="login-alert login-alert--error" role="alert">
              <mat-icon class="login-alert-icon">error_outline</mat-icon>
              <span class="login-alert-text">{{ errorMessage() }}</span>
            </div>
          }

          <form class="login-form" [formGroup]="form" (ngSubmit)="submit()">
            <mat-form-field appearance="outline" class="login-field">
              <mat-label>Contraseña actual</mat-label>
              <input
                [type]="showCurrent() ? 'text' : 'password'"
                matInput
                formControlName="currentPassword"
                autocomplete="current-password"
                placeholder="Ingresa tu contraseña actual"
              />
              <mat-icon matPrefix>lock_outline</mat-icon>
              <button
                matSuffix
                mat-icon-button
                type="button"
                (click)="showCurrent.set(!showCurrent())"
                [attr.aria-label]="'Mostrar u ocultar contraseña actual'"
                tabindex="-1"
              >
                <mat-icon>{{ showCurrent() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
              @if (form.controls.currentPassword.touched && form.controls.currentPassword.invalid) {
                <mat-error>Ingresa tu contraseña actual</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="login-field">
              <mat-label>Nueva contraseña</mat-label>
              <input
                [type]="showNew() ? 'text' : 'password'"
                matInput
                formControlName="newPassword"
                autocomplete="new-password"
                placeholder="Mínimo 8 caracteres"
              />
              <mat-icon matPrefix>password</mat-icon>
              <button
                matSuffix
                mat-icon-button
                type="button"
                (click)="showNew.set(!showNew())"
                [attr.aria-label]="'Mostrar u ocultar nueva contraseña'"
                tabindex="-1"
              >
                <mat-icon>{{ showNew() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
              @if (form.controls.newPassword.touched && form.controls.newPassword.invalid) {
                <mat-error>{{ newPasswordError() }}</mat-error>
              }
            </mat-form-field>

            @if (newPassword().length > 0) {
              <div class="strength-meter" aria-hidden="true">
                <div class="strength-bar">
                  <div
                    class="strength-fill"
                    [class.strength-fill--weak]="passwordStrength() === 0"
                    [class.strength-fill--fair]="passwordStrength() === 1"
                    [class.strength-fill--good]="passwordStrength() === 2"
                    [class.strength-fill--strong]="passwordStrength() === 3"
                    [style.width.%]="25 * (passwordStrength() + 1)"
                  ></div>
                </div>
                <span class="strength-label" [class]="'strength-label--' + strengthLabelClass()">{{ strengthLabel() }}</span>
              </div>

              <ul class="requirements-checklist" role="list">
                @for (req of passwordRequirements(); track req.label) {
                  <li class="requirement" [class.requirement--met]="req.met" [class.requirement--unmet]="!req.met">
                    <mat-icon class="requirement-icon">{{ req.met ? 'check_circle' : 'radio_button_unchecked' }}</mat-icon>
                    <span>{{ req.label }}</span>
                  </li>
                }
              </ul>
            }

            <mat-form-field appearance="outline" class="login-field">
              <mat-label>Confirmar nueva contraseña</mat-label>
              <input
                [type]="showConfirm() ? 'text' : 'password'"
                matInput
                formControlName="confirmPassword"
                autocomplete="new-password"
                placeholder="Repite la nueva contraseña"
              />
              <mat-icon matPrefix>lock</mat-icon>
              <button
                matSuffix
                mat-icon-button
                type="button"
                (click)="showConfirm.set(!showConfirm())"
                [attr.aria-label]="'Mostrar u ocultar confirmación'"
                tabindex="-1"
              >
                <mat-icon>{{ showConfirm() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
              @if (confirmPassword().length > 0 && newPassword().length > 0) {
                @if (confirmPassword() === newPassword()) {
                <mat-hint class="match-hint match-hint--ok">
                  <mat-icon>check_circle</mat-icon> Las contraseñas coinciden
                </mat-hint>
                }
              }
              @if (form.controls.confirmPassword.touched && form.controls.confirmPassword.invalid) {
                <mat-error>Las contraseñas no coinciden</mat-error>
              }
            </mat-form-field>

            <div class="security-note">
              <mat-icon class="hint-icon">shield</mat-icon>
              <span>Tu contraseña se cifra y almacena de forma segura. Nunca la compartas con terceros.</span>
            </div>

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
                  <span>Guardando...</span>
                </span>
              } @else {
                <span class="login-loading-content">
                  <mat-icon>check_circle_outline</mat-icon>
                  <span>Cambiar contraseña</span>
                </span>
              }
            </button>
          </form>
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
        gap: var(--space-6);
      }

      .login-form-header {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        gap: var(--space-2);
      }

      .change-password-icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 56px;
        height: 56px;
        border-radius: var(--radius-full);
        background: var(--color-primary-50);
        color: var(--color-primary-700);
        border: 1px solid var(--color-primary-100);
        margin-bottom: var(--space-1);
      }

      .change-password-icon mat-icon {
        font-size: 2rem;
        width: 2rem;
        height: 2rem;
      }

      .login-title {
        font-size: var(--font-size-h3);
        font-weight: var(--font-weight-extrabold);
        color: var(--color-on-surface);
        letter-spacing: -0.01em;
        margin: 0;
      }

      .login-subtitle {
        font-size: var(--font-size-body-sm);
        color: var(--color-on-surface-secondary);
        line-height: 1.6;
        margin: 0;
        max-width: 320px;
      }

      /* ── Error banner ── */
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

      /* ── Form ── */
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

      /* ── Strength meter ── */
      .strength-meter {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        padding: 0 var(--space-1);
      }

      .strength-bar {
        flex: 1;
        height: 5px;
        background: var(--color-surface-container-high);
        border-radius: var(--radius-full);
        overflow: hidden;
      }

      .strength-fill {
        height: 100%;
        border-radius: var(--radius-full);
        transition: width 0.4s cubic-bezier(0.4, 0, 0.2, 1), background-color 0.3s ease;
      }

      .strength-fill--weak {
        background: var(--color-error);
      }

      .strength-fill--fair {
        background: var(--color-warning);
      }

      .strength-fill--good {
        background: var(--color-info);
      }

      .strength-fill--strong {
        background: var(--color-success);
      }

      .strength-label {
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-bold);
        white-space: nowrap;
        min-width: 70px;
        text-align: right;
      }

      .strength-label--weak { color: var(--color-error); }
      .strength-label--fair { color: var(--color-warning); }
      .strength-label--good { color: var(--color-info); }
      .strength-label--strong { color: var(--color-success); }

      /* ── Requirements checklist ── */
      .requirements-checklist {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-2) var(--space-4);
        list-style: none;
        padding: var(--space-3) var(--space-4);
        margin: 0;
        background: var(--color-surface-container-low);
        border-radius: var(--radius-lg);
        border: 1px solid var(--color-border-light);
      }

      .requirement {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        font-weight: var(--font-weight-medium);
        transition: color var(--transition-fast);
      }

      .requirement--met {
        color: var(--color-success-text);
      }

      .requirement-icon {
        font-size: 1rem;
        width: 1rem;
        height: 1rem;
        flex-shrink: 0;
        transition: color var(--transition-fast);
      }

      .requirement--met .requirement-icon {
        color: var(--color-success);
      }

      .requirement--unmet .requirement-icon {
        color: var(--color-outline);
      }

      /* ── Match hint ── */
      ::ng-deep .match-hint {
        display: flex;
        align-items: center;
        gap: var(--space-1);
        font-size: var(--font-size-caption);
        font-weight: var(--font-weight-semibold);
      }

      ::ng-deep .match-hint mat-icon {
        font-size: 1rem;
        width: 1rem;
        height: 1rem;
      }

      ::ng-deep .match-hint--ok {
        color: var(--color-success);
      }

      /* ── Security note ── */
      .security-note {
        display: flex;
        align-items: flex-start;
        gap: var(--space-2);
        padding: var(--space-3) var(--space-4);
        font-size: var(--font-size-caption);
        color: var(--color-on-surface-secondary);
        line-height: 1.5;
        background: var(--color-surface-container-low);
        border-radius: var(--radius-lg);
        border: 1px solid var(--color-border-light);
      }

      .hint-icon {
        font-size: 1.125rem;
        width: 1.125rem;
        height: 1.125rem;
        flex-shrink: 0;
        margin-top: 0.05rem;
        color: var(--color-primary-600);
      }

      /* ── Submit button ── */
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
        gap: var(--space-2);
      }

      .login-loading-content mat-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
      }

      ::ng-deep .login-spinner .mdc-circular-progress__circle-path {
        stroke: currentColor !important;
      }

      /* ── Animations ── */
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
        .login-app-name {
          font-size: 2rem;
        }
      }

      @media (max-width: 768px) {
        .login-layout {
          grid-template-columns: 1fr;
        }

        .login-brand {
          display: none;
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
      }

      @media (max-width: 480px) {
        .login-form-section {
          padding: var(--space-8) var(--space-4);
        }

        .requirements-checklist {
          flex-direction: column;
          gap: var(--space-2);
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChangePasswordPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authApiService = inject(AuthApiService);
  private readonly authTokenService = inject(AuthTokenService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly showCurrent = signal(false);
  readonly showNew = signal(false);
  readonly showConfirm = signal(false);
  readonly currentYear = new Date().getFullYear();

  readonly form = this.formBuilder.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  });

  readonly newPasswordValue = signal('');
  readonly confirmPasswordValue = signal('');
  readonly currentPasswordValue = signal('');

  constructor() {
    this.form.controls.newPassword.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((v) => this.newPasswordValue.set(v ?? ''));

    this.form.controls.confirmPassword.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((v) => this.confirmPasswordValue.set(v ?? ''));

    this.form.controls.currentPassword.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((v) => this.currentPasswordValue.set(v ?? ''));
  }

  readonly newPassword = computed(() => this.newPasswordValue());
  readonly confirmPassword = computed(() => this.confirmPasswordValue());

  readonly passwordRequirements = computed<PasswordRequirement[]>(() => {
    const pw = this.newPasswordValue();
    return [
      { label: 'Mínimo 8 caracteres', met: pw.length >= 8 },
      { label: 'Una mayúscula', met: /[A-Z]/.test(pw) },
      { label: 'Un número', met: /\d/.test(pw) },
      { label: 'Diferente a la actual', met: pw.length > 0 && pw !== this.currentPasswordValue() }
    ];
  });

  readonly allRequirementsMet = computed(() => this.passwordRequirements().every((r) => r.met));

  readonly passwordStrength = computed(() => {
    const pw = this.newPasswordValue();
    if (pw.length === 0) return 0;
    let score = 0;
    if (pw.length >= 8) score++;
    if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) score++;
    if (/\d/.test(pw) || /[^a-zA-Z0-9]/.test(pw)) score++;
    if (pw.length >= 12 && /[^a-zA-Z0-9]/.test(pw)) score++;
    return Math.min(score, 3);
  });

  readonly strengthLabel = computed(() => {
    const labels = ['Débil', 'Aceptable', 'Buena', 'Fuerte'];
    return labels[this.passwordStrength()];
  });

  readonly strengthLabelClass = computed(() => {
    const classes = ['weak', 'fair', 'good', 'strong'];
    return classes[this.passwordStrength()];
  });

  newPasswordError(): string {
    const ctrl = this.form.controls.newPassword;
    if (ctrl.hasError('required')) return 'La nueva contraseña es obligatoria';
    if (ctrl.hasError('minlength')) return 'Mínimo 8 caracteres';
    return 'Contraseña inválida';
  }

  submit(): void {
    const value = this.form.getRawValue();
    if (value.newPassword !== value.confirmPassword) {
      this.form.controls.confirmPassword.setErrors({ mismatch: true });
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authApiService
      .changePassword({
        currentPassword: value.currentPassword ?? '',
        newPassword: value.newPassword ?? ''
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          this.authTokenService.clearMustChangePassword();
          this.snackBar.open('Contraseña actualizada correctamente', 'Cerrar', { duration: 3000 });
          void this.router.navigateByUrl('/dashboard');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(extractErrorMessage(err, 'Error al cambiar la contraseña'));
        }
      });
  }
}