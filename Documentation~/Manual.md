# MiradorWorks Settings Wizard — Manual

> Package version: 0.1.0 · Unity 6000.0+ · URP 17 (optional)
>
> 🇪🇸 [Versión en español](Manual.es.md)

This manual explains how to install the package, use it in a game and extend it with your own settings. If you want to know **why** it is built this way, see the [design document](DESIGN.md) (Spanish).

## Contents

1. [What it is](#1-what-it-is)
2. [Installation](#2-installation)
3. [Quick start](#3-quick-start)
4. [Concepts](#4-concepts)
5. [Included settings](#5-included-settings)
6. [Using the service](#6-using-the-service)
7. [Building your own UI](#7-building-your-own-ui)
8. [Saving](#8-saving)
9. [Creating your own settings](#9-creating-your-own-settings)
10. [Extending the Editor](#10-extending-the-editor)
11. [Testing your settings](#11-testing-your-settings)
12. [Troubleshooting](#12-troubleshooting)
13. [Limitations](#13-limitations)

---

## 1. What it is

A game settings system (display and graphics) that:

- **Only includes the settings you pick.** Each game has a *profile* with its settings; anything not in the profile does not exist.
- **Is extended by inheriting from a class.** A new setting is a class of a few lines, and the UI renders it without changes.
- **Does not impose a UI.** The package provides data and events; you build the UI. A sample menu is included.
- **Saves to a JSON file** by default and connects to any save system.
- **Is localizable** with Unity Localization.

---

## 2. Installation

### Requirements

- Unity **6000.0** or newer.
- **Unity Localization** 1.5.x. It is installed automatically as a dependency.
- **URP 17** for the graphics settings. It is optional: without URP, the rest of the package works and the URP settings do not show up.
- Platform: **PC** (Windows, macOS, Linux).

### Steps

1. **Window → Package Manager → + → Install package from git URL…** and paste:

   ```
   https://github.com/Goblanch/mw-settings-wizard.git#v0.1.0
   ```

   The `#v0.1.0` pins the version. Without it, you would install whatever is on the default branch, which can change at any time.

2. **Set up Localization** if your project does not have it yet: **Edit → Project Settings → Localization → Create**. Without this step, empty labels show the asset name.

3. (Optional) Import the sample: **Package Manager → MiradorWorks Settings Wizard → Samples → Basic Menu → Import**.

---

## 3. Quick start

### 3.1 Create the profile

In the Project window, right-click the folder you want → **Create → Mirador Works → Settings → Default Profile**.

This creates a **Game Settings** folder with:

- one asset per available setting, with its id and mode already set;
- the **Display** and **Graphics** categories;
- the **Low**, **Medium** and **High** presets, and the preset selector;
- the **Settings Profile** that groups everything.

Select the profile: the bottom of the inspector should say **"The profile is valid."**. You can remove the settings your game does not need from the list.

### 3.2 Create the service

The service is the object that holds the values and applies them. Create it **once**, at your game's entry point:

```csharp
using MiradorWorks.Settings;
using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] SettingsProfile settingsProfile;

    public SettingsService Settings { get; private set; }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Settings = new SettingsService(settingsProfile, new JsonFileSettingsStorage());
        Settings.Initialize();
    }

    void OnDestroy() => Settings?.Dispose();
}
```

- **`Initialize()`** loads what was saved, fixes invalid values and applies every setting.
- **`Dispose()`** releases whatever the service created (for example, the copy of the URP Asset).

The package has **no singletons**: you decide how the service reaches whoever needs it. It can be a serialized reference to `GameBootstrap`, an `Init(...)` method or your dependency container.

### 3.3 Show a menu

The fastest way is the sample:

1. Open the `BasicMenuDemo` scene from the imported sample.
2. Select the **Demo** object and drag your profile to **Profile**.
3. Press Play.

For your own UI, see [section 7](#7-building-your-own-ui).

---

## 4. Concepts

### Definition and value

Each setting has two parts that the package keeps apart:

| | Where it lives | What it holds |
| --- | --- | --- |
| **Definition** | An asset (`ScriptableObject`) | Id, label, category, default value, limits, how it is applied. It does not change during play. |
| **Value** | The `SettingsService` | What the player chose. |

They are kept apart because, in the Editor, whatever you change in a ScriptableObject during Play Mode stays written in the asset. If the value lived in the definition, every test run would change the defaults.

### Setting types

There are three base types, and each one maps to a UI control:

| Type | Value | Control |
| --- | --- | --- |
| `ToggleSetting` | `bool` | Toggle |
| `RangeSetting` | `float` (with min, max and step) | Slider |
| `ChoiceSetting` | `string` (the option's key) | Dropdown |

`ChoiceSetting`s save the option's **key** (`"2560x1440@144"`), never its position in the list: the list can change if the player changes monitor.

For special cases you can inherit directly from `SettingDefinition<T>` with any type (see [9.5](#95-your-own-value-type)).

### The id

Each setting has a hand-written **id** (`graphics.shadows`). It is the key it is saved under.

- Format: 1 to 64 characters among ASCII letters, digits, `.`, `_` and `-`.
- **Do not change it after your game ships.** If you do, the players' saved value is lost.
- Renaming the asset does not matter, because the id is not derived from the name.

### Profile

The `SettingsProfile` is the list of settings and presets of your game. The inspector shows errors as they happen: empty, duplicated or invalid ids, repeated settings, misconfigured presets… If the profile has errors, `Initialize()` throws an exception with the full list.

### Apply modes

- **Immediate:** the change is applied as soon as the player makes it. Right for VSync, textures or render scale.
- **Deferred:** the change stays **pending** until `ApplyPending()` is called (the "Apply" button). `RevertPending()` discards it. Right for resolution or display mode.

### Confirmation

A setting with **Requires Confirmation** asks for confirmation when it is applied ("Keep these changes?"). If the player does not confirm before time runs out (15 s by default), it goes back to the previous value.

- The countdown uses **real time**: it works even when the game is paused with `Time.timeScale = 0`.
- While waiting for confirmation, the saved file **keeps the previous value**. If the game crashes with a resolution the monitor does not support, the next launch uses the last one that worked.

### Presets

A `SettingsPreset` is an asset with values for several settings (Low, Medium, High…). The `PresetSetting` is a dropdown with the profile's presets plus the **custom** option:

- Choosing a preset applies its values (respecting each setting's mode).
- The selector **is computed automatically**: it shows the first preset that matches the current values, or `custom` if none does. If the player changes a setting in the preset, it switches to `custom` on its own.

### Passes

Each service operation (`Set`, `ApplyPending`, a reset…) is a **pass**. Everything it causes happens at the end of the pass, in this order:

1. Batches are applied (for example, a single `Screen.SetResolution` call even if both resolution and mode changed).
2. The preset selector is recomputed.
3. Settings are saved, once.
4. Events are raised.

That is why, when your UI receives an event, the change is already real.

---

## 5. Included settings

| Setting | Class | Suggested id | Type | Suggested mode | What it does |
| --- | --- | --- | --- | --- | --- |
| Display mode | `DisplayModeSetting` | `display.mode` | Choice | Deferred + confirmation | Borderless, exclusive fullscreen (Windows only), maximized window (macOS only) or windowed. |
| Resolution | `ResolutionSetting` | `display.resolution` | Choice | Deferred + confirmation | The monitor's resolutions, without duplicates, highest first. Native by default. |
| VSync | `VSyncSetting` | `display.vsync` | Toggle | Immediate | `QualitySettings.vSyncCount`. |
| Frame rate limit | `FrameRateLimitSetting` | `display.fps` | Choice | Immediate | `Application.targetFrameRate`. Ignored while VSync is on. |
| Textures | `TextureQualitySetting` | `graphics.textures` | Choice | Immediate | Texture resolution: full, half, quarter or eighth. |
| Render scale | `RenderScaleSetting` | `graphics.renderscale` | Range | Immediate | Internal render resolution (0.5–2). |
| Shadows | `ShadowQualitySetting` | `graphics.shadows` | Choice | Immediate | Distance, cascades and shadow map resolutions per level. |
| MSAA | `MsaaSetting` | `graphics.msaa` | Choice | Immediate | Multisample anti-aliasing: off, 2×, 4× or 8×. |
| Preset | `PresetSetting` | `graphics.preset` | Choice | — | Preset selector. |

The last three graphics settings are URP ones (`MiradorWorks.Settings.URP`).

### Options of each setting

**Display mode.** Keys `borderless`, `exclusive`, `maximized` and `windowed`. Each platform only shows its own. If a Windows player has `exclusive` saved and opens the game on Linux, the default is used.

**Resolution.**

- **Key:** `WxH@Hz`, for example `2560x1440@144`. Hz are rounded: 143.998 Hz is saved as 144.
- **Minimum Height** (720 by default): hides lower resolutions. If the monitor has no resolution that tall, all of them are shown.
- **Include Refresh Rate:** if you turn it off, each size shows up once, with its highest refresh rate.
- **If the saved resolution no longer exists** (because the player changed monitor), the closest one is picked.

**Frame rate limit.**

- **Rates** (30, 60, 120, 144 and 240 by default), plus the `unlimited` option, which is also the default.
- **With VSync on, Unity ignores this limit.** It makes sense to disable the control in your UI while VSync is on (see [7.4](#74-settings-that-depend-on-others)).

**Textures.** Keys `full`, `half`, `quarter` and `eighth`. Textures marked *Ignore Mipmap Limit* in their importer are not affected, which is useful for UI and icons.

**Shadows.**

- Levels are edited in the inspector. By default:

| Key | Distance | Cascades | Main light | Additional lights |
| --- | --- | --- | --- | --- |
| `off` | 0 | 1 | 256 | 256 |
| `low` | 25 | 1 | 1024 | 512 |
| `medium` | 50 | 2 | 2048 | 1024 |
| `high` | 100 | 4 | 4096 | 2048 |

- If you do not set a default key, the last level is used.
- **Important:** your project's URP Asset must have *Lighting → Main Light → Cast Shadows* enabled. If it is disabled, the build does not include the shadow shaders and no level can turn shadows on. The profile inspector warns you.

**MSAA.** Keys `off`, `2x`, `4x` and `8x`.

### URP settings do not modify your URP Asset

On first use, the service makes an **in-memory copy** of the active URP Asset and sets it as the pipeline. The settings modify that copy. On `Dispose()`, the original asset is restored. Your project's asset never changes, neither in the Editor nor in a build.

---

## 6. Using the service

### Reading and changing values

```csharp
bool vsyncOn = settings.Get(vsync);          // what the player sees (pending value if there is one)
bool applied = settings.GetApplied(vsync);   // what is actually applied

settings.Set(vsync, true);
settings.Set(resolution, "2560x1440@144");
settings.Set(renderScale, 0.85f);
```

- **`Set` fixes the value** if it is not valid: it clamps it to the range, snaps it to the step or, if the option does not exist, uses the closest one or the default.
- **If the value does not change, `Set` does nothing:** it does not apply, save or raise events.
- **The setting must be in the profile.** Otherwise, `Set` throws `ArgumentException`.

### Pending changes

```csharp
settings.Set(resolution, "1920x1080@60");   // Deferred: stays pending

bool any = settings.HasPendingChanges;                    // true
bool resolutionPending = settings.HasPending(resolution); // true

settings.ApplyPending();   // apply every pending change (and save once)
settings.RevertPending();  // or discard them
```

### Resetting values

```csharp
settings.ResetToDefaults();          // everything
settings.ResetToDefaults(graphics);  // only one category
```

Each setting respects its mode: deferred ones stay pending.

### Presets

```csharp
settings.ApplyPreset(highPreset);
SettingsPreset active = settings.ActivePreset;   // null = "custom"

// With the selector (same thing, through the dropdown):
settings.Set(presetSelector, "high");
```

### Events

```csharp
settings.ValueChanged += setting => { /* what Get returns changed: refresh that control */ };
settings.ValueApplied += setting => { /* the value became the applied one */ };
settings.ConfirmationRequested += request => confirmationDialog.Show(request);
```

- **`ValueChanged`** is raised by any change in what `Get` returns: an applied value, a pending one, a revert or a reset.
- **`ValueApplied`** is raised when the value becomes applied. It is not raised during `Initialize()`.
- **If a handler throws,** the exception is logged and the other handlers still run.

### Unsupported settings

```csharp
if (!settings.IsSupported(shadows))
    shadowsRow.SetActive(false);
```

A setting is unsupported when it makes no sense in the current setup. For example, URP settings in a project without URP. Its value is still saved, but not applied.

### Confirmation timeout

```csharp
settings.ConfirmationTimeoutSeconds = 10f;   // default: 15
```

---

## 7. Building your own UI

### 7.1 The idea

Your UI only needs to render **three controls**: toggle, slider and dropdown. Go through the profile and pick the control by the setting's base type:

```csharp
foreach (SettingDefinition setting in settings.Profile.Settings)
{
    if (!settings.IsSupported(setting))
        continue;

    switch (setting)
    {
        case ToggleSetting toggle: CreateToggle(toggle); break;
        case RangeSetting range: CreateSlider(range); break;
        case ChoiceSetting choice: CreateDropdown(choice); break;
    }
}
```

That way, a new setting that inherits from one of the three types shows up in your menu without changing the UI.

### 7.2 The three controls

**Toggle:**

```csharp
toggle.SetIsOnWithoutNotify(settings.Get(setting));
toggle.onValueChanged.AddListener(value => settings.Set(setting, value));
```

**Slider:**

```csharp
slider.minValue = setting.Min;
slider.maxValue = setting.Max;
slider.SetValueWithoutNotify(settings.Get(setting));
slider.onValueChanged.AddListener(value =>
{
    settings.Set(setting, value);
    slider.SetValueWithoutNotify(settings.Get(setting)); // snaps to the setting's step
});
```

**Dropdown:**

```csharp
IReadOnlyList<ChoiceOption> options = setting.GetOptions(settings.Context);

var labels = new List<string>();
foreach (ChoiceOption option in options)
{
    labels.Add(option.IsLocalized && !option.LocalizedLabel.IsEmpty
        ? option.LocalizedLabel.GetLocalizedString()
        : option.RawLabel ?? option.Key);
}

dropdown.ClearOptions();
dropdown.AddOptions(labels);
dropdown.SetValueWithoutNotify(ChoiceSetting.IndexOf(options, settings.Get(setting)));
dropdown.onValueChanged.AddListener(index => settings.Set(setting, options[index].Key));
```

- **Use `...WithoutNotify`** when showing a value that comes from the service. Otherwise the control would raise its own event, call `Set` again and you would end up in a loop.
- **Option labels** can be localized (`LocalizedLabel`, for fixed options like "Borderless") or plain text (`RawLabel`, for generated ones like "2560 × 1440 @ 144 Hz").
- **The setting's label** is in `setting.Label`, and its category's in `setting.Category.Label`.

### 7.3 Keeping the UI up to date

```csharp
settings.ValueChanged += changed =>
{
    if (changed == setting)
        toggle.SetIsOnWithoutNotify(settings.Get(setting));

    applyButton.interactable = settings.HasPendingChanges;
};
```

This is needed because some changes do not come from that control: a preset, a reset, a confirmation that reverts or a setting that changes the preset selector.

### 7.4 Settings that depend on others

Unity ignores the frame rate limit while VSync is on, so it is a good idea to disable that control:

```csharp
void RefreshFrameRateRow() =>
    frameRateDropdown.interactable = !settings.Get(vsync);

settings.ValueChanged += changed =>
{
    if (changed == vsync)
        RefreshFrameRateRow();
};
```

### 7.5 Confirmation dialog

```csharp
settings.ConfirmationRequested += request =>
{
    dialog.SetActive(true);
    keepButton.onClick.AddListener(request.Confirm);
    revertButton.onClick.AddListener(request.Cancel);

    // Completed is also raised when time runs out.
    request.Completed += confirmed =>
    {
        keepButton.onClick.RemoveAllListeners();
        revertButton.onClick.RemoveAllListeners();
        dialog.SetActive(false);
    };
};

// Every frame, while the dialog is open:
countdownText.text = $"Reverting in {Mathf.CeilToInt(request.RemainingSeconds)} s";
```

`settings.ActiveConfirmation` returns the request in progress, or `null` if there is none.

### 7.6 The Basic Menu sample

A complete menu with UGUI and TextMeshPro, and the fastest starting point:

| Piece | What it does |
| --- | --- |
| `SettingsMenu` | Goes through the profile, creates a header per category and a row per supported setting. Wires Apply, Revert and Reset. |
| `SettingRow` | Base of the rows: sets the (localized) label and refreshes on `ValueChanged`. |
| `ToggleRow`, `SliderRow`, `DropdownRow` | One row per base type. |
| `CategoryHeader` | Category title. |
| `ConfirmationDialog` | "Keep these changes?" with a countdown. |
| `BasicMenuDemo` | Example entry point: creates the service and gives it to the menu. |

- **To change the look,** edit the prefabs.
- **For your own setting types,** inherit from `SettingsMenu` and override `PrefabFor` (see [9.5](#95-your-own-value-type)).
- **It is an example:** copy it and adapt it to your game, do not use it as is.

---

## 8. Saving

### 8.1 By default: a JSON file

`JsonFileSettingsStorage` saves to `Application.persistentDataPath/settings.json`. On Windows that is `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\settings.json`:

```json
{
    "version": 1,
    "entries": [
        { "id": "display.mode", "value": "borderless" },
        { "id": "display.resolution", "value": "2560x1440@144" },
        { "id": "display.vsync", "value": "true" },
        { "id": "graphics.renderscale", "value": "0.85" }
    ]
}
```

- **Atomic writes:** it first writes `settings.json.tmp` and then replaces the file. Closing the game halfway through a save does not corrupt the file.
- **Corrupt files:** if it cannot be read, a copy is made at `settings.json.bak` and the defaults are used.
- **Custom path:** `new JsonFileSettingsStorage("full/path/to/file.json")`.
- **Ids no longer in the profile:** they are kept when saving. If you remove a setting and add it back, its value is still there.

### 8.2 Connecting your save system

Implement `ISettingsStorage`:

```csharp
using System;
using MiradorWorks.Settings;
using UnityEngine;

public sealed class MySaveSystemSettingsStorage : ISettingsStorage
{
    const string Key = "settings";

    readonly MySaveSystem saves;

    public MySaveSystemSettingsStorage(MySaveSystem saves) => this.saves = saves;

    public bool TryLoad(out SettingsData data)
    {
        data = null;
        try
        {
            if (!saves.TryRead(Key, out string json))
                return false;

            data = JsonUtility.FromJson<SettingsData>(json);
            return data != null;
        }
        catch (Exception)
        {
            data = null;
            return false; // TryLoad must never throw: the game starts with defaults instead.
        }
    }

    // It may throw: the settings service catches the error and logs it.
    public void Save(SettingsData data) => saves.Write(Key, JsonUtility.ToJson(data));
}
```

```csharp
Settings = new SettingsService(settingsProfile, new MySaveSystemSettingsStorage(saveSystem));
```

**The contract:**

- `TryLoad` **never** throws: if something fails, it returns `false`.
- `Save` **may** throw. The service logs it and the value stays applied for the session.
- **Settings are global.** Save them in a shared entry, not inside each save slot.

If your system does not store text, you can go through `data.Entries` (each with `Id` and `Value`) and rebuild the data with `new SettingsData()` and `data.Set(id, value)`.

### 8.3 In memory

`InMemorySettingsStorage` does not write to disk. It is meant for tests and prototypes (see [11](#11-testing-your-settings)).

---

## 9. Creating your own settings

In every case there are three steps:

1. Create the class, inheriting from the right base type.
2. Create the asset from your `CreateAssetMenu` entry and give it an **id**, label, category and mode.
3. Add it to the profile.

The service loads it, saves it and applies it, and your UI (or the sample) renders it without changes.

### 9.1 A toggle

A gameplay setting the game reads when it needs it. It does not touch the engine, so `Apply` is empty:

```csharp
using MiradorWorks.Settings;
using UnityEngine;

[CreateAssetMenu(menuName = "My Game/Settings/Camera Shake")]
public sealed class CameraShakeSetting : ToggleSetting
{
    // The camera reads the value; nothing to apply to the engine.
    public override void Apply(bool value, SettingsApplyContext context) { }
}
```

```csharp
public sealed class CameraShaker : MonoBehaviour
{
    [SerializeField] CameraShakeSetting cameraShake;
    SettingsService settings;

    public void Init(SettingsService settingsService) => settings = settingsService;

    public void Shake(float strength)
    {
        if (!settings.Get(cameraShake))
            return;
        // ...
    }
}
```

The default value is set in the asset's **Default Value** field.

### 9.2 A slider with its own range

Volume of an Audio Mixer channel. Audio is not part of the package, but it is a good extension example:

```csharp
using MiradorWorks.Settings;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "My Game/Settings/Mixer Volume")]
public sealed class MixerVolumeSetting : RangeSetting
{
    [SerializeField] AudioMixer mixer;
    [Tooltip("Name of the exposed parameter in the Audio Mixer.")]
    [SerializeField] string exposedParameter = "MasterVolume";

    // Range 0–1 in steps of 0.05, 0.8 by default. Values saved in the asset still win.
    public MixerVolumeSetting() : base(min: 0f, max: 1f, step: 0.05f, defaultValue: 0.8f) { }

    public bool HasMixer => mixer != null;

    public override void Apply(float value, SettingsApplyContext context)
    {
        if (mixer == null)
            return;

        // The mixer works in decibels: 0 dB = full volume, -80 dB = silence.
        float decibels = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        mixer.SetFloat(exposedParameter, decibels);
    }
}
```

- **The constructor with `base(...)`** sets the range and default for new assets. If you later change the values in the inspector, the inspector wins.
- **`AudioMixer.SetFloat` has no effect when called in `Awake` of the first scene.** If you use this setting, initialize the service in `Start`.

### 9.3 A dropdown with fixed options

Anisotropic filtering with three localizable options:

```csharp
using System.Collections.Generic;
using MiradorWorks.Settings;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "My Game/Settings/Anisotropic Filtering")]
public sealed class AnisotropicFilteringSetting : ChoiceSetting
{
    public const string Off = "off";
    public const string On = "on";
    public const string Forced = "forced";

    [SerializeField] LocalizedString offLabel = new LocalizedString();
    [SerializeField] LocalizedString onLabel = new LocalizedString();
    [SerializeField] LocalizedString forcedLabel = new LocalizedString();

    public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context) => new[]
    {
        ChoiceOption.Localized(Off, offLabel),
        ChoiceOption.Localized(On, onLabel),
        ChoiceOption.Localized(Forced, forcedLabel)
    };

    public override void Apply(string value, SettingsApplyContext context) =>
        QualitySettings.anisotropicFiltering = value switch
        {
            On => AnisotropicFiltering.Enable,
            Forced => AnisotropicFiltering.ForceEnable,
            _ => AnisotropicFiltering.Disable
        };
}
```

- **Keys cannot change after you ship,** because they are what gets saved. Labels can.
- **The default value** is the inspector's **Default Key** or, if it is empty, the first option.

### 9.4 A dropdown with generated options

When the options depend on the system (monitors, installed languages…):

```csharp
public override IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context)
{
    var options = new List<ChoiceOption>();
    for (int i = 0; i < Display.displays.Length; i++)
        options.Add(ChoiceOption.Text(i.ToString(), $"Display {i + 1}"));
    return options;
}

// If the saved key no longer exists, which option is closest?
protected override string FindClosest(string key, IReadOnlyList<ChoiceOption> options) =>
    options.Count > 0 ? options[0].Key : null;
```

- **`ChoiceOption.Text`** is for generated labels that do not need translating.
- **`FindClosest`** is optional. If you do not override it, a key that no longer exists falls back to the default.
- **`GetOptions` is called often.** If building the options is expensive, cache them.

`ResolutionSetting` is the full example of this case.

### 9.5 Your own value type

If none of the three types fits, inherit from `SettingDefinition<T>`. For example, the crosshair color:

```csharp
using MiradorWorks.Settings;
using UnityEngine;

[CreateAssetMenu(menuName = "My Game/Settings/Crosshair Color")]
public sealed class CrosshairColorSetting : SettingDefinition<Color>
{
    [SerializeField] Color defaultColor = Color.white;

    public override Color GetDefault(SettingsContext context) => defaultColor;

    // Must not throw: turn any value into a valid one.
    public override Color Sanitize(Color value, SettingsContext context)
    {
        value.a = 1f;
        return value;
    }

    public override void Apply(Color value, SettingsApplyContext context) { } // the HUD reads it

    public override string Serialize(Color value) => "#" + ColorUtility.ToHtmlStringRGBA(value);

    // Must not throw: return false if the text cannot be read.
    public override bool TryDeserialize(string raw, out Color value)
    {
        if (!string.IsNullOrEmpty(raw) && ColorUtility.TryParseHtmlString(raw, out value))
            return true;

        value = default;
        return false;
    }
}
```

The methods you have to implement:

| Method | What for | Rule |
| --- | --- | --- |
| `GetDefault` | Initial value. | May depend on the runtime. |
| `Sanitize` | Turn any value into a valid one. | Must not throw. |
| `Apply` | Apply it to the engine or the game. | May be empty. |
| `Serialize` / `TryDeserialize` | Turn it into text and read it back. | `TryDeserialize` must not throw. Use `CultureInfo.InvariantCulture` for numbers. |
| `AreEqual` (optional) | Compare two values. | Uses `Equals` by default. |

**Your UI does not know how to render this type,** so it needs its own row. With the sample:

```csharp
using MiradorWorks.Settings;
using MiradorWorks.Settings.Samples;
using UnityEngine;
using UnityEngine.UI;

public sealed class ColorRow : SettingRow
{
    [SerializeField] Image preview;
    [SerializeField] Button nextButton;
    [SerializeField] Color[] palette = { Color.white, Color.green, Color.cyan, Color.magenta };

    CrosshairColorSetting Setting => (CrosshairColorSetting)Definition;

    protected override void OnBind()
    {
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(Next);
    }

    protected override void Refresh() => preview.color = Service.Get(Setting);

    void Next()
    {
        int index = System.Array.IndexOf(palette, Service.Get(Setting));
        Service.Set(Setting, palette[(index + 1) % palette.Length]);
    }
}

public sealed class MySettingsMenu : SettingsMenu
{
    [SerializeField] ColorRow colorRowPrefab;

    protected override SettingRow PrefabFor(SettingDefinition setting) =>
        setting is CrosshairColorSetting ? colorRowPrefab : base.PrefabFor(setting);
}
```

### 9.6 Settings that depend on others

From `GetDefault`, `Sanitize`, `IsSupported` or `Apply` you can read other settings' values:

```csharp
[SerializeField] VSyncSetting vsync;

public override void Apply(string value, SettingsApplyContext context)
{
    if (context.Settings.TryGetValue(vsync, out bool vsyncOn) && vsyncOn)
        return; // nothing to do while VSync is on
    // ...
}
```

- `TryGetValue` (the value the player sees) and `TryGetApplied` (the applied value).
- Also by id: `context.Settings.TryGetValue<bool>("display.vsync", out bool on)`. The asset reference is better, because it keeps working if the id changes.
- **During `Initialize()`, settings are loaded in profile order.** If the other setting comes later, `TryGetValue` returns `false`. If you need it, put the setting you depend on first in the profile.

### 9.7 Batches: several settings, one call

If several settings end up in **the same engine call**, do not make it in each `Apply`. Use a batch: each setting writes its part, and the call is made once at the end of the pass.

```csharp
public sealed class BloomBatch : IApplyBatch
{
    public bool? Enabled { get; set; }
    public float? Intensity { get; set; }

    public void Commit(SettingsContext context)
    {
        // One call with both values. Whatever no setting provided in this pass keeps its current value.
    }
}

// In BloomSetting.Apply:
context.GetBatch<BloomBatch>().Enabled = value;

// In BloomIntensitySetting.Apply:
context.GetBatch<BloomBatch>().Intensity = value;
```

- **Every setting in the same pass gets the same batch,** because it is looked up by type.
- **The batch needs a parameterless constructor.** The service creates it the first time someone asks for it.
- **Do not keep the `SettingsApplyContext`** beyond `Apply`: it stops working when the pass ends.

`DisplayBatch` (resolution and display mode) is the package's example.

### 9.8 Shared objects

If several settings need **the same object** (a copy of an asset, a handle to a system…), ask the context for it. Do not use a singleton:

```csharp
RuntimeVolumeHandle handle = context.Settings.GetOrAddService(() => new RuntimeVolumeHandle());
```

- **It is created the first time** someone asks for it, and everyone else gets the same object.
- **It lives as long as the service.** If it implements `IDisposable`, it is disposed with it.

`UrpAssetHandle.Get(context.Settings)` is the package's example.

### 9.9 Where to put the code

- **Your settings go in a runtime assembly** that references `MiradorWorks.Settings` (and `MiradorWorks.Settings.URP` if you use URP types).
- **[Section 10](#10-extending-the-editor) code goes in an Editor-only assembly** that also references `MiradorWorks.Settings.Editor`.

---

## 10. Extending the Editor

### 10.1 Adding your settings to the default profile

```csharp
using MiradorWorks.Settings;
using MiradorWorks.Settings.Editor;

internal sealed class MyGameSettingsContributor : DefaultProfileContributor
{
    public override int Order => 100;   // after the package's settings

    public override void Contribute(DefaultProfileBuilder builder)
    {
        SettingCategory gameplay = builder.Category("gameplay", order: 2);

        builder.AddSetting<CameraShakeSetting>("gameplay.camerashake", gameplay,
            configure: so => so.FindProperty("defaultValue").boolValue = true);

        var aniso = builder.AddSetting<AnisotropicFilteringSetting>("graphics.aniso",
            builder.Category(DefaultProfileBuilder.GraphicsCategory, order: 1));
        builder.AddPresetValue(DefaultProfileBuilder.Low, aniso, AnisotropicFilteringSetting.Off);
        builder.AddPresetValue(DefaultProfileBuilder.High, aniso, AnisotropicFilteringSetting.On);
    }
}
```

- **It is found automatically:** no need to register it anywhere.
- **`configure`** receives the `SerializedObject` of the newly created asset, to fill in its other fields.
- **`Category`** returns the existing category with that id, or creates it.

### 10.2 Your own warnings in the profile inspector

```csharp
using System.Collections.Generic;
using MiradorWorks.Settings;
using MiradorWorks.Settings.Editor;

internal sealed class MixerAssignedCheck : SettingsProfileCheck
{
    public override void Collect(SettingsProfile profile, List<string> warnings)
    {
        foreach (SettingDefinition setting in profile.Settings)
        {
            if (setting is MixerVolumeSetting volume && !volume.HasMixer)
                warnings.Add($"'{volume.name}' has no Audio Mixer assigned.");
        }
    }
}
```

Warnings show up in yellow, below the validation errors.

### 10.3 Editing preset values

A preset's inspector shows the right control for each value: toggle, slider or dropdown. With your own type (9.5) it shows the text exactly as it is saved; in the color example, `#RRGGBBAA`.

---

## 11. Testing your settings

Build the profile in code with an in-memory storage:

```csharp
using MiradorWorks.Settings;
using NUnit.Framework;
using UnityEngine;

public sealed class CameraShakeSettingTests
{
    CameraShakeSetting cameraShake;
    SettingsProfile profile;
    SettingsService service;
    InMemorySettingsStorage storage;

    [SetUp]
    public void SetUp()
    {
        cameraShake = ScriptableObject.CreateInstance<CameraShakeSetting>();
        JsonUtility.FromJsonOverwrite("{\"id\":\"gameplay.camerashake\",\"defaultValue\":true}", cameraShake);

        profile = SettingsProfile.Create(new SettingDefinition[] { cameraShake });
        storage = new InMemorySettingsStorage();
        service = new SettingsService(profile, storage);
        service.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        service.Dispose();
        Object.DestroyImmediate(profile);
        Object.DestroyImmediate(cameraShake);
    }

    [Test]
    public void IsOnByDefaultAndIsSaved()
    {
        Assert.That(service.Get(cameraShake), Is.True);

        service.Set(cameraShake, false);

        Assert.That(storage.SaveCount, Is.EqualTo(1));
    }
}
```

- **`JsonUtility.FromJsonOverwrite`** writes private serialized fields (like the id), also outside the Editor.
- **`InMemorySettingsStorage.SaveCount`** tells you how many times it saved.
- **Tests that change the engine** (`QualitySettings`, `Screen`, URP) go in **PlayMode**, and must restore the values in `TearDown`. Unity restores Quality Settings when leaving Play Mode, but in EditMode the changes would stay written in the project.
- **In EditMode tests, destroy with `DestroyImmediate`,** not `Destroy`.

---

## 12. Troubleshooting

| Symptom | Cause | Fix |
| --- | --- | --- |
| The shadows setting does not turn shadows on in the build. | The URP Asset has shadows disabled and the build stripped those shaders. | Enable *Lighting → Main Light → Cast Shadows* in the URP Asset. The profile inspector warns about it. |
| Resolution or display mode do not change in the Editor. | The Game view is not a real window. | Test it in a build. |
| VSync does not cap the frame rate in the Editor. | Unity only uses VSync in the player. | Test it in a build. |
| The frame rate limit does nothing. | VSync is on. | Turn it off (or disable the control in your UI). |
| Exception on start: *"Settings profile … is invalid"*. | The profile has errors. | Check the profile inspector: it shows each error. |
| *"'X' is not part of settings profile"*. | That setting is not in the service's profile. | Add it to the profile or use the asset that is. |
| Labels show the asset name. | The `LocalizedString` is empty or Localization is not set up. | Set up Localization and fill in the labels. |
| Settings are not saved. | Write error (permissions, disk). | Look for *"Could not save settings"* in the console. |
| URP graphics go back to their values after changing quality level. | `QualitySettings.SetQualityLevel` changes the pipeline and the copy stops being used. | Do not change quality level while the system is active (see [13](#13-limitations)). |
| *"Saved value … is invalid. Using the default."* | The saved file has a value that cannot be read. | It is a warning: the default is used and the game goes on. |

---

## 13. Limitations

What v0.1.0 does not do:

- **Audio and controls.** Audio can be added with your own setting (see 9.2).
- **FXAA/SMAA, HDR, upscaling, soft shadows and post-processing.**
- **Consoles, mobile and HDRP.** Display settings are marked unsupported outside PC.
- **Changing quality level** while the system is active: the URP Asset copy stops being used.
- **Hot monitor changes:** resolution options are computed when requested, but there is no event that reports the change.
- **Automatic initialization:** the service is always created from your code.
