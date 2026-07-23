#!/usr/bin/env node
import { readFile, writeFile, mkdir, access } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { constants } from 'node:fs';

const ROOT_MARKER = '# Generated Current Context Pack\n\n> Do not edit manually.\n';

const GLOBAL_SOURCES = [
  'docs/context/project-context.md',
  'docs/context/current-state.md',
  'docs/context/openspec-context-index.md',
  'docs/requirements/product-requirements.md',
  'docs/architecture/architecture.md',
  'docs/methodology/delivery-methodology.md',
  'docs/methodology/deviation-policy.md',
  'docs/methodology/evidence-standard.md',
  'docs/roadmap/roadmap.md',
  'docs/backlog/backlog.md',
  'docs/decisions/decision-register.md',
  'docs/governance/github-governance.md',
  'AGENTS.md',
  '.cursor/rules/00-project-governance.mdc',
  '.cursor/rules/30-delivery-evidence.mdc',
];

function parseField(markdown, label) {
  const escaped = label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const patterns = [
    new RegExp(`\\|\\s*${escaped}\\s*\\|\\s*\`([^\`]+)\`\\s*\\|`, 'i'),
    new RegExp(`^\\*\\*?${escaped}\\*\\*?\\s*:\\s*\`([^\`]+)\``, 'im'),
    new RegExp(`^${escaped}\\s*:\\s*\`([^\`]+)\``, 'im'),
  ];
  for (const pattern of patterns) {
    const match = markdown.match(pattern);
    if (match) return match[1].trim();
  }
  throw new Error(`Unable to parse current-state field: ${label}`);
}

function waveIdToSlug(waveId) {
  // Canonical machine wave IDs are lowercase kebab-case: w00, w01, …
  const match = /^w(\d{2})$/.exec(waveId);
  if (!match) {
    throw new Error(
      `Active wave ID must be lowercase kebab-case (wNN). Rejected: ${waveId}`,
    );
  }
  return match[1];
}

async function assertExists(path) {
  try {
    await access(path, constants.R_OK);
  } catch {
    throw new Error(`Required context source missing: ${path}`);
  }
}

async function resolveActiveSources(currentStateMarkdown) {
  const waveId = parseField(currentStateMarkdown, 'Active wave ID');
  const waveDirectory = parseField(currentStateMarkdown, 'Active wave directory');
  const waveSlug = waveIdToSlug(waveId);
  const userStoryCatalog = `docs/backlog/user-stories/w${waveSlug}-user-stories.md`;

  const activeSources = [
    `${waveDirectory}/contract.md`,
    `${waveDirectory}/execution-plan.md`,
    userStoryCatalog,
  ];

  for (const path of activeSources) {
    await assertExists(path);
  }

  return { waveId, waveDirectory, activeSources };
}

function sha256(content) {
  return createHash('sha256').update(content).digest('hex');
}

export function buildPack(entries) {
  const blocks = entries.map(
    ({ path, content }) => `\n\n---\n\n## SOURCE: ${path}\n\n${content.trim()}`,
  );
  return `${ROOT_MARKER}${blocks.join('')}\n`;
}

export async function collectContextSources() {
  const currentState = await readFile('docs/context/current-state.md', 'utf8');
  const { waveId, waveDirectory, activeSources } = await resolveActiveSources(currentState);

  const ordered = [...GLOBAL_SOURCES, ...activeSources];
  const unique = [];
  const seen = new Set();
  for (const path of ordered) {
    if (seen.has(path)) continue;
    seen.add(path);
    unique.push(path);
  }

  for (const path of unique) {
    await assertExists(path);
  }

  const entries = [];
  for (const path of unique) {
    const content = await readFile(path, 'utf8');
    entries.push({ path, content, sha256: sha256(content) });
  }

  return { waveId, waveDirectory, entries };
}

export async function generateContextPack() {
  const { waveId, waveDirectory, entries } = await collectContextSources();
  const pack = buildPack(entries);
  const manifest = {
    version: 1,
    activeWaveId: waveId,
    activeWaveDirectory: waveDirectory,
    packPath: 'docs/context/generated/current-context-pack.md',
    packSha256: sha256(pack),
    sources: entries.map(({ path, sha256: hash }) => ({ path, sha256: hash })),
  };

  await mkdir('docs/context/generated', { recursive: true });
  await writeFile('docs/context/generated/current-context-pack.md', pack);
  await writeFile(
    'docs/context/generated/context-manifest.json',
    `${JSON.stringify(manifest, null, 2)}\n`,
  );

  return manifest;
}

const isDirectRun =
  process.argv[1] &&
  (await import('node:url')).pathToFileURL(process.argv[1]).href === import.meta.url;

if (isDirectRun) {
  const manifest = await generateContextPack();
  console.log(
    `Generated context pack with ${manifest.sources.length} sources for ${manifest.activeWaveId}.`,
  );
}
