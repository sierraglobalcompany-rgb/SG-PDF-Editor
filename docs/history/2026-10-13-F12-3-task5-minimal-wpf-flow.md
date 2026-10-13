# F12.3 Task 5 — Minimal WPF AcroForm flow

Date: 2026-10-13

## Result

Task 5 is functionally GREEN for the supported existing classic AcroForm text-field scope (`/Tx`). The UI reuses the existing `MainWindow` shell and right-side properties host, exposes only fields returned by `PdfAcroFormInspector`, and saves through the existing transactional `PdfAcroFormSaveService` using `Guardar como...`.

## Scope implemented

- Existing classic `/Tx` fields only.
- Focused `Campos de formulario...` entry under Edit.
- Existing right-side properties host reused; no second editor shell/window/sidebar.
- Values are loaded from `PdfAcroFormInspector.InspectTextFields`.
- Only fields whose values actually changed are sent to the writer.
- No-op save is skipped before opening `Guardar como...`.
- The original PDF remains the source; output is created through the existing transactional save service.
- Form UI state is reset when switching PDF or switching to the ZPL workspace.
- Form mode is mutually exclusive with FIRMAR, EDITAR and ORGANIZAR.
- Existing edit/organize/signature guards are reused when entering Forms; no new mode coordinator was introduced.

## TDD / hardening checkpoints

- Initial Task 5 RED: `37d60a1d5b25a27440e2ac4501415f8c1763dd59`.
- RED aligned to the existing runtime-UI pattern: `4a62d9035e632af4ddee94cbab7f9d296ff9d58f`.
- Minimal UI GREEN: `65dc4d31cf79b5ec3019ed650d942145bea61e5f`; workflow `38444540427`; 1170/1170 PASS.
- Hardening RED for changed-fields-only + workspace reset: `4debb2937c68fc162f656ffdc9c0399bff879df8`; workflow `38445453673`; build clean, 1170 baseline PASS + 2 intended failures.
- Hardening GREEN: `2476ee017c62ca68f85e06ae2fce4967f2e54c60`; workflow `38447153597`; 1172/1172 PASS.
- Mode-exclusivity RED: `d72c9a8c3edaf05e6a62f0cbfc8850ec2a5a2bd0`; workflow `38448279524`; build clean, 1172 baseline PASS + 1 intended failure.
- Mode-exclusivity GREEN / functional Task 5 HEAD: `85aaefd3f4dc8b1822cc145d3dc44a26ae6c2d8a`; workflow `38449772950`; job `115477374426`; build 0 warnings / 0 errors; 1173/1173 PASS, 0 skipped.

## KISS / Ley del Ruido

PASS.

The Task 5 implementation adds one focused `MainWindow.Forms.cs` partial and narrow lifecycle hooks to existing Reader/Labels/Sign/Edit/Organize paths. It does not add a new PDF engine, package, backend, network dependency, telemetry, MVVM/controller/view-model framework, form designer, second editor shell or generic mode coordinator.

The hardening work removed two concrete integration risks instead of adding architecture: stale form state across document/workspace changes, and overlapping form/sign/edit/organize property modes.

## Supported / not supported

AUTO-proven for this slice:
- existing classic text fields `/Tx` already exposed by the inspector;
- hierarchical text-field names already supported by Tasks 2–4;
- `Guardar como...` transactional publication through the existing service.

Still out of scope / NOT RUN:
- checkbox/radio (`/Btn`);
- combo/list (`/Ch`);
- XFA;
- JavaScript/calculations/actions;
- signature fields as form fields;
- flattening;
- form creation/designer;
- advanced form styling;
- universal compatibility/preservation claims.

## Closure boundary

Task 5 is functionally GREEN at `85aaefd3f4dc8b1822cc145d3dc44a26ae6c2d8a`. This history update closes Task 5 documentation only. Task 6 remains a separate exact-head closure gate for F12.3 and must not start another professional slice or merge without explicit approval.
