import { registerLocaleData } from '@angular/common';
import tr from '@angular/common/locales/tr';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideNzI18n, tr_TR } from 'ng-zorro-antd/i18n';
import { routes } from './app.routes';
import { authInterceptor } from './core/http/auth.interceptor';
import { apiErrorInterceptor } from './core/http/api-error.interceptor';
import { AuthService } from './core/auth/auth.service';

registerLocaleData(tr);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([apiErrorInterceptor, authInterceptor])),
    provideRouter(routes),
    provideAppInitializer(() => inject(AuthService).restoreSession()),
    provideNzI18n(tr_TR),
    { provide: LOCALE_ID, useValue: 'tr-TR' },
  ],
};
