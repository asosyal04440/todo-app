import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { test } from 'node:test';
import { resolveBuildSettings } from './build.mjs';

const workspace = JSON.parse(fs.readFileSync(new URL('../angular.json', import.meta.url), 'utf8'));

test('the default production build selects the same environment file as Angular fileReplacements', () => {
  assert.deepEqual(resolveBuildSettings(workspace, []), {
    configuration: 'production',
    environmentFile: 'src/environments/environment.prod.ts',
    outputDirectory: path.join('dist/todo-web', 'browser'),
  });
});

test('development uses the base environment and supports an explicit output directory', () => {
  assert.deepEqual(
    resolveBuildSettings(workspace, ['--configuration', 'development', '--output-path=dist/dev']),
    {
      configuration: 'development',
      environmentFile: 'src/environments/environment.ts',
      outputDirectory: path.join('dist/dev', 'browser'),
    },
  );
});

test('combined configurations and -c use the final Angular file replacement', () => {
  assert.equal(
    resolveBuildSettings(workspace, ['-c', 'development,production']).environmentFile,
    'src/environments/environment.prod.ts',
  );
});

test('invalid or missing configurations fail before a build is started', () => {
  assert.throws(
    () => resolveBuildSettings(workspace, ['-c', 'unknown']),
    /Unknown build configuration/,
  );
  assert.throws(() => resolveBuildSettings(workspace, ['--configuration']), /needs a value/);
});
