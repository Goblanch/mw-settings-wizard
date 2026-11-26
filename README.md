# MiradorWorks Settings Wizard

> 🇪🇸 Español · [🇬🇧 English](#english)

Sistema de ajustes de juego para Unity: eliges qué ajustes tiene tu juego, añades los tuyos heredando de una clase y tu UI los pinta sin cambios.

## Características

- **Solo los ajustes que necesitas:** cada juego elige los suyos en un `SettingsProfile`.
- **Ampliable:** un ajuste nuevo es una clase que hereda de `ToggleSetting`, `RangeSetting` o `ChoiceSetting`, sin tocar el paquete ni la UI.
- **Sin UI impuesta:** el paquete da datos y eventos. Incluye un menú de ejemplo.
- **Aplicar y revertir:** ajustes inmediatos o diferidos, con confirmación y reversión automática.
- **Presets** (Bajo, Medio, Alto…) con detección automática de "Personalizado".
- **Guardado intercambiable:** JSON por defecto, o tu sistema de guardado con `ISettingsStorage`.
- **Localizable** con Unity Localization.
- **URP sin tocar tus assets:** los ajustes gráficos modifican una copia en runtime del URP Asset.
- **Sin singletons ni estado estático.**

## Ajustes incluidos

| Pantalla | Gráficos |
| --- | --- |
| Modo de pantalla | Calidad de texturas |
| Resolución | Render scale (URP) |
| VSync | Calidad de sombras (URP) |
| Límite de FPS | Antialiasing MSAA (URP) |
| | Preset de calidad |

## Requisitos

- Unity `6000.0` o superior.
- `com.unity.localization` `1.5.x` (se instala solo).
- URP `17.x` para los ajustes de URP (opcional).
- PC: Windows, macOS y Linux.

## Instalación

**Window → Package Manager → + → Install package from git URL…**

```text
https://github.com/Goblanch/mw-settings-wizard.git#v0.1.0
```

Si tu proyecto no tiene Localization configurado: **Edit → Project Settings → Localization → Create**.

## Uso rápido

1. **Crea el perfil:** clic derecho en el Project → **Create → Mirador Works → Settings → Default Profile**. Se crea la carpeta `Game Settings` con todos los ajustes, categorías y presets.
2. **Crea el servicio** en el arranque del juego:

   ```csharp
   using MiradorWorks.Settings;
   using UnityEngine;

   public sealed class GameBootstrap : MonoBehaviour
   {
       [SerializeField] SettingsProfile settingsProfile;

       public SettingsService Settings { get; private set; }

       void Awake()
       {
           Settings = new SettingsService(settingsProfile, new JsonFileSettingsStorage());
           Settings.Initialize();
       }

       void OnDestroy() => Settings?.Dispose();
   }
   ```

3. **Conecta tu UI** con `Get`, `Set` y los eventos `ValueChanged` y `ConfirmationRequested`, o parte del sample **Basic Menu** (Package Manager → Samples).

## Un ajuste propio

```csharp
using MiradorWorks.Settings;
using UnityEngine;

[CreateAssetMenu(menuName = "My Game/Settings/Camera Shake")]
public sealed class CameraShakeSetting : ToggleSetting
{
    public override void Apply(bool value, SettingsApplyContext context) { }
}
```

Crea el asset, ponle un id (`gameplay.camerashake`) y añádelo al perfil. El manual tiene ejemplos de sliders, desplegables, tipos propios, lotes y extensiones del Editor.

## Documentación

- [Manual de uso](Documentation~/Manual.es.md): instalación, uso, UI, guardado y cómo ampliarlo.
- [Documento de diseño](Documentation~/DESIGN.md): decisiones y especificación.
- [Changelog](CHANGELOG.md)

## Licencia

Ver [LICENSE](LICENSE).

---

<a id="english"></a>

# MiradorWorks Settings Wizard

> [🇪🇸 Español](#miradorworks-settings-wizard) · 🇬🇧 English

Game settings system for Unity: pick the settings your game needs, add your own by inheriting from a class, and your UI renders them without changes.

## Features

- **Only the settings you need:** each game picks its own in a `SettingsProfile`.
- **Extensible:** a new setting is a class inheriting from `ToggleSetting`, `RangeSetting` or `ChoiceSetting`, with no changes to the package or the UI.
- **No UI imposed:** the package provides data and events. A sample menu is included.
- **Apply and revert:** immediate or deferred settings, with confirmation and automatic revert.
- **Presets** (Low, Medium, High…) with automatic "Custom" detection.
- **Pluggable saving:** JSON by default, or your own save system through `ISettingsStorage`.
- **Localizable** with Unity Localization.
- **URP without touching your assets:** graphics settings modify a runtime copy of the URP Asset.
- **No singletons or static state.**

## Included settings

| Display | Graphics |
| --- | --- |
| Display mode | Texture quality |
| Resolution | Render scale (URP) |
| VSync | Shadow quality (URP) |
| Frame rate limit | MSAA anti-aliasing (URP) |
| | Quality preset |

## Requirements

- Unity `6000.0` or newer.
- `com.unity.localization` `1.5.x` (installed automatically).
- URP `17.x` for the URP settings (optional).
- PC: Windows, macOS and Linux.

## Installation

**Window → Package Manager → + → Install package from git URL…**

```text
https://github.com/Goblanch/mw-settings-wizard.git#v0.1.0
```

If your project does not have Localization set up yet: **Edit → Project Settings → Localization → Create**.

## Quick start

1. **Create the profile:** right-click in the Project window → **Create → Mirador Works → Settings → Default Profile**. It creates a `Game Settings` folder with every setting, category and preset.
2. **Create the service** at your game's entry point:

   ```csharp
   using MiradorWorks.Settings;
   using UnityEngine;

   public sealed class GameBootstrap : MonoBehaviour
   {
       [SerializeField] SettingsProfile settingsProfile;

       public SettingsService Settings { get; private set; }

       void Awake()
       {
           Settings = new SettingsService(settingsProfile, new JsonFileSettingsStorage());
           Settings.Initialize();
       }

       void OnDestroy() => Settings?.Dispose();
   }
   ```

3. **Hook up your UI** with `Get`, `Set` and the `ValueChanged` and `ConfirmationRequested` events, or start from the **Basic Menu** sample (Package Manager → Samples).

## Your own setting

```csharp
using MiradorWorks.Settings;
using UnityEngine;

[CreateAssetMenu(menuName = "My Game/Settings/Camera Shake")]
public sealed class CameraShakeSetting : ToggleSetting
{
    public override void Apply(bool value, SettingsApplyContext context) { }
}
```

Create the asset, give it an id (`gameplay.camerashake`) and add it to the profile. The manual has examples of sliders, dropdowns, custom types, batches and Editor extensions.

## Documentation

- [User manual](Documentation~/Manual.md): installation, usage, UI, saving and extending it.
- [Design document](Documentation~/DESIGN.md) (Spanish): decisions and specification.
- [Changelog](CHANGELOG.md)

## License

See [LICENSE](LICENSE).
