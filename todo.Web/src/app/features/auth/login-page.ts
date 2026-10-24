import { ApiError } from '../../core/errors/api-error';
import { ApiErrorService } from '../../core/errors/api-error.service';
import { ApiErrors } from '../../shared/errors/api-errors';
import {
  Component,
  ElementRef,
  OnInit,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { requiredTrimmed } from '../../shared/forms/form-validators';
import { FormFieldError } from '../../shared/forms/form-field-error';
import { Router } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { AuthResult } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { GoogleIdentityService } from './google-identity.service';
import { AUTH_VALIDATION_LIMITS, isRegistrationPasswordValid } from './registration-validation';

type LoginMode = 'login' | 'register';
type SignInMethod = 'password' | 'google';

@Component({
  selector: 'app-login-page',
  imports: [ApiErrors, ReactiveFormsModule, FormFieldError, NzButtonModule, NzInputModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage implements OnInit {
  protected readonly validationLimits = AUTH_VALIDATION_LIMITS;
  readonly passwordPolicyMessage = `Şifre en az ${AUTH_VALIDATION_LIMITS.passwordMinimumLength} karakter olmalı, harf ve rakam içermeli.`;
  private readonly auth = inject(AuthService);
  private readonly googleIdentity = inject(GoogleIdentityService);
  private readonly router = inject(Router);
  private readonly googleButtonContainer = viewChild<ElementRef<HTMLElement>>('googleButton');

  private readonly errors = inject(ApiErrorService);
  readonly apiError = signal<ApiError | null>(null);
  readonly mode = signal<LoginMode>('login');
  readonly isSubmitting = signal(false);
  readonly googleStatus = signal<'loading' | 'ready' | 'disabled' | 'unavailable'>('loading');
  private readonly googleClientId = signal<string | null>(null);

  readonly form = inject(FormBuilder).nonNullable.group({
    email: [
      '',
      [
        requiredTrimmed,
        Validators.email,
        Validators.maxLength(AUTH_VALIDATION_LIMITS.emailMaximumLength),
      ],
    ],
    displayName: ['', Validators.maxLength(AUTH_VALIDATION_LIMITS.displayNameMaximumLength)],
    password: [
      '',
      [Validators.required, Validators.maxLength(AUTH_VALIDATION_LIMITS.passwordMaximumLength)],
    ],
  });

  constructor() {
    effect(() => {
      const container = this.googleButtonContainer()?.nativeElement;
      const clientId = this.googleClientId();

      if (container === undefined || clientId === null) {
        return;
      }

      untracked(() => void this.renderGoogleButton(container, clientId));
    });
  }

  ngOnInit(): void {
    void this.setUpGoogleSignIn();
  }

  switchMode(mode: LoginMode): void {
    this.mode.set(mode);
    this.form.controls.displayName.setValidators(
      mode === 'register'
        ? [requiredTrimmed, Validators.maxLength(AUTH_VALIDATION_LIMITS.displayNameMaximumLength)]
        : [Validators.maxLength(AUTH_VALIDATION_LIMITS.displayNameMaximumLength)],
    );
    this.form.controls.password.setValidators([
      Validators.required,
      Validators.maxLength(AUTH_VALIDATION_LIMITS.passwordMaximumLength),
      ...(mode === 'register'
        ? [
            (control: import('@angular/forms').AbstractControl) =>
              !control.value || isRegistrationPasswordValid(control.value)
                ? null
                : { passwordPolicy: true },
          ]
        : []),
    ]);
    this.form.controls.displayName.updateValueAndValidity();
    this.form.controls.password.updateValueAndValidity();
    this.form.markAsUntouched();
    this.apiError.set(null);
  }

  async submit(): Promise<void> {
    if (this.isSubmitting()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const { email: rawEmail, displayName, password } = this.form.getRawValue();
    const email = rawEmail.trim();

    if (this.mode() === 'login') {
      await this.signIn(this.auth.login({ email, password }), 'password');
    } else {
      await this.signIn(
        this.auth.register({ email, displayName: displayName.trim(), password }),
        'password',
      );
    }
  }

  private async setUpGoogleSignIn(): Promise<void> {
    try {
      const config = await firstValueFrom(this.auth.getAuthConfig());

      if (config.googleClientId === null) {
        this.googleStatus.set('disabled');
        return;
      }

      this.googleClientId.set(config.googleClientId);
      this.googleStatus.set('ready');
    } catch {
      this.googleStatus.set('unavailable');
    }
  }

  private async renderGoogleButton(container: HTMLElement, clientId: string): Promise<void> {
    try {
      await this.googleIdentity.renderButton(container, clientId, (idToken) => {
        void this.signIn(this.auth.loginWithGoogle(idToken), 'google');
      });
    } catch {
      this.googleStatus.set('unavailable');
    }
  }

  private async signIn(request: Observable<AuthResult>, method: SignInMethod): Promise<void> {
    this.isSubmitting.set(true);
    this.apiError.set(null);

    try {
      await firstValueFrom(request);
      await this.router.navigate(['/']);
    } catch (error: unknown) {
      const normalized = this.errors.normalize(error);
      const message =
        normalized.status === 401
          ? method === 'google'
            ? 'Google ile giriş doğrulanamadı.'
            : 'E-posta adresi veya şifre hatalı.'
          : normalized.status === 409
            ? 'Bu e-posta adresiyle zaten bir hesap var. Giriş yapmayı dene; hesabını Google ile açtıysan "Google ile devam et" butonunu kullan.'
            : normalized.message;
      this.errors.applyFieldErrors(this.form, normalized);
      this.apiError.set(
        new ApiError(normalized.status, message, normalized.problemType, normalized.fieldErrors),
      );
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
