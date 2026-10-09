import { HttpErrorResponse } from '@angular/common/http';
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
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { AuthResult } from '../../models/auth.models';
import { AuthService } from '../../services/auth.service';
import { GoogleIdentityService } from '../../services/google-identity.service';

type LoginMode = 'login' | 'register';
type SignInMethod = 'password' | 'google';

const MINIMUM_PASSWORD_LENGTH = 8;

@Component({
  selector: 'app-login-page',
  imports: [FormsModule, NzButtonModule, NzInputModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly googleIdentity = inject(GoogleIdentityService);
  private readonly router = inject(Router);
  private readonly googleButtonContainer = viewChild<ElementRef<HTMLElement>>('googleButton');

  readonly mode = signal<LoginMode>('login');
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly googleStatus = signal<'loading' | 'ready' | 'disabled' | 'unavailable'>('loading');
  private readonly googleClientId = signal<string | null>(null);

  email = '';
  displayName = '';
  password = '';

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
    this.errorMessage.set(null);
  }

  async submit(): Promise<void> {
    const validationError = this.validateForm();

    if (validationError !== null) {
      this.errorMessage.set(validationError);
      return;
    }

    const email = this.email.trim();

    if (this.mode() === 'login') {
      await this.signIn(this.auth.login({ email, password: this.password }), 'password');
    } else {
      await this.signIn(
        this.auth.register({ email, displayName: this.displayName.trim(), password: this.password }),
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
    this.errorMessage.set(null);

    try {
      await firstValueFrom(request);
      await this.router.navigate(['/']);
    } catch (error: unknown) {
      this.errorMessage.set(this.describeError(error, method));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private validateForm(): string | null {
    const email = this.email.trim();

    if (email.length === 0 || !email.includes('@')) {
      return 'Geçerli bir e-posta adresi gir.';
    }

    if (this.mode() === 'register' && this.displayName.trim().length === 0) {
      return 'Adını boş bırakma.';
    }

    if (this.password.length === 0) {
      return 'Şifreni gir.';
    }

    if (this.mode() === 'register') {
      const hasLetter = /\p{L}/u.test(this.password);
      const hasDigit = /\d/.test(this.password);

      if (this.password.length < MINIMUM_PASSWORD_LENGTH || !hasLetter || !hasDigit) {
        return `Şifre en az ${MINIMUM_PASSWORD_LENGTH} karakter olmalı, harf ve rakam içermeli.`;
      }
    }

    return null;
  }

  private describeError(error: unknown, method: SignInMethod): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Giriş yapılamadı. Lütfen tekrar dene.';
    }

    if (error.status === 0) {
      return 'API sunucusuna bağlanılamadı. HerSeyiBaslat.bat ile uygulamayı aç.';
    }

    if (error.status === 401) {
      return method === 'google'
        ? 'Google ile giriş doğrulanamadı.'
        : 'E-posta adresi veya şifre hatalı.';
    }

    if (error.status === 409) {
      return 'Bu e-posta adresiyle zaten bir hesap var. Giriş yapmayı dene; hesabını Google ile açtıysan "Google ile devam et" butonunu kullan.';
    }

    if (error.status === 429) {
      return 'Çok fazla deneme yapıldı. Bir dakika bekleyip tekrar dene.';
    }

    const problem = error.error as { detail?: string; title?: string } | null;

    return problem?.detail ?? problem?.title ?? `Sunucu yanıtı: ${error.status}`;
  }
}
