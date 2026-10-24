import { defineConfig } from '@playwright/test';
import path from 'node:path';

const webDirectory = path.resolve(__dirname, '..');

export default defineConfig({
  testDir: __dirname,
  testMatch: 'lazy-loading.spec.ts',
  outputDir: '../test-results/lazy-loading',
  reporter: 'list',
  timeout: 45_000,
  expect: { timeout: 10_000 },
  use: {
    browserName: 'chromium',
    headless: true,
    serviceWorkers: 'block',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'development', use: { baseURL: 'http://localhost:4310' } },
    { name: 'production', use: { baseURL: 'http://localhost:4311' } },
  ],
  webServer: [
    {
      command:
        'node node_modules/@angular/cli/bin/ng.js serve --configuration development --host localhost --port 4310',
      cwd: webDirectory,
      url: 'http://localhost:4310',
      reuseExistingServer: false,
      timeout: 120_000,
    },
    {
      command: 'node e2e/serve-production.mjs',
      env: { TODO_PREVIEW_PORT: '4311' },
      cwd: webDirectory,
      url: 'http://localhost:4311',
      reuseExistingServer: false,
      timeout: 30_000,
    },
  ],
});
