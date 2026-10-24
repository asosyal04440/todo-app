import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import assert from 'node:assert/strict';
import { test } from 'node:test';
import ts from 'typescript';
import { findBoundaryViolations } from './check-feature-boundaries.mjs';

function check(sourcePath, sourceText, options = {}) {
  const fixture = fs.mkdtempSync(path.join(os.tmpdir(), 'todo-feature-boundaries-'));
  const appRoot = path.join(fixture, 'app');
  try {
    fs.writeFileSync(path.join(fixture, 'bootstrap.ts'), 'export const value = 1;');
    for (const file of ['core/auth/session.ts', 'shared/pagination.ts', 'features/todos/model.ts', 'features/admin/model.ts', 'app.routes.ts']) {
      fs.mkdirSync(path.dirname(path.join(appRoot, file)), { recursive: true });
      fs.writeFileSync(path.join(appRoot, file), 'export const value = 1;');
    }
    const source = path.join(appRoot, sourcePath);
    fs.mkdirSync(path.dirname(source), { recursive: true });
    fs.writeFileSync(source, sourceText);
    return findBoundaryViolations(appRoot, [source], {
      module: ts.ModuleKind.Preserve, moduleResolution: ts.ModuleResolutionKind.Bundler, ...options,
      baseUrl: appRoot,
    });
  } finally {
    const resolved = path.resolve(fixture);
    const parent = path.resolve(os.tmpdir());
    if (path.dirname(resolved) !== parent || !path.basename(resolved).startsWith('todo-feature-boundaries-'))
      throw new Error('Refusing to remove an unexpected fixture path.');
    fs.rmSync(resolved, { recursive: true, force: true });
  }
}

test('features can use their own files, core and shared', () => {
  assert.deepEqual(check('features/todos/page.ts', `
    import { value as own } from './model';
    import { value as session } from '../../core/auth/session';
    import { value as shared } from '../../shared/pagination';
  `), []);
});
test('root composition can connect independent features', () => {
  assert.deepEqual(check('app.routes.ts', `import { value } from './features/todos/model';`), []);
});
test('features cannot import other features', () => {
  assert.match(check('features/admin/page.ts', `import { value } from '../todos/model';`)[0], /feature:admin cannot depend on feature:todos/);
});
test('core cannot import a feature', () => {
  assert.match(check('core/service.ts', `import { value } from '../features/todos/model';`)[0], /core cannot depend on feature:todos/);
});
test('shared cannot import core', () => {
  assert.match(check('shared/model.ts', `import { value } from '../core/auth/session';`)[0], /shared cannot depend on core/);
});
test('type imports cannot bypass feature boundaries', () => {
  assert.equal(check('features/admin/page.ts', `type Model = typeof import('../todos/model');`).length, 1);
});
test('dynamic imports and re-exports cannot bypass feature boundaries', () => {
  assert.equal(check('features/admin/page.ts', `const module = import('../todos/model'); export * from '../todos/model';`).length, 2);
});
test('TypeScript path aliases cannot bypass feature boundaries', () => {
  assert.equal(check('features/admin/page.ts', `import { value } from '@todos/model';`, { paths: { '@todos/*': ['features/todos/*'] } }).length, 1);
});
test('unresolved module paths are rejected', () => {
  assert.match(check('features/admin/page.ts', `import { value } from './missing';`)[0], /Cannot resolve/);
});
test('features cannot reach the root composition through a barrel', () => {
  assert.match(check('features/todos/page.ts', `export * from '../../app.routes';`)[0], /feature:todos cannot depend on composition/);
});
test('application imports cannot bypass the rules by leaving app', () => {
  assert.match(check('features/todos/page.ts', `import { value } from '../../../bootstrap';`)[0], /must stay inside app/);
});
test('new layer-based folders are rejected', () => {
  assert.match(check('services/service.ts', 'export const value = 1;')[0], /named feature/);
});
