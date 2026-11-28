# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Audio:** `AudioChannel` (where a volume goes) with `MixerParameterChannel` (exposed AudioMixer parameter, in decibels) and `ListenerChannel` (`AudioListener.volume`), plus `VolumeSetting` and `MuteSetting`. Channels are restored when the settings service is disposed.
- **Editor:** Audio category in the Default Profile (master volume and mute on a listener channel), `DefaultProfileBuilder.CreateAsset`, and a profile warning for audio settings without a working channel.

## [0.1.0] - 2026-11-26

First release.

### Added

- **Settings:** `SettingDefinition<T>` and the `ToggleSetting`, `RangeSetting` and `ChoiceSetting` base types, plus `ChoiceOption`, `SettingCategory` and `ApplyMode`. `RangeSetting` constructor for subclasses with their own range and default.
- **Profile:** `SettingsProfile` with validation (missing entries, empty, invalid or duplicated ids, preset errors), `SettingId` rules and `SettingsProfile.Create` to build profiles at runtime.
- **Service:** `SettingsService` with loading, immediate and deferred settings, apply/revert pending changes, reset to defaults (all or per category), a single save per operation and change events.
- **Context and batches:** `SettingsContext` to read other settings' values and share services that live as long as the settings service. `IApplyBatch`, committed once per pass so related settings reach the engine in a single call.
- **Confirmation:** `ConfirmationRequest` with confirm, cancel and automatic revert after a real-time timeout. The saved file keeps the last confirmed value until then.
- **Presets:** `SettingsPreset` assets, the `PresetSetting` selector (shows the matching preset or "custom"), `ApplyPreset` and `ActivePreset`.
- **Persistence:** `SettingsData`, `ISettingsStorage`, `JsonFileSettingsStorage` (atomic writes, backup of corrupt files) and `InMemorySettingsStorage`.
- **Display settings:** `DisplayModeSetting` (per-platform modes) and `ResolutionSetting` (native by default, closest match for missing ones), applied together through `DisplayBatch`. `VSyncSetting`, `FrameRateLimitSetting` and `TextureQualitySetting`.
- **URP settings** (optional assembly): `UrpAssetHandle`, a runtime copy of the active URP Asset so the project's asset is never modified, plus `RenderScaleSetting`, `ShadowQualitySetting` and `MsaaSetting`.
- **Editor:** profile inspector with validation errors and warnings, typed drawer for preset values, and **Create > Mirador Works > Settings > Default Profile**. Extension points `SettingsProfileCheck` and `DefaultProfileContributor`, found automatically; the URP editor assembly uses them to add its settings and warn when the URP Asset has shadows disabled.
- **Sample "Basic Menu":** UGUI and TextMeshPro menu generated from a profile, with toggle, slider and dropdown rows, category headers, a confirmation dialog and a demo scene.
- **Documentation:** user manual (Spanish and English), design document, README (Spanish and English) and this changelog.
- **CI:** GameCI workflow running EditMode and PlayMode tests on a dedicated URP test project.

[Unreleased]: https://github.com/Goblanch/mw-settings-wizard/compare/v0.1.0...develop
[0.1.0]: https://github.com/Goblanch/mw-settings-wizard/releases/tag/v0.1.0
