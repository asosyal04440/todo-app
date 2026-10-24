import http from 'node:http';
import https from 'node:https';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../dist/todo-web/browser/', import.meta.url));
const port = Number(process.env.TODO_PREVIEW_PORT ?? '4201');
if (!Number.isInteger(port) || port < 1 || port > 65535)
  throw new Error('Preview port must be an integer between 1 and 65535.');
const backend = new URL(process.env.TODO_E2E_API_URL ?? 'http://localhost:17070/api');
if (!['http:', 'https:'].includes(backend.protocol))
  throw new Error('Preview API must be HTTP(S).');
if (!fs.existsSync(path.join(root, 'index.html')))
  throw new Error('Run the Angular production build before previewing.');
const mime = {
  '.html': 'text/html',
  '.js': 'text/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.woff2': 'font/woff2',
};
const server = http.createServer((request, response) => {
  const url = new URL(request.url ?? '/', `http://localhost:${port}`);
  if (url.pathname === '/api' || url.pathname.startsWith('/api/')) {
    const target = new URL(url.pathname + url.search, backend.origin);
    const transport = target.protocol === 'https:' ? https : http;
    const upstream = transport.request(
      target,
      { method: request.method, headers: { ...request.headers, host: target.host } },
      (incoming) => {
        response.writeHead(incoming.statusCode ?? 502, incoming.headers);
        incoming.pipe(response);
      },
    );
    upstream.on('error', () => {
      if (response.headersSent) response.destroy();
      else {
        response.writeHead(503, { 'Content-Type': 'application/json' });
        response.end('{"title":"Preview API unavailable"}');
      }
    });
    request.on('aborted', () => upstream.destroy());
    response.on('close', () => {
      if (!response.writableEnded) upstream.destroy();
    });
    request.pipe(upstream);
    return;
  }
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    response.writeHead(405);
    response.end();
    return;
  }
  let file;
  try {
    file = path.resolve(root, '.' + decodeURIComponent(url.pathname));
  } catch {
    response.writeHead(400);
    response.end();
    return;
  }
  if (file !== path.resolve(root) && !file.startsWith(path.resolve(root) + path.sep)) {
    response.writeHead(403);
    response.end();
    return;
  }
  if (!fs.existsSync(file) || !fs.statSync(file).isFile()) {
    if (url.pathname !== '/' && !request.headers.accept?.includes('text/html')) {
      response.writeHead(404);
      response.end();
      return;
    }
    file = path.join(root, 'index.html');
  }
  response.writeHead(200, {
    'Content-Type': mime[path.extname(file)] ?? 'application/octet-stream',
    'Cache-Control': 'no-store',
  });
  if (request.method === 'HEAD') {
    response.end();
    return;
  }
  const stream = fs.createReadStream(file);
  stream.on('error', () => response.destroy());
  stream.pipe(response);
});
server.listen(port, 'localhost', () =>
  console.log(`Production preview ready at http://localhost:${port}`),
);
for (const signal of ['SIGINT', 'SIGTERM'])
  process.on(signal, () => {
    server.closeAllConnections();
    server.close(() => process.exit(0));
  });
