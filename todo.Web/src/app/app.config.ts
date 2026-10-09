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
import { authInterceptor } from './interceptors/auth.interceptor';
import { AuthService } from './services/auth.service';

registerLocaleData(tr);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter(routes),
    provideAppInitializer(() => inject(AuthService).restoreSession()),
    provideNzI18n(tr_TR),
    { provide: LOCALE_ID, useValue: 'tr-TR' },
  ],
};
