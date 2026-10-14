# SG PDF Editor — Release UI Modernization

**Status:** approved release-polish work; F0–F12 functional baseline remains frozen.

## Goal

Modernize SG PDF Editor from default/legacy-looking WPF controls to a coherent Windows 11 / Fluent desktop experience without changing PDF behavior, feature scope, persistence, file formats, or runtime architecture.

## Evidence / root cause

- `App.xaml` currently has no shared visual resources.
- `MainWindow.xaml` and signature/password dialogs largely rely on default WPF styling and repeated inline margins/padding/colors.
- The result is functionally correct but visually inconsistent and dated.

## Reference inputs

- Microsoft WPF Fluent theme (`ThemeMode="System"`) for .NET 9+ / .NET 10.
- `nextlevelbuilder/ui-ux-pro-max-skill` (MIT) as **development/design guidance only**. Its WPF guidance explicitly favors XAML, resource dictionaries, implicit/shared styles, visible focus, scalable layout, and Fluent theme usage.
- The reference repository is **not** a runtime dependency and must not be bundled with SG PDF Editor.

## Design direction

Professional desktop document editor; neutral Windows-native shell; restrained accent use; strong hierarchy; compact controls; clear hover/pressed/focus/disabled states; Segoe UI/system typography; soft surfaces and borders; no decorative gradients, neumorphism, glass-heavy effects, or web-like card overload.

## KISS architecture

1. Enable native WPF Fluent theme with `ThemeMode="System"`.
2. Add one small app-level resource dictionary for SG PDF visual tokens and reusable variants.
3. Reuse native WPF controls; no third-party UI framework.
4. Apply shared styles progressively to the main shell, then dialogs.
5. Keep all existing element names and event handlers unless a proven UX defect requires otherwise.

## Explicit non-goals

- No MahApps, MaterialDesign, HandyControl, WinUI migration, Avalonia migration, web shell, MVVM rewrite, command-system rewrite, icon package, animation framework, or navigation framework.
- No functional redesign of F0–F12.
- No installer work until the primary visual pass has real Windows screenshots/QA.

## Release visual gates

- App uses native Fluent System theme.
- Main shell has coherent toolbar/navigation/panels/document surface/status hierarchy.
- Buttons/inputs/combos have consistent spacing and interaction states.
- Focus remains visible for keyboard users.
- Layout remains usable at 1366×768 and higher, including Windows scaling.
- Signature/password and other user-facing dialogs inherit the same design language.
- Build/tests remain green; Windows screenshot QA is required before declaring UI release gate PASS.

## First bounded slice

**UI-1 Foundation:** Fluent System theme + one shared resource dictionary + stable visual tokens/styles. No behavioral code changes.