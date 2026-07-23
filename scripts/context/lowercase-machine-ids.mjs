#!/usr/bin/env node
/**
 * One-shot baseline correction: lowercase machine-readable wave/slice/US/change IDs
 * while preserving human display titles of the form `W00 — …` / `W00-S01 — …`.
 *
 * Usage: node scripts/context/lowercase-machine-ids.mjs
 */
import { readdir, readFile, writeFile, stat } from 'node:fs/promises';
import path from 'node:path';

const ROOT = process.cwd();

const SKIP_DIR_NAMES = new Set([
  '.git',
  'node_modules',
  '.cursor/commands',
  '.cursor/skills',
  '.codex/skills',
  'docs/context/generated',
]);

const SKIP_PATH_PREFIXES = [
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

function shouldSkip(relPath) {
  const normalized = relPath.split(path.sep).join('/');
  if (SKIP_PATH_PREFIXES.some((prefix) => normalized === prefix.slice(0, -1) || normalized.startsWith(prefix))) {
    return true;
  }
  if (normalized === 'scripts/context/lowercase-machine-ids.mjs') return true;
  if (normalized === 'scripts/context/validate-machine-ids.mjs') return true;
  return false;
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
    if (!TEXT_EXTENSIONS.has(ext) && entry.name !== 'FILE-INDEX.md') continue;
    out.push(rel);
  }
  return out;
}

/**
 * Transform machine IDs to lowercase kebab-case.
 * Preserves display titles: `W00 — Title` and `W00-S01 — Title`.
 */
export function lowercaseMachineIds(content) {
  let result = content;
  const protectedBlocks = [];

  const protect = (match) => {
    const token = `\u0000PROT${protectedBlocks.length}\u0000`;
    protectedBlocks.push(match);
    return token;
  };

  // Preserve human display titles (em dash).
  result = result.replace(/W\d{2}-S\d{2}\s*—/g, protect);
  result = result.replace(/W\d{2}\s*—/g, protect);

  // OpenSpec change names (always machine IDs).
  result = result.replace(/CHG-W(\d{2})-S(\d{2})-([A-Za-z0-9-]+)/g, (_, w, s, rest) => {
    return `chg-w${w}-s${s}-${rest.toLowerCase()}`;
  });

  // User Story IDs (always machine IDs).
  result = result.replace(/US-W(\d{2})-S(\d{2})-(\d{3})/g, (_, w, s, n) => {
    return `us-w${w}-s${s}-${n}`;
  });

  // Slice IDs not part of a display title.
  result = result.replace(/W(\d{2})-S(\d{2})/g, (_, w, s) => `w${w}-s${s}`);

  // Wave IDs not part of a display title.
  result = result.replace(/\bW(\d{2})\b/g, (_, w) => `w${w}`);

  // Restore protected display titles.
  result = result.replace(/\u0000PROT(\d+)\u0000/g, (_, idx) => protectedBlocks[Number(idx)]);

  return result;
}

function countMatches(content, pattern) {
  return (content.match(pattern) || []).length;
}

async function main() {
  const files = await walk(ROOT);
  let changedFiles = 0;
  const stats = {
    changeNames: 0,
    userStories: 0,
    slices: 0,
    waves: 0,
  };

  for (const rel of files) {
    const abs = path.join(ROOT, rel);
    const before = await readFile(abs, 'utf8');

    // Count uppercase machine IDs before transform (excluding display titles).
    const withoutDisplay = before
      .replace(/W\d{2}-S\d{2}\s*—/g, '')
      .replace(/W\d{2}\s*—/g, '');
    stats.changeNames += countMatches(withoutDisplay, /CHG-W\d{2}-S\d{2}-[A-Za-z0-9-]+/g);
    stats.userStories += countMatches(withoutDisplay, /US-W\d{2}-S\d{2}-\d{3}/g);
    stats.slices += countMatches(withoutDisplay, /W\d{2}-S\d{2}/g);
    stats.waves += countMatches(withoutDisplay, /\bW\d{2}\b/g);

    const after = lowercaseMachineIds(before);
    if (after !== before) {
      await writeFile(abs, after);
      changedFiles += 1;
      console.log(`updated: ${rel}`);
    }
  }

  console.log(
    JSON.stringify(
      {
        scanned: files.length,
        changedFiles,
        uppercaseOccurrencesReplaced: stats,
      },
      null,
      2,
    ),
  );
}

const isDirectRun =
  process.argv[1] &&
  (await import('node:url')).pathToFileURL(process.argv[1]).href === import.meta.url;

if (isDirectRun) {
  await main();
}
