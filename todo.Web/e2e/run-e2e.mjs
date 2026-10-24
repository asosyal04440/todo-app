import { randomBytes } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const e2eDirectory = path.dirname(fileURLToPath(import.meta.url));
const webDirectory = path.resolve(e2eDirectory, '..');
const solutionDirectory = path.resolve(webDirectory, '..');
const databaseName = `TodoApiE2E_${Date.now()}_${randomBytes(3).toString('hex')}`;
const connectionString = `Server=localhost\\SQLEXPRESS;Database=${databaseName};Integrated Security=True;TrustServerCertificate=True`;
const browserNodeDirectory = path.join(process.env.LOCALAPPDATA ?? '', 'hermes', 'node');
const browserNodeExecutable = path.join(browserNodeDirectory, 'node.exe');
const nodeExecutable = existsSync(browserNodeExecutable) ? browserNodeExecutable : process.execPath;
const environment = {
  ...process.env,
  TODO_E2E_PRODUCTION: String(process.argv.includes('--production')),
  ConnectionStrings__TodoDatabase: connectionString,
  Jwt__SigningKey: randomBytes(48).toString('base64'),
  Security__AuthenticationRequestsPerMinute: '1000',
  Reminders__Enabled: 'false',
  TODO_E2E_API_URL: 'http://localhost:17070/api',
  Logging__LogLevel__Microsoft: 'Warning',
  PATH: `${browserNodeDirectory};${process.env.PATH ?? ''}`,
};

function run(command, args, workingDirectory, childEnvironment = environment) {
  return spawnSync(command, args, {
    cwd: workingDirectory,
    env: childEnvironment,
    stdio: 'inherit',
    windowsHide: true,
  });
}

function requireSuccess(result, action) {
  if (result.error) {
    throw result.error;
  }

  if (result.status !== 0) {
    throw new Error(`${action} failed with exit code ${result.status ?? 'unknown'}.`);
  }
}

let exitCode = 1;

try {
  console.log(`Creating isolated SQL Server database: ${databaseName}`);

  const restoreTools = run('dotnet', ['tool', 'restore'], solutionDirectory);
  requireSuccess(restoreTools, 'Restoring local .NET tools');

  const migrateDatabase = run(
    'dotnet',
    [
      'ef',
      'database',
      'update',
      '--project',
      'todo.Infrastructure/todo.Infrastructure.csproj',
      '--startup-project',
      'todo.API/todo.API.csproj',
      '--configuration',
      'E2E',
      '--framework',
      'net8.0',
    ],
    solutionDirectory,
  );
  requireSuccess(migrateDatabase, 'Preparing the isolated end-to-end database');

  const playwrightCli = path.join(webDirectory, 'node_modules', '@playwright', 'test', 'cli.js');
  const runTests = run(
    nodeExecutable,
    [
      playwrightCli,
      'test',
      '--config',
      'e2e/playwright.config.ts',
      ...process.argv.slice(2).filter((argument) => argument !== '--production'),
    ],
    webDirectory,
  );

  if (runTests.error) {
    throw runTests.error;
  }

  exitCode = runTests.status ?? 1;
} catch (error) {
  console.error(error);
} finally {
  console.log(`Removing isolated SQL Server database: ${databaseName}`);

  const removeDatabase = run(
    'dotnet',
    [
      'ef',
      'database',
      'drop',
      '--force',
      '--project',
      'todo.Infrastructure/todo.Infrastructure.csproj',
      '--startup-project',
      'todo.API/todo.API.csproj',
      '--configuration',
      'E2E',
      '--framework',
      'net8.0',
    ],
    solutionDirectory,
  );

  if (removeDatabase.error || removeDatabase.status !== 0) {
    console.error('Could not remove the isolated end-to-end database.');
    console.error(removeDatabase.error ?? `dotnet ef exited with code ${removeDatabase.status}.`);
    exitCode = 1;
  }
}

process.exitCode = exitCode;
