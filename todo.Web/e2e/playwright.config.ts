import { defineConfig } from '@playwright/test';
import path from 'node:path';

const e2eDirectory = __dirname;
const webDirectory = path.resolve(e2eDirectory, '..');
const solutionDirectory = path.resolve(webDirectory, '..');

export default defineConfig({
  testDir: e2eDirectory,
  testMatch: '*.spec.ts',
  fullyParallel: false,
  timeout: 45_000,
  expect: {
    timeout: 10_000,
  },
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:4201',
    browserName: 'chromium',
    headless: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  webServer: [
    {
      command:
        'dotnet run --configuration E2E --project todo.API/todo.API.csproj --framework net8.0 --urls http://localhost:17070',
      cwd: solutionDirectory,
      url: 'http://localhost:17070/api/auth/config',
      reuseExistingServer: false,
      timeout: 120_000,
    },
    {
      command:
        'node ./node_modules/@angular/cli/bin/ng.js serve --host localhost --port 4201',
      cwd: webDirectory,
      url: 'http://localhost:4201',
      reuseExistingServer: false,
      timeout: 120_000,
    },
  ],
});
