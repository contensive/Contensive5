# Help Documentation & Video Project — Plan

**Created:** 2026-08-28
**Status:** Draft for review — foundational decisions made, execution not yet started
**Owner:** Jay
**Scope:** Contensive5 core + all `ao*` addon projects in `C:\Git`
**Related repos:** `Contensive5`, `aoHelpCenter`, all `ao*` addon repos

## Purpose

Produce a matched pair of help document + help video for every meaningful feature in Contensive and its addons, aimed at two audiences — site admins and developers — and get them hosted somewhere customers will actually find them. This plan is the roadmap for the decisions and work that gets us there. It reflects a first pass through `Contensive5/patterns`, the existing `Contensive5/patterns/help-doc-pattern.md`, and the `aoHelpCenter` repo, plus a scan of all 77 `ao*` projects for existing help content.

## Decisions made (this session)

These four are locked in and drive everything below. Revisit them only if something in execution proves them wrong.

| Decision | Choice | Why |
|---|---|---|
| Document structure | **Hybrid** — short "what this is" intro, a compact reference table for fields/buttons, then 1–3 numbered task walkthroughs for the most common goals | Pure reference reads like a spec no one enjoys; pure tutorial leaves questions about the fields it didn't cover. Hybrid lets the video mirror just the walkthrough part, keeping video length sane. |
| Hosting | **Centralize on contensive.com** | Matches the new monthly-upgrade policy — one current copy to maintain instead of one per customer site. Docs stay authored in each repo alongside the code; a new publish step in the build pipeline pushes them to the central site instead of (or in addition to) bundling them into the site install. |
| Tone/voice | **Plain-spoken and encouraging**, one voice for both admin and developer content, technical vocabulary allowed to increase for dev docs | Contensive's customer base leans non-technical site admins. A single consistent voice is also easier for both a small writing team and an AI actor script to sustain. |
| Sequencing | **Fix the 7 addons that already have some help content, then move to the highest-usage addons**, long tail last | Cheapest wins first (editing beats writing from scratch), then value where the most customers will see it. |

## What already exists (research findings)

Contensive already has a documented, working help system — this project extends and re-platforms it, it doesn't invent it.

**Architecture today** (`patterns/help-doc-pattern.md`, implemented in `aoHelpCenter` as the "Help Center" collection): every addon collection can ship markdown help files from a `helpfiles/` folder at the repo root, sorted into role subfolders (`admin/`, `dev/`, `member/`, plus a public root and a `fieldhelp/` folder for database-field documentation). The build script (`Invoke-ContensiveBuild` in `Contensive5/scripts/build-addon-collection.psm1`) already has a `HelpFilesPath` parameter that zips this folder into `helpFiles.zip` and the collection XML installs it as a `helpfiles`-type resource — landing on each customer's site at `privateFiles/helpFiles/{collection}.{topic}.{article}.md`, namespaced automatically by collection name. A per-site "Help Center" addon (`Contensive.HelpCenter`) then indexes every installed file with OpenAI embeddings and serves navigation, keyword fallback, and AI-answered search at `/help` on that site, re-indexing on every collection install via a background task (`HelpCenterReindexProcess`).

In other words: the authoring pattern (markdown in `helpfiles/`, role-based folders, period-delimited filenames for nav placement) is already solid and doesn't need to change. What needs to change is what happens to that folder at publish time, and where the indexing/search/nav code runs.

**Current content inventory** — of 77 `ao*` projects, only 7 have any help content at all:

| Addon | Existing .md files |
|---|---|
| aoDesignBlocks | 30 |
| aoEcommerce | 26 |
| aoMCP | 10 |
| aoHelpCenter | 5 |
| aoBlog | 1 |
| aoMeetingManager | 1 |
| aoToolPanel | 1 |

Contensive5 core also has its own `helpfiles/` (admin/dev/member folders) covering base platform concepts (Addons, Website, CLI, Remote Methods, etc.) — this is effectively the "core platform concepts" content and should be treated as its own track alongside the addon-by-addon work.

