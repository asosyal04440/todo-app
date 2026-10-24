import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { test } from 'node:test';
import { configureDeployment } from './configure-deployment.mjs';

function fixture(check) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'todo-deployment-'));
  try {
    fs.writeFileSync(
      path.join(root, 'index.html'),
      fs.readFileSync(new URL('../src/index.html', import.meta.url)),
    );
    fs.writeFileSync(path.join(root, 'main.js'), 'unchanged build');
    check(root);
  } finally {
    const resolved = path.resolve(root);
    if (
      path.dirname(resolved) !== path.resolve(os.tmpdir()) ||
      !path.basename(resolved).startsWith('todo-deployment-')
    )
      throw new Error('Invalid deployment test cleanup path.');
    fs.rmSync(resolved, { recursive: true, force: true });
  }
}
test('allows the built environment API origin without changing JavaScript or adding runtime config', () =>
  fixture((root) => {
    configureDeployment(root, 'https://api.example.com/api');
    assert.equal(fs.existsSync(path.join(root, 'runtime-config.json')), false);
    const index = fs.readFileSync(path.join(root, 'index.html'), 'utf8');
    assert.match(
      index,
      /connect-src 'self' https:\/\/api.example.com https:\/\/accounts.google.com\/gsi\//,
    );
    assert.doesNotMatch(index, /localhost:7070|localhost:17070/);
    assert.equal(fs.readFileSync(path.join(root, 'main.js'), 'utf8'), 'unchanged build');
  }));
test('replaces the old origin and supports returning to a same-origin API', () =>
  fixture((root) => {
    configureDeployment(root, 'https://first.example.com/api');
    configureDeployment(root, 'https://second.example.com/api');
    assert.doesNotMatch(
      fs.readFileSync(path.join(root, 'index.html'), 'utf8'),
      /first.example.com/,
    );
    configureDeployment(root, '/api');
    assert.doesNotMatch(
      fs.readFileSync(path.join(root, 'index.html'), 'utf8'),
      /second.example.com/,
    );
  }));
test('rejects invalid URLs before modifying deployment files', () =>
  fixture((root) => {
    const before = fs.readFileSync(path.join(root, 'index.html'), 'utf8');
    assert.throws(() => configureDeployment(root, "https://api.example.com/'; connect-src *"));
    assert.equal(fs.readFileSync(path.join(root, 'index.html'), 'utf8'), before);
    assert.equal(fs.existsSync(path.join(root, 'runtime-config.json')), false);
  }));
