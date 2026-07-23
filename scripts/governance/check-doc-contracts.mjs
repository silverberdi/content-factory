#!/usr/bin/env node
/**
 * Doc contract checks for w00-s01:
 * - Active-slice User Stories bind the expected OpenSpec change ID
 * - Change proposal binds wave/slice/US/operators and excludes S02–S04 / future waves
 * - Governance docs assign CI automation to w00-s04
 */
import { readFile } from 'node:fs/promises';
import path from 'node:path';

const ROOT = process.cwd();
const CHANGE_ID = 'chg-w00-s01-repository-governance-and-openspec-foundation';
const ACTIVE_US = [
  'us-w00-s01-001',
  'us-w00-s01-002',
  'us-w00-s01-003',
  'us-w00-s01-004',
];

const S04_EXCLUSION_REQUIRED = [
  'GitHub Actions',
  'Nx validation',
  'CI-driven merge gates',
  'fully automated slice auto-merge',
  'w00-s04',
];

function fail(message) {
  console.error(`Doc contract check FAILED: ${message}`);
  process.exitCode = 1;
}

async function read(rel) {
  return readFile(path.join(ROOT, rel), 'utf8');
}

async function checkUserStoryBindings() {
  const catalog = await read('docs/backlog/user-stories/w00-user-stories.md');
  const backlog = await read('docs/backlog/backlog.md');
  const currentState = await read('docs/context/current-state.md');
  const contract = await read('docs/waves/w00-project-foundation/contract.md');

  if (!currentState.includes(`\`${CHANGE_ID}\``)) {
    fail(`current-state missing expected change ID ${CHANGE_ID}`);
  }

  for (const usId of ACTIVE_US) {
    if (!catalog.includes(usId)) {
      fail(`w00 user-story catalog missing ${usId}`);
    }
    const usBlockMatch = catalog.match(
      new RegExp(`## ${usId}[\\s\\S]*?(?=\\n## us-|$)`),
    );
    if (!usBlockMatch) {
      fail(`unable to isolate User Story block for ${usId}`);
      continue;
    }
    if (!usBlockMatch[0].includes(CHANGE_ID)) {
      fail(`${usId} does not bind OpenSpec change ${CHANGE_ID}`);
    }
    const backlogLine = backlog
      .split('\n')
      .find((line) => line.includes(usId));
    if (!backlogLine || !backlogLine.includes(CHANGE_ID)) {
      fail(`backlog missing binding for ${usId} / ${CHANGE_ID}`);
    }
  }

  if (!contract.includes(CHANGE_ID)) {
    fail(`w00 contract missing change ID ${CHANGE_ID}`);
  }
}

async function checkChangeScope() {
  const proposal = await read(
    `openspec/changes/${CHANGE_ID}/proposal.md`,
  );
  const tasks = await read(`openspec/changes/${CHANGE_ID}/tasks.md`);
  const design = await read(`openspec/changes/${CHANGE_ID}/design.md`);

  for (const usId of ACTIVE_US) {
    if (!proposal.includes(usId)) {
      fail(`proposal missing User Story binding ${usId}`);
    }
  }
  if (!proposal.includes('w00') || !proposal.includes('w00-s01')) {
    fail('proposal must bind wave w00 and slice w00-s01');
  }
  if (!proposal.includes('CURSOR') || !proposal.includes('CODEX')) {
    fail('proposal must identify CURSOR implementer and CODEX reviewer');
  }

  const combined = `${proposal}\n${design}\n${tasks}`;
  for (const phrase of S04_EXCLUSION_REQUIRED) {
    if (!combined.toLowerCase().includes(phrase.toLowerCase())) {
      fail(`change artifacts missing explicit S04 exclusion phrase: ${phrase}`);
    }
  }

  // Tasks must not schedule implementation of later w00 slices.
  const taskLines = tasks.split('\n').filter((line) => /^- \[[ x]\]/.test(line));
  for (const line of taskLines) {
    if (/\bimplement\b.*\bw00-s0[2-4]\b/i.test(line)) {
      fail(`task appears to implement later w00 slice: ${line.trim()}`);
    }
    if (/\b(create|build|add)\b.*\b(nx monorepo|docker compose|github actions)\b/i.test(line)
      && !/exclude|out of scope|not |never /i.test(line)) {
      fail(`task appears to pull S02–S04 implementation into S01: ${line.trim()}`);
    }
  }
}

async function checkGovernanceDocsExcludeCi() {
  const files = [
    'docs/methodology/delivery-methodology.md',
    'docs/governance/github-governance.md',
    'docs/waves/w00-project-foundation/contract.md',
  ];
  for (const rel of files) {
    const content = await read(rel);
    if (!content.includes('w00-s04')) {
      fail(`${rel} must explicitly assign CI automation to w00-s04`);
    }
    if (!/GitHub Actions/i.test(content)) {
      fail(`${rel} must mention GitHub Actions exclusion / w00-s04 ownership`);
    }
  }
}

await checkUserStoryBindings();
await checkChangeScope();
await checkGovernanceDocsExcludeCi();

if (!process.exitCode) {
  console.log('Doc contract checks PASS for w00-s01.');
}
