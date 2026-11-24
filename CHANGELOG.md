# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Design document (`Documentation~/DESIGN.md`).
- README (Spanish and English) and changelog.
- Package manifest and assembly definitions (core, optional URP, editor and tests).
- CI workflow with GameCI running EditMode and PlayMode tests on a dedicated URP test project.
- Setting definitions: `SettingDefinition<T>` and the `ToggleSetting`, `RangeSetting` and `ChoiceSetting` base types, plus `ChoiceOption`, `SettingCategory` and `ApplyMode`.
- Persistence: `SettingsData`, the `ISettingsStorage` interface, `JsonFileSettingsStorage` (atomic writes, backup of corrupt files) and `InMemorySettingsStorage`.
- `SettingsProfile` with validation (missing entries, empty, invalid or duplicated ids) and `SettingId` rules.
- `SettingsService`: loading, immediate and deferred settings, apply/revert pending changes, reset to defaults, saving and change events.
- `SettingsContext`: read other settings' values and share services that live as long as the settings service.
- Apply batches (`IApplyBatch`), committed once per apply pass so related settings reach the engine in a single call.
- Confirmation for settings marked `RequiresConfirmation`: `ConfirmationRequest` with confirm, cancel and automatic revert after a real-time timeout. The saved file keeps the last confirmed value until then.
- `SettingsProfile.Create` to build profiles at runtime.
- Presets: `SettingsPreset` assets, the `PresetSetting` selector (shows the matching preset or "custom"), `ApplyPreset` and `ActivePreset` on the service, and preset validation in the profile.
- `SettingsContext.Profile`.
- Display settings: `DisplayModeSetting` (per-platform modes) and `ResolutionSetting` (display resolutions, native by default, closest match for missing ones), applied together through `DisplayBatch` in a single `Screen.SetResolution` call.
  