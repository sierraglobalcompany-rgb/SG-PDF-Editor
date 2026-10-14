# SG PDF Editor — Release UI Modernization

**Status:** UI-1/UI-2/UI-3 automated GREEN; UI-4 real Windows visual QA OPEN.

## Goal

Modernize SG PDF Editor from default/legacy-looking WPF controls to a coherent Windows 11 / Fluent desktop experience without changing PDF behavior, feature scope, persistence, file formats, or runtime architecture.

## Design / architecture

- Native WPF Fluent with `ThemeMode="System"` on .NET 10.
- One small app-level `Themes/Modern.xaml` resource dictionary.
- Native WPF controls only; no third-party runtime UI framework.
- Existing `x:Name`, handlers and F0–F12 behavior remain frozen.
- Deferred `DynamicResource` use is intentional where application-level resources must remain safe for direct test construction.
- Main shell and dialogs reuse shared SG surface/button/text/border variants.

## Explicit non-goals

No MahApps, MaterialDesign, HandyControl, WinUI/Avalonia migration, web shell, MVVM rewrite, command-system rewrite, icon package, animation framework, navigation framework, or functional redesign of F0–F12.

## UI-1 Foundation — GREEN

Implementation anchor: `198b278605d33bff61be0ee5a9097d359239b324`.

Implemented:
- `App.xaml` uses `ThemeMode="System"`;
- `Themes/Modern.xaml` defines the shared SG resource layer;
- no new runtime UI package/framework.

Exact-head CI: run `38599058194` — SUCCESS.

## UI-2 Main shell — GREEN

Final correction HEAD: `fb5413d84fb5e2e733a791a44c6688b6d01a7acd`.

Root cause resolved:
- eager app-level `StaticResource` lookup failed when existing tests instantiated `MainWindow` without bootstrapping `App.xaml`;
- the minimal correction retained the same SG resource keys and changed shell consumption to deferred `DynamicResource` where required;
- no duplicate theme dictionary, no test bootstrap, no framework addition, no functional rewrite.

Exact-head CI: run `38630414847` — SUCCESS.

## UI-3 Dialog consistency — GREEN

RED contract HEAD: `494115e0e84fe9d923c3503da21a0be9ab83540f`.

RED evidence:
- build PASS;
- 1209 existing tests PASS;
- exactly 5 new expected failures, one per dialog, because shared dialog visual resources were not yet applied.

GREEN implementation HEAD: `60bb0f73090cd4d3e24d5eab5c69ee707432f823`.

Changed only the approved dialog XAML surfaces:
- `PdfPasswordDialog.xaml`;
- `SignatureDrawDialog.xaml`;
- `SignatureLibraryDialog.xaml`;
- `SignatureNameDialog.xaml`;
- `SignaturePhotoDialog.xaml`.

Implemented:
- shared SG window surface;
- shared primary/secondary button hierarchy;
- shared border/header/muted-text resources where applicable;
- white signature drawing canvas and checkerboard transparency previews preserved;
- no C# behavior, package, handler or control-name change.

Exact-head CI: run `38632179198` — SUCCESS.
Full suite: `1214/1214 PASS`, 0 failed, 0 skipped.

## UI-4 Windows visual QA — READY / NOT RUN

Canonical execution matrix:
`docs/release/2026-10-14-ui-visual-qa.md`

Automated implementation is ready for manual rendering validation, but no real Windows screenshot/interactivity evidence has been recorded yet.

Required gate includes:
- Windows light and dark system theme;
- 1366×768 at 100% scaling;
- one higher-resolution/scaled profile;
- visible keyboard focus;
- clear disabled state;
- main shell, loaded PDF, label workflow, password dialog and signature dialogs;
- no critical clipping, unreadable surfaces, detached handlers or crash.

**Current status for every UI-4 manual case: `NOT RUN`.** CI must not be used to promote these cases to PASS.

## Official application / PDF icon

User-approved visual identity exists from chat:
- red stylized PDF/document mark;
- white negative-space page/fold shapes;
- high-contrast silhouette.

Intended uses:
1. application/executable icon;
2. PDF file-association icon;
3. installer/start-menu/shortcut icon.

The binary `.ico` asset is still **not committed**. Materialization requires the approved visual master to be available as an actual source image; do not redraw or substitute it silently.

## Remaining release-adjacent gates

1. run and record UI-4 real Windows visual QA;
2. materialize/integrate the approved icon asset when its source image is available;
3. resolve `System.Security.Cryptography.Pkcs 8.0.1` NU1902/NU1903 by root cause;
4. deterministic self-contained `win-x64` publish;
5. runtime inventory + clean/offline smoke;
6. one minimal installer + installer smoke;
7. RC and remaining representative Packs A–D.

`main` remains read-only until explicit user approval.
