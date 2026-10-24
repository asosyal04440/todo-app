import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import ts from 'typescript';

export function findBoundaryViolations(appDirectory, fileNames, compilerOptions) {
  const appRoot = path.resolve(appDirectory);
  const violations = [];
  for (const fileName of fileNames) {
    const source = ts.createSourceFile(fileName, fs.readFileSync(fileName, 'utf8'), ts.ScriptTarget.Latest, true);
    const origin = path.relative(appRoot, fileName).replaceAll('\\', '/');
    if (area(origin) === 'unknown') violations.push(`${origin}: Place application code in core, shared or a named feature.`);
    function inspectModule(specifier, node) {
      const location = source.getLineAndCharacterOfPosition(node.getStart(source));
      const resolution = ts.resolveModuleName(specifier, fileName, compilerOptions, ts.sys).resolvedModule;
      if (!resolution) {
        violations.push(`${origin}:${location.line + 1}: Cannot resolve "${specifier}".`);
        return;
      }
      const target = path.relative(appRoot, resolution.resolvedFileName).replaceAll('\\', '/');
      if (resolution.isExternalLibraryImport) return;
      if (target.startsWith('../') || path.isAbsolute(target)) {
        violations.push(`${origin}:${location.line + 1}: Application dependencies must stay inside app ("${specifier}").`);
        return;
      }
      const from = area(origin);
      const to = area(target);
      if (to !== 'unknown' && (from === 'composition' || from === to || (from !== 'shared' && to === 'shared')
        || (from.startsWith('feature:') && to === 'core'))) return;
      violations.push(`${origin}:${location.line + 1}: ${from} cannot depend on ${to} ("${specifier}").`);
    }
    function visit(node) {
      if ((ts.isImportDeclaration(node) || ts.isExportDeclaration(node)) && node.moduleSpecifier
        && ts.isStringLiteralLike(node.moduleSpecifier)) inspectModule(node.moduleSpecifier.text, node);
      if (ts.isImportTypeNode(node) && ts.isLiteralTypeNode(node.argument)
        && ts.isStringLiteralLike(node.argument.literal)) inspectModule(node.argument.literal.text, node);
      if (ts.isCallExpression(node) && node.expression.kind === ts.SyntaxKind.ImportKeyword) {
        const argument = node.arguments[0];
        if (argument && ts.isStringLiteralLike(argument)) inspectModule(argument.text, node);
        else violations.push(`${path.relative(appRoot, fileName)}: Dynamic imports must use a literal module path.`);
      }
      ts.forEachChild(node, visit);
    }
    visit(source);
  }
  return violations;
}

function area(relativePath) {
  const parts = relativePath.split('/');
  if (parts[0] === 'features' && parts.length >= 3) return `feature:${parts[1]}`;
  if (parts[0] === 'core' || parts[0] === 'shared') return parts[0];
  return parts.length === 1 ? 'composition' : 'unknown';
}

function collectTypescript(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const entryPath = path.join(directory, entry.name);
    return entry.isDirectory() ? collectTypescript(entryPath) : entry.name.endsWith('.ts') ? [entryPath] : [];
  });
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const webDirectory = fileURLToPath(new URL('../', import.meta.url));
  const configPath = path.join(webDirectory, 'tsconfig.app.json');
  const config = ts.readConfigFile(configPath, ts.sys.readFile);
  if (config.error) throw new Error(ts.flattenDiagnosticMessageText(config.error.messageText, '\n'));
  const parsed = ts.parseJsonConfigFileContent(config.config, ts.sys, webDirectory);
  if (parsed.errors.length) throw new Error(parsed.errors.map(error => ts.flattenDiagnosticMessageText(error.messageText, '\n')).join('\n'));
  const appDirectory = path.join(webDirectory, 'src/app');
  const files = collectTypescript(appDirectory);
  const violations = findBoundaryViolations(appDirectory, files, parsed.options);
  if (violations.length) {
    console.error(`Feature boundary check failed:\n${violations.join('\n')}`);
    process.exitCode = 1;
  } else console.log(`Feature boundaries checked: ${files.length} TypeScript files.`);
}
