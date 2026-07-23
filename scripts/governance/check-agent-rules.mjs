#!/usr/bin/env node
/**
 * Contract check: required project-owned agent rule files exist and contain
 * mandatory governance keywords/gates for w00-s01.
 */
import { access, readFile } from 'node:fs/promises';
import { constants } from 'node:fs';
import path from 'node:path';

const ROOT = process.cwd();

const REQUIRED = [
  {
    path: 'AGENTS.md',
    keywords: [
      'Roadmap → Wave → Slice → User Stories → OpenSpec tasks',
      'wave/*',
      'slice/*',
      'Verify must be exactly `PASS`',
      'Deviation synchronization procedure',
      'Never expose or commit secrets',
      'Never use destructive reset',
      'Never delegate repository file edits to Silverio',
      'READY_TO_MERGE',
      'CHANGES_REQUIRED',
    ],
  },
  {
    path: '.cursor/rules/00-project-governance.mdc',
    keywords: [
      'check-context-pack.mjs',
      'current-context-pack.md',
      'future-wave',
      'Verify must be exactly `PASS`',
      'Do not ask Silverio to edit',
    ],
  },
  {
    path: '.cursor/rules/30-delivery-evidence.mdc',
    keywords: [
      'check-context-pack.mjs',
      'Verify exactly `PASS`',
      'READY_TO_MERGE',
      'archived change',
      'Slice PRs target wave branches',
    ],
  },
  {
    path: 'openspec/config.yaml',
    keywords: [
      'Roadmap -> Wave -> Slice -> User Stories -> OpenSpec tasks',
      'PASS',
      'READY_TO_MERGE',
      'wave/*',
      'slice/*',
      'Never push directly',
    ],
  },
];

const IMMUTABLE_TREES = [
  '.cursor/commands',
  '.cursor/skills',
  '.codex/skills',
];

function fail(message) {
  console.error(`Agent rules contract check FAILED: ${message}`);
  process.exitCode = 1;
}

async function assertReadable(rel) {
  try {
    await access(path.join(ROOT, rel), constants.R_OK);
  } catch {
    fail(`missing required file: ${rel}`);
    return false;
  }
  return true;
}

for (const entry of REQUIRED) {
  if (!(await assertReadable(entry.path))) continue;
  const content = await readFile(path.join(ROOT, entry.path), 'utf8');
  for (const keyword of entry.keywords) {
    if (!content.includes(keyword)) {
      fail(`${entry.path} missing mandatory keyword/gate: ${keyword}`);
    }
  }
}

for (const tree of IMMUTABLE_TREES) {
  if (!(await assertReadable(tree))) {
    fail(`generated OpenSpec integration tree missing: ${tree}`);
  }
}

if (!process.exitCode) {
  console.log('Agent rules contract checks PASS.');
}
