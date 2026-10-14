# SG PDF Editor — Release UI Modernization

**Status:** release-polish work OPEN; F0–F12 functional baseline remains frozen.

## Goal

Modernize SG PDF Editor from default/legacy-looking WPF controls to a coherent Windows 11 / Fluent desktop experience without changing PDF behavior, feature scope, persistence, file formats, or runtime architecture.

## Evidence / root cause

- The original `App.xaml` had no shared visual resources.
- `MainWindow.xaml` and signature/password dialogs largely relied on default WPF styling and repeated inline margins/padding/colors.
- The result was functionally correct but visually inconsistent and dated.

## Reference inputs

- Microsoft WPF Fluent theme (`ThemeMode="System"`) for .NET 10.
- `nextlevelbuilder/ui-ux-pro-max-skill` (MIT) as **development/design guidance only**.
- The reference repository is **not** a runtime dependency and must not be bundled with SG PDF Editor.

## Official application / PDF file icon

User-approved visual reference received in chat on **2026-10-14**.

Identity:
- stylized red PDF/document mark;
- white negative-space page/fold shapes;
- simple high-contrast silhouette suitable for Windows application/file icons.

Intended uses:
1. SG PDF Editor executable/application icon;
2. Windows PDF file-association icon when SG PDF Editor is registered as a PDF handler;
3. installer/start-menu/shortcut icon later in the release sequence.

**Important:** the user-provided image is the visual master reference, but the binary icon asset is **not yet committed to the repository**. When implementation resumes, derive the Windows `.ico` asset from that supplied reference at appropriate multi-resolution sizes; do not redesign its identity without user approval.

## Design direction

Professional desktop document editor; neutral Windows-native shell; restrained accent use; strong hierarchy; compact controls; clear hover/pressed/focus/disabled states; Segoe UI/system typography; soft surfaces and borders; no decorative gradients, neumorphism, glass-heavy effects, or web-like card overload.

## KISS architecture

1. Native WPF Fluent with `ThemeMode="System"`.
2. One small app-level resource dictionary for SG PDF tokens and reusable variants.
3. Native WPF controls; no third-party UI framework.
4. Apply shared styles progressively to main shell, then dialogs.
5. Preserve existing element names/event handlers and behavior-owned shell structure.

## Explicit non-goals

- No MahApps, MaterialDesign, HandyControl, WinUI migration, Avalonia migration, web shell, MVVM rewrite, command-system rewrite, icon package, animation framework, or navigation framework.
- No functional redesign of F0–F12.
- No installer work until the primary visual pass has real Windows screenshots/QA.

## Release visual gates

- Native Fluent System theme.
- Coherent main toolbar/navigation/panels/document surface/status hierarchy.
- Consistent buttons/inputs/combos and interaction states.
- Visible keyboard focus.
- Usable at 1366×768 and higher, including Windows scaling.
- Signature/password and other dialogs share the same design language.
- Official icon integrated for app/file-association packaging.
- Build/tests remain green; Windows screenshot QA required before final visual PASS.

## Checkpoint — 2026-10-14

Branch: `feat/release-ui-modernization`

Base checkpoint: `25ac6e163662207bbc40157d1bed3811d1974e62`

### UI-1 Foundation — GREEN

Implemented:
- `App.xaml` uses `ThemeMode="System"`;
- `Themes/Modern.xaml` created;
- small shared visual token/style layer;
- no new UI package or runtime framework.

Exact-head verification for UI-1 implementation:
- implementation HEAD: `198b278605d33bff61be0ee5a9097d359239b324`;
- workflow run `38599058194`;
- job `115973245330`;
- hygiene PASS;
- runtime staging PASS;
- locked restore PASS;
- build PASS;
- full tests PASS.

### UI-2 Main shell — RED / OPEN

Current contract HEAD before this checkpoint: `383413bff2cdf6dca130ccb6bd1379f2ad89cc06`.

RED contract requires:
- preserve `DockPanel` root and three-column reader shell structure;
- preserve behavior-owned control names;
- apply shared modern surface resources to window/navigation/canvas/right panel.

Workflow run `38599600200` was still executing the test step when this checkpoint was requested. **Do not treat UI-2 as GREEN.**

## Resume point

When work resumes:
1. inspect the final result/logs of run `38599600200` and confirm a valid UI-2 RED;
2. implement the minimal main-shell visual changes only;
3. run exact-head CI to GREEN;
4. continue dialogs;
5. materialize/integrate the user-approved icon;
6. perform real Windows screenshot QA;
7. only then resume portable publish/installer work.
