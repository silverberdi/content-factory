#!/usr/bin/env node
import { readFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { buildPack, collectContextSources } from './generate-context-pack.mjs';
import { validateMachineIds } from './validate-machine-ids.mjs';

function sha256(content) {
  return createHash('sha256').update(content).digest('hex');
}

const machineIds = await validateMachineIds({ fail: false });
if (!machineIds.ok) {
  console.error('Context integrity blocked: machine IDs must be lowercase kebab-case.');
  for (const finding of machineIds.findings.slice(0, 20)) {
    console.error(`- ${finding.file}: ${finding.kind} ${finding.value}`);
  }
  if (machineIds.findings.length > 20) {
    console.error(`- ... and ${machineIds.findings.length - 20} more`);
  }
  process.exit(1);
}

const manifestPath = 'docs/context/generated/context-manifest.json';
const packPath = 'docs/context/generated/current-context-pack.md';

const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
const pack = await readFile(packPath, 'utf8');
const { entries } = await collectContextSources();
const expectedPack = buildPack(entries);

const stale = [];
const expectedPaths = entries.map((entry) => entry.path);
const manifestPaths = manifest.sources.map((item) => item.path);

if (JSON.stringify(expectedPaths) !== JSON.stringify(manifestPaths)) {
  console.error('Context pack source list mismatch.');
  console.error('Expected:', expectedPaths);
  console.error('Manifest:', manifestPaths);
  process.exit(1);
}

for (const entry of entries) {
  const recorded = manifest.sources.find((item) => item.path === entry.path);
  if (!recorded || recorded.sha256 !== entry.sha256) {
    stale.push(entry.path);
  }
}

const packHash = sha256(pack);
if (manifest.packSha256 !== packHash || pack !== expectedPack) {
  stale.push(packPath);
}

if (stale.length) {
  console.error('Context pack is stale:', stale);
  process.exit(1);
}

console.log('Context pack is current.');
