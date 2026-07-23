#!/usr/bin/env node
/**
 * Run all automated checks applicable to w00-s01.
 */
import { spawnSync } from 'node:child_process';

const steps = [
  ['scripts/context/validate-machine-ids.mjs'],
  ['scripts/governance/check-doc-contracts.mjs'],
  ['scripts/governance/check-agent-rules.mjs'],
  ['scripts/governance/check-openspec-workflow.mjs'],
  ['--test', 'scripts/context/context-pack.test.mjs'],
  ['scripts/context/check-context-pack.mjs'],
];

let failed = false;
for (const args of steps) {
  console.log(`\n→ node ${args.join(' ')}`);
  const result = spawnSync(process.execPath, args, { stdio: 'inherit' });
  if (result.status !== 0) {
    failed = true;
    console.error(`FAILED: node ${args.join(' ')}`);
  }
}

if (failed) {
  process.exit(1);
}
console.log('\nAll w00-s01 automated checks PASS.');
