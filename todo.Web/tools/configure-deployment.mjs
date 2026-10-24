import fs from 'node:fs';
import path from 'node:path';
import { parseApiConfig, apiConnectSource } from '../src/app/core/config/api-config.ts';

export function configureDeployment(directory, apiUrl) {
  const config = parseApiConfig({ apiUrl });
  const root = path.resolve(directory);
  const indexPath = path.join(root, 'index.html');
  const source = fs.readFileSync(indexPath, 'utf8');
  const policy = /(<meta\s+http-equiv="Content-Security-Policy"\s+content=")([^"]+)(")/i;
  if (!policy.test(source)) throw new Error('Deployment index.html must contain the CSP meta tag.');
  const rendered = source.replace(policy, (_match, prefix, content, suffix) => {
    if (!/connect-src\s+[^;]+/.test(content)) throw new Error('CSP connect-src is missing.');
    return (
      prefix +
      content.replace(
        /connect-src\s+[^;]+/,
        `connect-src ${apiConnectSource(config)} https://accounts.google.com/gsi/`,
      ) +
      suffix
    );
  });
  fs.writeFileSync(indexPath, rendered);
  return config;
}
