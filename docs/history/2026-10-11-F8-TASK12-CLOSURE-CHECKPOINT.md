# F8 — Task 12 Closure Checkpoint

**Phase:** F8 Comentarios V1  
**Branch:** `feat/f8-comments-v1`  
**Stacked base:** `feat/f7-text-v1` @ `f4cd27a8e752071ca2ca4879b1eab84d52d188eb`  
**Status:** **AUTOMATED CLOSURE CANDIDATE** — this containing commit must pass exact-head Windows CI before final closure is claimed.  
**Manual Windows/stylus/touch/Adobe/Foxit/Edge QA:** **NOT RUN**.

## Task 12 additions

New regression/hardening file:

`tests/SGPdf.App.Tests/CommentHardeningTests.cs`

Hardening contract:

1. durable comment state graph contains no native handle types (`IntPtr`, `UIntPtr`, pointers) transitively;
2. existing-comment discovery remains bounded to the requested page;
3. a pre-canceled discovery call throws without poisoning the later uncanceled read/native-gate path.

No production code was required to satisfy these checks.

## Fresh Task-12 hardening CI before closure docs

Commit:
`2359dc793667f59266062b64eb12e0e0f4efe08c`

Windows CI:

- workflow `38104169562`;
- job `114366028415`;
- exact checkout `2359dc793667f59266062b64eb12e0e0f4efe08c`;
- repository hygiene PASS;
- Labelize staging PASS;
- locked dependency restore PASS;
- Release build **0 warnings / 0 errors**;
- **851 passed / 0 failed / 0 skipped**.

Because the whole test assembly ran, established LEER, FIRMAR, ORGANIZAR, EDITAR and ZPL regression suites remained green in the same run.

## Preservation/lifecycle prerequisite

Task 11 checkpoint:
`88fea2d0f0b4de2ccbb323620c2b45bdf6bba12c`

Exact-head Windows CI there was PASS with **848/848** tests.

Fresh F8 preservation evidence is recorded in:
`docs/history/2026-10-10-F8-preservation-matrix.md`.

It covers representative forms, bookmarks, named destinations, internal links, tagged structure, page labels, attachment exact payload, metadata, page rotation, supported comments and unsupported `STAMP` preservation/read-only behavior.

Lifecycle proven through independent reopen cycles:

`create → save → reopen → edit → save → reopen → delete → save → reopen`.

## Final pre-checkpoint branch scope audit

Compared:

- base: F7 closure `f4cd27a8e752071ca2ca4879b1eab84d52d188eb`;
- docs head immediately before this checkpoint: `5334c4a9749e5dd2fcb17293c09241df911655c4`.

Result:

- status: ahead;
- **66 commits ahead / 0 behind**;
- merge base exactly equals the F7 closure;
- no `.csproj` change;
- no `packages.lock.json` change;
- no unjustified runtime package;
- no second PDF editor engine;
- no runtime network/cloud/account/API-key/service layer;
- product changes remain constrained to F8 comment state/geometry/identity/writer/validator/preflight/UI, PDFium comment interop/discovery and the minimal reader-selection/native partial wiring required by that feature.

Documentation/planning was reconciled in the closure slice:

- `.planning/REQUIREMENTS.md` promotes `COMMENTS` to **AUTO PASS** only with exact evidence;
- `.planning/STATE.md` advances current project state to F8 closure;
- `.planning/ROADMAP.md` records Phase 9 F8;
- `.planning/phases/09-f8-comments/PLAN.md` records the 12-task execution contract;
- `docs/history/2026-10-11-F8.md` records durable F8 history.

## Architecture frozen at closure

- Independent `COMENTAR` mode.
- Real native `TEXT`, `SQUARE`, `CIRCLE`, `HIGHLIGHT`, `UNDERLINE`, `STRIKEOUT`, `INK` annotations.
- No comment rasterization into page content.
- Managed durable state; request-scoped native handles.
- Stable structural identity; annotation index not durable authority.
- Unsupported annotations preserved/read-only.
- Transactional Save As from fresh source with reopen/validation/source-race check/atomic publication/cleanup.
- Password-opened and cryptographically signed sources blocked.
- PDFium remains the only PDF editor engine.
- No merge to `main` without explicit user approval.

## Remaining closure gate

This checkpoint's containing commit must receive fresh exact-head Windows CI:

- repository hygiene PASS;
- locked restore PASS;
- Release build 0 warnings / 0 errors;
- all tests PASS.

After that automated gate:

1. create stacked **draft** PR `feat/f7-text-v1` → `feat/f8-comments-v1`;
2. verify PR open/draft/unmerged and exact head;
3. verify `main` remains unchanged;
4. stop F8.

## Manual gate

Still **NOT RUN** and deliberately separate from automated closure:

- real Windows mouse/stylus/touch interaction;
- zoom/rotation/overlap UX;
- dialogs/dirty guards by hand;
- heavy real PDFs;
- physical network-disabled smoke;
- manual reopen/interop in Adobe Acrobat Reader, Foxit and Edge.

Automated closure must never be reported as manual QA PASS.
