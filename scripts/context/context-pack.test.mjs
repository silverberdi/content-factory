#!/usr/bin/env node
/**
 * Automated tests for context-pack generate/check success and stale failure.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile, writeFile, copyFile, mkdir } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { buildPack, collectContextSources, generateContextPack } from './generate-context-pack.mjs';

const ROOT = process.cwd();
const PACK = 'docs/context/generated/current-context-pack.md';
const MANIFEST = 'docs/context/generated/context-manifest.json';
const BACKUP_DIR = path.join(ROOT, '.tmp-context-pack-test');

function runCheck() {
  return spawnSync(process.execPath, ['scripts/context/check-context-pack.mjs'], {
    cwd: ROOT,
    encoding: 'utf8',
  });
}

test('buildPack includes only provided sources and generated marker', () => {
  const pack = buildPack([
    { path: 'a.md', content: 'alpha' },
    { path: 'b.md', content: 'beta' },
  ]);
  assert.match(pack, /Generated Current Context Pack/);
  assert.match(pack, /## SOURCE: a\.md/);
  assert.match(pack, /## SOURCE: b\.md/);
  assert.doesNotMatch(pack, /gamma/);
});

test('collectContextSources scopes to active wave only', async () => {
  const { waveId, entries } = await collectContextSources();
  assert.equal(waveId, 'w00');
  const paths = entries.map((e) => e.path);
  assert.ok(paths.includes('docs/waves/w00-project-foundation/contract.md'));
  assert.ok(paths.includes('docs/backlog/user-stories/w00-user-stories.md'));
  assert.ok(!paths.some((p) => p.includes('w01-')));
  assert.ok(!paths.some((p) => p.includes('w11-')));
});

test('generate then check succeeds when synchronized', async () => {
  await generateContextPack();
  const result = runCheck();
  assert.equal(result.status, 0, result.stderr || result.stdout);
  assert.match(result.stdout, /Context pack is current/);
});

test('check fails when a tracked source changes without regenerate', async () => {
  await mkdir(BACKUP_DIR, { recursive: true });
  const source = 'docs/methodology/evidence-standard.md';
  const backup = path.join(BACKUP_DIR, 'evidence-standard.md');
  await copyFile(source, backup);

  try {
    await generateContextPack();
    const original = await readFile(source, 'utf8');
    await writeFile(source, `${original}\n\n<!-- stale-pack-test-marker -->\n`);
    const result = runCheck();
    assert.notEqual(result.status, 0);
    assert.match(`${result.stderr}\n${result.stdout}`, /stale|Context pack is stale/i);
  } finally {
    await copyFile(backup, source);
    await generateContextPack();
  }
});

test('check fails on manifest source-list mismatch', async () => {
  await mkdir(BACKUP_DIR, { recursive: true });
  const backup = path.join(BACKUP_DIR, 'context-manifest.json');
  await generateContextPack();
  await copyFile(MANIFEST, backup);

  try {
    const manifest = JSON.parse(await readFile(MANIFEST, 'utf8'));
    manifest.sources = manifest.sources.slice(0, -1);
    await writeFile(MANIFEST, `${JSON.stringify(manifest, null, 2)}\n`);
    const result = runCheck();
    assert.notEqual(result.status, 0);
    assert.match(`${result.stderr}\n${result.stdout}`, /source list mismatch/i);
  } finally {
    await copyFile(backup, MANIFEST);
    await generateContextPack();
  }
});
