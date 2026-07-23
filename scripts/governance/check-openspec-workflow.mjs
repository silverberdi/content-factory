#!/usr/bin/env node
/**
 * Lightweight OpenSpec workflow contract for w00-s01:
 * propose → apply → verify PASS → sync → archive expectations.
 */
import { access, readFile, readdir } from 'node:fs/promises';
import { constants } from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const ROOT = process.cwd();
const CHANGE = 'chg-w00-s01-repository-governance-and-openspec-foundation';
const CHANGE_DIR = path.join(ROOT, 'openspec/changes', CHANGE);

const REQUIRED_ARTIFACTS = ['proposal.md', 'design.md', 'tasks.md'];
const REQUIRED_SPEC_CAPS = [
  'repository-governance',
  'openspec-workflow',
  'agent-operating-rules',
  'context-pack',
];

const REQUIRED_COMMANDS = [
  'opsx-propose.md',
  'opsx-apply.md',
  'opsx-verify.md',
  'opsx-sync.md',
  'opsx-archive.md',
];

const REQUIRED_SKILLS = [
  'openspec-propose',
  'openspec-apply-change',
  'openspec-verify-change',
  'openspec-sync-specs',
  'openspec-archive-change',
];

function fail(message) {
  console.error(`OpenSpec workflow contract FAILED: ${message}`);
  process.exitCode = 1;
}

async function exists(rel) {
  try {
    await access(path.join(ROOT, rel), constants.R_OK);
    return true;
  } catch {
    return false;
  }
}

let version = '';
try {
  version = execFileSync('openspec', ['--version'], { encoding: 'utf8' }).trim();
} catch {
  fail('openspec CLI not available');
}
if (version !== '1.6.0') {
  fail(`expected OpenSpec 1.6.0, found ${version || '(empty)'}`);
}

for (const artifact of REQUIRED_ARTIFACTS) {
  if (!(await exists(path.join('openspec/changes', CHANGE, artifact)))) {
    fail(`missing apply-required artifact: ${artifact}`);
  }
}

for (const cap of REQUIRED_SPEC_CAPS) {
  const specPath = path.join('openspec/changes', CHANGE, 'specs', cap, 'spec.md');
  if (!(await exists(specPath))) {
    fail(`missing capability spec: ${cap}`);
  }
}

const proposal = await readFile(path.join(CHANGE_DIR, 'proposal.md'), 'utf8');
const design = await readFile(path.join(CHANGE_DIR, 'design.md'), 'utf8');
const tasks = await readFile(path.join(CHANGE_DIR, 'tasks.md'), 'utf8');
const methodology = await readFile(
  path.join(ROOT, 'docs/methodology/delivery-methodology.md'),
  'utf8',
);
const deviation = await readFile(
  path.join(ROOT, 'docs/methodology/deviation-policy.md'),
  'utf8',
);

for (const needle of [
  'Verify',
  'PASS',
  'sync',
  'archive',
  'propose',
  'apply',
]) {
  if (!`${proposal}\n${design}\n${tasks}`.toLowerCase().includes(needle.toLowerCase())) {
    fail(`change artifacts missing workflow keyword: ${needle}`);
  }
}

if (!methodology.includes('Verify exactly `PASS`') && !methodology.includes('exactly `PASS`')) {
  fail('delivery methodology must require Verify exactly PASS');
}
if (!deviation.includes('exactly `PASS`')) {
  fail('deviation policy must require Verify exactly PASS');
}
if (/PASS WITH NOTES/i.test(methodology + deviation) === false) {
  // Must explicitly reject PASS WITH NOTES somewhere in operating contract
  const agents = await readFile(path.join(ROOT, 'AGENTS.md'), 'utf8');
  if (!/PASS WITH NOTES/.test(agents + methodology + deviation)) {
    fail('operating docs must forbid PASS WITH NOTES as closure');
  }
}

for (const cmd of REQUIRED_COMMANDS) {
  if (!(await exists(path.join('.cursor/commands', cmd)))) {
    fail(`missing generated Cursor command: ${cmd}`);
  }
}

for (const skill of REQUIRED_SKILLS) {
  if (!(await exists(path.join('.cursor/skills', skill, 'SKILL.md')))) {
    fail(`missing generated Cursor skill: ${skill}`);
  }
  if (!(await exists(path.join('.codex/skills', skill, 'SKILL.md')))) {
    fail(`missing generated Codex skill: ${skill}`);
  }
}

// Binding checks
for (const id of ['w00', 'w00-s01', 'us-w00-s01-001', 'CURSOR', 'CODEX']) {
  if (!proposal.includes(id)) {
    fail(`proposal missing binding reference: ${id}`);
  }
}

const entries = await readdir(path.join(ROOT, '.cursor/commands'));
if (!entries.some((name) => name.startsWith('opsx-'))) {
  fail('no opsx-* commands found under .cursor/commands');
}

if (!process.exitCode) {
  console.log('OpenSpec workflow contract PASS (propose→apply→verify-PASS→sync→archive).');
}
