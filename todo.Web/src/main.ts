import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { API_URL } from './app/core/config/api-url.token';
import { parseApiConfig } from './app/core/config/api-config';
import { environment } from './environments/environment';

async function start(): Promise<void> {
  const config = parseApiConfig(environment);
  await bootstrapApplication(App, {
    ...appConfig,
    providers: [...appConfig.providers, { provide: API_URL, useValue: config.apiUrl }],
  });
}

void start().catch((error) => {
  console.error(error);
  const host = document.querySelector('app-root');
  if (host) {
    host.textContent =
      'Uygulama ayarları yüklenemedi. Sayfayı yenile; sorun devam ederse yöneticine bildir.';
    host.setAttribute('role', 'alert');
  }
});
