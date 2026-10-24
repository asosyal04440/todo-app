import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { parseApiConfig } from '../src/app/core/config/api-config.ts';
import { configureDeployment } from './configure-deployment.mjs';

const webDirectory = fileURLToPath(new URL('../', import.meta.url));

function option(args, names) {
  let value;
  for (let index = 0; index < args.length; index++) {
    const name = names.find((name) => args[index] === name || args[index].startsWith(`${name}=`));
    if (!name) continue;
    value = args[index].includes('=') ? args[index].slice(name.length + 1) : args[++index];
    if (!value || value.startsWith('-')) throw new Error(`${name} needs a value.`);
  }
  return value;
}

export function resolveBuildSettings(workspace, args) {
  const build = workspace.projects['todo-web'].architect.build;
  const configuration = option(args, ['--configuration', '-c']) ?? build.defaultConfiguration;
  let options = { ...build.options };
  for (const name of configuration.split(',')) {
    if (!Object.hasOwn(build.configurations, name))
      throw new Error(`Unknown build configuration: ${name}`);
    options = { ...options, ...build.configurations[name] };
  }
  const replacement = options.fileReplacements?.find(
    (entry) => entry.replace === 'src/environments/environment.ts',
  );
  const output = option(args, ['--output-path']) ?? options.outputPath ?? 'dist/todo-web';
  const base = typeof output === 'string' ? output : output.base;
  const browser = typeof output === 'string' ? 'browser' : (output.browser ?? 'browser');
  return {
    configuration,
    environmentFile: replacement?.with ?? 'src/environments/environment.ts',
    outputDirectory: path.join(base, browser),
  };
}

async function build(args) {
  if (args.some((arg) => arg === '--watch' || arg === '--watch=true'))
    throw new Error(
      'Use npm run watch for development; npm run build finalizes deployment CSP once.',
    );
  const workspace = JSON.parse(fs.readFileSync(path.join(webDirectory, 'angular.json'), 'utf8'));
  const settings = resolveBuildSettings(workspace, args);
  const { environment } = await import(
    pathToFileURL(path.resolve(webDirectory, settings.environmentFile)).href
  );
  const config = parseApiConfig(environment);
  const result = spawnSync(
    process.execPath,
    [path.join(webDirectory, 'node_modules/@angular/cli/bin/ng.js'), 'build', ...args],
    { cwd: webDirectory, stdio: 'inherit', windowsHide: true },
  );
  if (result.error) throw result.error;
  if (result.status !== 0)
    throw new Error(`Angular build failed (${result.status ?? result.signal}).`);
  configureDeployment(path.resolve(webDirectory, settings.outputDirectory), config.apiUrl);
  console.log(`Environment: ${settings.environmentFile}; API: ${config.apiUrl}; CSP updated.`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  build(process.argv.slice(2)).catch((error) => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
