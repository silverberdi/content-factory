#!/usr/bin/env node
/**
 * Validate that machine-readable wave/slice/User Story/OpenSpec change IDs are
 * lowercase kebab-case. Human display titles `W00 — …` / `W00-S01 — …` are allowed.
 */
import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';

const ROOT = process.cwd();

const SKIP_PATH_PREFIXES = [
  '.git/',
  'node_modules/',
  '.cursor/commands/',
  '.cursor/skills/',
  '.codex/skills/',
  'docs/context/generated/',
];

const TEXT_EXTENSIONS = new Set([
  '.md',
  '.mdc',
  '.yml',
  '.yaml',
  '.json',
  '.mjs',
  '.js',
  '.ts',
  '.tsx',
  '.txt',
]);

const WAVE_ID_RE = /^w\d{2}$/;
const SLICE_ID_RE = /^w\d{2}-s\d{2}$/;
const USER_STORY_ID_RE = /^us-w\d{2}-s\d{2}-\d{3}$/;
const CHANGE_NAME_RE = /^chg-w\d{2}-s\d{2}-[a-z0-9]+(?:-[a-z0-9]+)*$/;

const SKIP_FILES = new Set([
  'scripts/context/validate-machine-ids.mjs',
  'scripts/context/lowercase-machine-ids.mjs',
]);

function shouldSkip(relPath) {
  const normalized = relPath.split(path.sep).join('/');
  if (SKIP_FILES.has(normalized)) return true;
  return SKIP_PATH_PREFIXES.some(
    (prefix) => normalized === prefix.slice(0, -1) || normalized.startsWith(prefix),
  );
}

async function walk(dir, out = []) {
  const entries = await readdir(dir, { withFileTypes: true });
  for (const entry of entries) {
    const abs = path.join(dir, entry.name);
    const rel = path.relative(ROOT, abs).split(path.sep).join('/');
    if (entry.isDirectory()) {
      if (entry.name === '.git' || entry.name === 'node_modules') continue;
      if (shouldSkip(rel + '/')) continue;
      await walk(abs, out);
      continue;
    }
    if (shouldSkip(rel)) continue;
    const ext = path.extname(entry.name);
    if (!TEXT_EXTENSIONS.has(ext)) continue;
    out.push(rel);
  }
  return out;
}

function stripDisplayTitles(content) {
  return content
    .replace(/W\d{2}-S\d{2}\s*—[^\n]*/g, '')
    .replace(/W\d{2}\s*—[^\n]*/g, '');
}

function findUppercaseMachineIds(content, relPath) {
  const findings = [];
  const scanned = stripDisplayTitles(content);

  const patterns = [
    { kind: 'change', re: /CHG-[A-Za-z0-9-]+/g },
    { kind: 'user-story', re: /US-W\d{2}-S\d{2}-\d{3}/g },
    { kind: 'slice', re: /\bW\d{2}-S\d{2}\b/g },
    { kind: 'wave', re: /\bW\d{2}\b/g },
  ];

  for (const { kind, re } of patterns) {
    for (const match of scanned.matchAll(re)) {
      findings.push({ file: relPath, kind, value: match[0] });
    }
  }

  // Reject change names that are not fully lowercase kebab-case.
  for (const match of content.matchAll(/\bchg-[A-Za-z0-9-]+\b/g)) {
    if (!CHANGE_NAME_RE.test(match[0])) {
      findings.push({ file: relPath, kind: 'change-case', value: match[0] });
    }
  }

  return findings;
}

export function assertCanonicalId(kind, value) {
  const checkers = {
    wave: WAVE_ID_RE,
    slice: SLICE_ID_RE,
    'user-story': USER_STORY_ID_RE,
    change: CHANGE_NAME_RE,
  };
  const re = checkers[kind];
  if (!re) throw new Error(`Unknown ID kind: ${kind}`);
  if (!re.test(value)) {
    throw new Error(`Invalid ${kind} machine ID (expected lowercase kebab-case): ${value}`);
  }
}

export async function validateMachineIds({ fail = true } = {}) {
  const files = await walk(ROOT);
  const findings = [];

  for (const rel of files) {
    const content = await readFile(path.join(ROOT, rel), 'utf8');
    findings.push(...findUppercaseMachineIds(content, rel));
  }

  // Canonical current-state fields must be lowercase machine IDs.
  const currentState = await readFile('docs/context/current-state.md', 'utf8');
  const field = (label) => {
    const match = currentState.match(
      new RegExp(`\\|\\s*${label}\\s*\\|\\s*\`([^\`]+)\`\\s*\\|`, 'i'),
    );
    if (!match) throw new Error(`Missing current-state field: ${label}`);
    return match[1].trim();
  };

  assertCanonicalId('wave', field('Active wave ID'));
  assertCanonicalId('slice', field('Active slice ID'));
  assertCanonicalId('change', field('Expected OpenSpec change'));

  if (findings.length) {
    const summary = findings
      .slice(0, 50)
      .map((f) => `${f.file}: ${f.kind} ${f.value}`)
      .join('\n');
    const message = `Found ${findings.length} uppercase/invalid machine ID(s).\n${summary}${
      findings.length > 50 ? `\n... and ${findings.length - 50} more` : ''
    }`;
    if (fail) {
      console.error(message);
      process.exitCode = 1;
      return { ok: false, findings };
    }
    return { ok: false, findings };
  }

  console.log('Machine IDs are lowercase kebab-case.');
  return { ok: true, findings: [] };
}

const isDirectRun =
  process.argv[1] &&
  (await import('node:url')).pathToFileURL(process.argv[1]).href === import.meta.url;

if (isDirectRun) {
  await validateMachineIds();
}