**A taxonomy inconsistency worth fixing before mass-producing content:** the pattern doc (`help-doc-pattern.md`) specifies role folders as `helpfiles/`, `helpfiles/members/`, `helpfiles/admin/`, `helpfiles/dev/`, `helpfiles/fieldhelp/`. But `aoHelpCenter`'s own published help content (`Managing Help Content.md`) describes `helpfiles/guests/`, `helpfiles/members/`, `helpfiles/admin/`, `helpfiles/dev/`, `fieldhelp/` — and the actual folders on disk in both `Contensive5` and `aoHelpCenter` are `admin/`, `dev/`, `member/` (singular, no `guests/` or root-level public folder present). Pick one naming convention and correct the pattern doc, the Help Center's own docs, and the folder structure before the writing team starts producing 70+ addons' worth of content in the wrong folders.

## Open decisions still needed

The four locked-in decisions set direction; these still need a call from Jay (defaults proposed — flag any you'd change):

1. **Central site information architecture.** Where on contensive.com does help live — `contensive.com/help/{addon}/{topic}/{article}`? Does it need the same role-gating (guest/member/admin/dev) contensive.com can't easily enforce the way a logged-in customer site can, since a visitor to contensive.com isn't authenticated into any customer's site. *Proposed default:* admin/dev content is still openly readable on contensive.com (it's product documentation, not customer data) — gating by role only matters on a live customer site where "admin" means "admin of this data." Flag if that's wrong.
2. **Publish mechanism.** The build pipeline already zips `helpfiles/` into a site-install resource. Centralizing needs a *second*, new publish step — pushing the same folder to contensive.com instead of/in addition to that. Simplest option: a small authenticated upload endpoint on contensive.com that accepts a collection's `helpFiles.zip` and re-indexes just that collection's content, called from `build-addon-collection.psm1` (or a new deploy step) after a successful build. *Needs its own short technical design once the writing standards below are locked* — not blocking on content work starting.
3. **Fate of per-site `aoHelpCenter`.** Deprecate it once contensive.com is live and monthly upgrades guarantee currency, or keep it running for admins who want in-context help without leaving their site? *Proposed default:* keep the per-site `/help` UI (admins like not leaving their dashboard) but point its search/content at the central store instead of re-indexing locally on every site — avoids 1 OpenAI indexing job × N customer sites. This is a bigger technical lift and should be phase 2 of the pipeline work, not a blocker for phase 1 (get content flowing to contensive.com at all).
4. **Video length ↔ doc length pairing rule.** Proposed default in the template below: a reference-only doc (no walkthrough, e.g. a settings-panel page) gets a 60–90 second video; a doc with 1 task walkthrough gets 90–150 seconds; a doc with 2–3 walkthroughs gets up to 3–4 minutes, split into chapters. Longer than that, split into multiple docs/videos rather than one long one.
5. **Review workflow.** Who checks technical accuracy (presumably whoever owns that addon) vs. voice/tone consistency (one editor across everything, to keep 70+ addons sounding like one product)? Proposed: technical review by addon owner, tone/style pass by one designated editor before anything is scripted for McFerre.
6. **Re-record cadence.** Docs are cheap to patch; videos are not. Proposed: a doc edit that doesn't change the on-screen steps/UI shown in the video doesn't trigger a re-record; a UI or workflow change does. Track this with a simple flag in the tracking sheet (see Phase 0).
7. **Success metric.** How will you know this worked — search usage on contensive.com, support-ticket deflection, video view/completion rate on YouTube, or just "coverage" (% of addons documented)? Worth picking one primary metric before Phase 1 so the pilot can be judged.

## Standards to lock in during Phase 0

### Help document template (hybrid structure)

```
# {Feature Name}

*One or two sentences: what this is and who it's for (admin/dev).*

## What you can do here
- Plain-language list of the 2–4 things this feature is for (prose, not a spec)

## Reference
| Field / Button | What it does |
|---|---|
| ... | ... |
(Only the fields worth naming — skip anything self-explanatory)

## How to: {most common task}
1. Step
2. Step
3. Step

## How to: {second most common task, if there is one}
1. Step
...

## Related
- Links to related help docs
```

