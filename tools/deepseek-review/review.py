#!/usr/bin/env python3
import argparse, json, os, pathlib, sys, urllib.request, urllib.error

parser = argparse.ArgumentParser()
parser.add_argument("--change", required=True)
parser.add_argument("--diff-file")
args = parser.parse_args()

root = pathlib.Path(__file__).resolve().parents[2]
prompt_path = root / ".agents/skills/deepseek-cross-review/references/reviewer-prompt.md"
system_prompt = prompt_path.read_text(encoding="utf-8")

if args.diff_file:
    diff = pathlib.Path(args.diff_file).read_text(encoding="utf-8")
else:
    diff = sys.stdin.read()

if not diff.strip():
    print("No diff supplied.", file=sys.stderr)
    sys.exit(2)

change_dir = root / "openspec/changes" / args.change
artifacts = []
for p in sorted(change_dir.rglob("*.md")) if change_dir.exists() else []:
    artifacts.append(f"\n--- {p.relative_to(root)} ---\n{p.read_text(encoding='utf-8')}")

canonical = []
for name in [
    "00_CANONICAL_AUTHORITY.md",
    "03_DOMAIN_MODEL_V2.md",
    "04_IDENTITY_SECURITY.md",
    "06_UX_UI_CONSTITUTION.md",
    "07_DASHBOARD_CONTROL_CENTER.md",
    "16_DEFINITION_OF_DONE.md",
]:
    p = root / "docs/canonical" / name
    if p.exists():
        canonical.append(f"\n--- {p.relative_to(root)} ---\n{p.read_text(encoding='utf-8')}")

user_content = f"""CHANGE: {args.change}

OPENSpec ARTIFACTS:
{''.join(artifacts)}

CANONICAL CONSTRAINTS:
{''.join(canonical)}

IMPLEMENTATION DIFF:
{diff}
"""

api_key = os.environ.get("DEEPSEEK_API_KEY")
if not api_key:
    env_file = root / ".env"
    if env_file.exists():
        for line in env_file.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line.startswith("DEEPSEEK_API_KEY="):
                api_key = line.split("=", 1)[1].strip().strip('"').strip("'")
                break

if not api_key:
    print("DEEPSEEK_API_KEY is not configured.", file=sys.stderr)
    sys.exit(3)

base = os.environ.get("DEEPSEEK_BASE_URL", "https://api.deepseek.com").rstrip("/")
model = os.environ.get("DEEPSEEK_REVIEW_MODEL", os.environ.get("DEEPSEEK_DEFAULT_MODEL", "deepseek-chat"))

payload = {
    "model": model,
    "messages": [
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": user_content},
    ],
    "temperature": 0.1,
}
req = urllib.request.Request(
    f"{base}/chat/completions",
    data=json.dumps(payload).encode("utf-8"),
    headers={
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    },
    method="POST",
)

try:
    with urllib.request.urlopen(req, timeout=180) as resp:
        data = json.loads(resp.read().decode("utf-8"))
except urllib.error.HTTPError as e:
    print(e.read().decode("utf-8", errors="replace"), file=sys.stderr)
    sys.exit(4)
except Exception as e:
    print(str(e), file=sys.stderr)
    sys.exit(5)

print(data["choices"][0]["message"]["content"])
