# Agent skills layout

Project skills live under [`.cursor/skills/`](../../.cursor/skills/), grouped by category (same taxonomy as [mattpocock/skills](https://github.com/mattpocock/skills)):

| Folder | Purpose |
|--------|---------|
| `engineering/` | Specs, tickets, TDD, code review, domain modeling, triage |
| `productivity/` | Grilling, handoff, teach, writing workflows |
| `personal/` | Personal workflows (e.g. Obsidian, editing) |
| `misc/` | Repo tooling (pre-commit, git guardrails, exercises) |
| `in-progress/` | Experimental or upstream WIP skills |
| `deprecated/` | Kept for reference; prefer newer alternatives |

Each skill is a folder with `SKILL.md` (required) and optional support files (`agents/openai.yaml`, reference docs, scripts).

## Version lock

[`skills-lock.json`](../../skills-lock.json) pins the upstream commit hash for each installed skill. Commit it with the skill files so everyone gets the same versions.

## Updating skills

Do **not** run `npx skills experimental_install` in this repo — that tool flattens skills into `.agents/skills/` and fights this layout.

To upgrade:

1. `npx skills@latest check` — see what changed upstream
2. `npx skills@latest add mattpocock/skills` — refresh the lock file
3. Re-run the repo install script (see below) or copy changed skill folders into `.cursor/skills/<category>/<name>/` using paths from `skills-lock.json` (`skillPath`).

Install / restore from lock file:

```powershell
.\scripts\install-cursor-skills.ps1
```

That script reads `skills-lock.json`, fetches from GitHub when needed, and writes into `.cursor/skills/` with categories preserved.

## Repo-specific agent config

- [`AGENTS.md`](../../AGENTS.md) — entry point for agents (issue tracker, domain docs)
- [`issue-tracker.md`](./issue-tracker.md), [`domain.md`](./domain.md), [`triage-labels.md`](./triage-labels.md) — per-repo settings from `/setup-matt-pocock-skills`
