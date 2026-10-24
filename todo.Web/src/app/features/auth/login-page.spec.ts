import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { LoginPage } from './login-page';
import { AuthService } from '../../core/auth/auth.service';
import { GoogleIdentityService } from './google-identity.service';

describe('LoginPage reactive validation', () => {
  const login = vi.fn();
  const register = vi.fn();
  beforeEach(() => {
    login.mockReset();
    register.mockReset();
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { login, register, getAuthConfig: () => of({ googleClientId: null }) },
        },
        { provide: GoogleIdentityService, useValue: {} },
      ],
    });
  });

  it('uses the registration password policy only in registration mode', () => {
    const page = TestBed.createComponent(LoginPage).componentInstance;
    page.form.patchValue({ email: 'ali@example.com', password: 'short' });
    expect(page.form.valid).toBe(true);
    page.switchMode('register');
    expect(page.form.controls.password.hasError('passwordPolicy')).toBe(true);
    expect(page.form.controls.displayName.hasError('required')).toBe(true);
    page.form.patchValue({ password: 'GucluSifre1', displayName: 'Ali' });
    expect(page.form.valid).toBe(true);
    page.switchMode('login');
    page.form.controls.password.setValue('short');
    expect(page.form.valid).toBe(true);
  });

  it('marks invalid fields touched and sends no HTTP request', async () => {
    const page = TestBed.createComponent(LoginPage).componentInstance;
    await page.submit();
    expect(page.form.controls.email.touched).toBe(true);
    expect(page.form.controls.password.hasError('required')).toBe(true);
    expect(login).not.toHaveBeenCalled();
    expect(register).not.toHaveBeenCalled();
  });

  it('rejects invalid email and values exceeding Domain mirrored limits', () => {
    const page = TestBed.createComponent(LoginPage).componentInstance;
    page.form.patchValue({ email: 'invalid', password: 'a'.repeat(129) });
    expect(page.form.controls.email.hasError('email')).toBe(true);
    expect(page.form.controls.password.hasError('maxlength')).toBe(true);
    page.switchMode('register');
    page.form.controls.displayName.setValue('a'.repeat(101));
    expect(page.form.controls.displayName.hasError('maxlength')).toBe(true);
  });
});