Keep each doc scoped to one feature/screen — the AI search indexer chunks by heading, so a focused doc indexes and answers better than a sprawling one (this is already documented in `help-doc-pattern.md`'s "Writing Help Content for AI Search" section — follow it).

### Video script template (for McFerre)

Scripts need to give the third-party tool everything it needs without narrating decisions the tool can't act on. One row per beat:

| Beat / Time | McFerre says (verbatim VO) | On-screen | Editor notes |
|---|---|---|---|
| 0:00–0:08 (hook) | ... | Screen recording starts on {screen} | |
| ... | ... | Callout/overlay text: "..." | |
| Outro | "...and that's it — the full write-up is linked below." | Show URL card | Link to the paired help doc's contensive.com URL |

Notes fields to always fill in: which screen/state to be on before recording starts, exact click path if the tool needs it spelled out, any on-screen text overlays (McFerre's VO shouldn't have to carry information better shown as text), and the outro CTA linking back to the doc. Title/description/thumbnail text goes at the top of the script, not buried in the beats.

### Tone one-pager (for both writers and McFerre scripts)

Short sentences. Say "you" and "your site," not "the user" and "the application." Explain the why in one clause when it's not obvious ("Turn this off if you don't want visitors leaving comments" beats "Disables the comment field"). No jargon in admin-facing docs unless the UI itself uses that word. Never say "simply" or "just" — if it were simple they wouldn't be reading a help doc.

## Phased roadmap

**Phase 0 — Foundation (before any content is produced at scale)**
Fix the role-folder naming inconsistency; finalize and circulate the doc + video templates above; build a tracking sheet (addon, doc status, video status, last-reviewed date, needs-re-record flag); resolve open decisions #1–#3 enough to know where Phase 1 content will actually be published; pick the success metric.

**Phase 1 — Pilot (prove the pattern end to end)**
Pick 2–3 addons — recommend `aoHelpCenter` itself (dogfooding, and it already has the most content to fix) plus one from the "fix existing" list like `aoEcommerce` or `aoDesignBlocks`. Take them fully through: rewrite/clean docs to the hybrid template → script and produce videos → publish to contensive.com (even manually, if the pipeline isn't built yet) → review against the success metric. This validates the templates and the doc↔video length pairing before committing to 70+ more addons.

**Phase 2 — Fix existing (the other 5 of the 7)**
`aoMCP`, `aoBlog`, `aoMeetingManager`, `aoToolPanel`, and the remainder of `aoEcommerce`/`aoDesignBlocks`/`aoHelpCenter` content not covered in the pilot.

**Phase 3 — Highest-usage addons (proposed list — confirm or replace with real usage/support-ticket data if you have it)**
Ecommerce (done in Phase 1/2), Membership Manager, CRM, Events, Contact Manager, Newsletter, Article Library, Catalog, Form Wizard, Page Builder — the addons most likely to be installed on a broad range of customer sites. Confirm this ranking against actual install counts or ticket volume before committing; it's a guess based on addon names, not data.

**Phase 4 — Long tail**
Everything else, in the fix-existing-first spirit: any addon that already has partial docs from Phase 1/2 spillover gets finished before starting a fresh addon from zero.

**Phase 5 — Maintenance (ongoing, tied to the monthly upgrade cadence)**
A doc/video review pass folded into the monthly upgrade cycle for whichever addons changed that month — not a full re-review of all 70+ every month, just the ones with code changes. This is where the re-record-cadence decision (#6) actually gets used.

## Publish pipeline — target state summary

Today: `helpfiles/` → zipped by `build-addon-collection.psm1` → installed as a site resource on every customer site → indexed locally by each site's Help Center task.

Target: `helpfiles/` stays exactly where it is and is authored the same way (no change to the writing pattern) → a new publish step pushes it to contensive.com instead of/in addition to the site-install path → contensive.com holds the one current, indexed copy → (phase-2 pipeline work, not blocking) per-site `/help` UIs, if kept, query the central index rather than re-indexing locally. This is a real engineering task that deserves its own short design doc once Phase 0/1 confirm the content standards — it is **not** a prerequisite to starting Phase 1 content work, which can publish manually while the pipeline is built in parallel.

## Immediate next actions

1. Fix the role-folder naming inconsistency (decide `helpfiles/` vs `helpfiles/guests/` for public content, `member/` vs `members/`) and correct `patterns/help-doc-pattern.md` plus `aoHelpCenter`'s own docs to match.
2. Confirm or adjust the open decisions above (#1–#7), at least enough to start Phase 1.
3. Pick the 2–3 Phase 1 pilot addons and start with one help doc + one video script, so the templates get pressure-tested on real content before scaling.
