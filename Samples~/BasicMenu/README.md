# Basic Menu sample

A settings menu built with UGUI and TextMeshPro that is generated from a `SettingsProfile`: one header per category and one row per supported setting.

It is an example. Copy it and adapt it to your game.

## Requirements

- **TextMeshPro Essential Resources:** if Unity asks for them when opening the scene, click **Import TMP Essentials**.
- **A Settings Profile:** **Create → Mirador Works → Settings → Default Profile**.
- **Localization** set up in the project (**Edit → Project Settings → Localization**), so labels show text instead of asset names.

## Try it

1. Open `BasicMenuDemo.unity`.
2. Select the **Demo** object and assign your profile to **Profile**.
3. Press Play.

Resolution, display mode and VSync only take effect in a build.

## What is inside

| File | What it does |
| --- | --- |
| `Scripts/BasicMenuDemo.cs` | Example entry point: creates the `SettingsService` and binds the menu. In your game, use your own bootstrap. |
| `Scripts/SettingsMenu.cs` | Builds the menu from the profile and wires Apply, Revert and Reset. |
| `Scripts/SettingRow.cs` | Base class of the rows: label and refresh on `ValueChanged`. |
| `Scripts/ToggleRow.cs`, `SliderRow.cs`, `DropdownRow.cs` | One row per base setting type. |
| `Scripts/CategoryHeader.cs` | Category title. |
| `Scripts/ConfirmationDialog.cs` | "Keep these changes?" dialog with a real-time countdown. |
| `Prefabs/` | The look of every piece. Edit them freely. |

## Your own setting types

Inherit from `SettingsMenu` and override `PrefabFor` to return your own row for your type. See the manual, section 9.5.
