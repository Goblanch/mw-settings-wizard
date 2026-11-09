# MiradorWorks Settings Wizard

> 🇪🇸 Español · [🇬🇧 English](#english)

Sistema de ajustes de juego para Unity: eliges qué ajustes tiene tu juego, añades los tuyos heredando de una clase y tu UI los pinta sin cambios.

> ⚠️ **En desarrollo.** Todavía no hay versión publicada. La API puede cambiar hasta `v0.1.0`.

## Características

- **Solo los ajustes que necesitas:** cada juego elige sus ajustes en un `SettingsProfile`. Si solo quieres resolución, texturas y sombras, solo aparecen esos tres.
- **Ampliable:** un ajuste nuevo es una clase que hereda de `ToggleSetting`, `RangeSetting` o `ChoiceSetting`. No hace falta tocar el paquete ni la UI.
- **Sin UI impuesta:** el paquete expone datos y eventos; la UI la hace cada juego. Incluye un sample de ejemplo.
- **Aplicar y revertir:** ajustes inmediatos o diferidos, con confirmación y reversión automática para la resolución.
- **Presets:** Bajo, Medio, Alto, Ultra… y "Personalizado" cuando el jugador cambia algo.
- **Persistencia intercambiable:** guarda en JSON por defecto y se conecta a cualquier sistema de guardado implementando `ISettingsStorage`.
- **Localizable:** etiquetas con Unity Localization.
- **URP sin tocar tus assets:** los ajustes gráficos modifican una copia en runtime del URP Asset; el original del proyecto no cambia.
- **Sin singletons ni estado estático:** cada proyecto decide quién crea el servicio.

## Ajustes incluidos

| Pantalla | Gráficos (URP) |
| --- | --- |
| Modo de pantalla | Render scale |
| Resolución | Calidad de sombras |
| VSync | Antialiasing (MSAA) |
| Límite de FPS | Preset de calidad |
| Calidad de texturas | |

## Requisitos

- Unity `6000.0` o superior.
- `com.unity.localization` `1.5.x` (se instala automáticamente como dependencia).
- Universal Render Pipeline `17.x` para los ajustes gráficos (opcional: sin URP, el resto del paquete funciona).
- Plataforma: PC (Windows, macOS, Linux).

## Instalación

En Unity: **Window → Package Manager → + → Install package from git URL…**

```text
https://github.com/goblanch/mw-settings-wizard.git#v0.1.0
```

O añade esta línea en `Packages/manifest.json`:

```json
"com.miradorworks.settings": "https://github.com/goblanch/mw-settings-wizard.git#v0.1.0"
```

> Localization necesita configuración inicial: **Edit → Project Settings → Localization → Create**.

## Uso rápido

**1. Crea un perfil** con los ajustes que quieras: **Assets → Create → Mirador Works → Settings → Default Profile**.

**2. Crea el servicio** en el punto de arranque de tu juego:

```csharp
using MiradorWorks.Settings;
using UnityEngine;
 
public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] SettingsProfile settingsProfile;
 
    SettingsService settings;
 
    void Awake()
    {
        settings = new SettingsService(settingsProfile, new JsonFileSettingsStorage());
        settings.Initialize();
    }
 
    void OnDestroy() => settings?.Dispose();
}
```

**3. Conecta tu UI** con `Get`, `Set` y los eventos `ValueChanged` y `ConfirmationRequested`. El sample **Basic Menu** (Package Manager → Samples) es un ejemplo completo.

## Añadir un ajuste propio

```csharp
using MiradorWorks.Settings;
using UnityEngine;
 
[CreateAssetMenu(menuName = "Mirador Works/Settings/Motion Blur")]
public sealed class MotionBlurSetting : ToggleSetting
{
    [SerializeField] bool defaultValue = true;
 
    public override bool GetDefault(SettingsContext context) => defaultValue;
 
    public override void Apply(bool value, SettingsApplyContext context)
    {
        // Enable or disable motion blur in your game.
    }
}
```

Crea el asset, ponle un `id` único y añádelo al perfil.

## Conectar tu sistema de guardado

```csharp
public sealed class MySaveSystemStorage : ISettingsStorage
{
    public bool TryLoad(out SettingsData data) { /* read from your save system */ }
    public void Save(SettingsData data) { /* write to your save system */ }
}
```

Pásalo al crear el servicio en lugar de `JsonFileSettingsStorage`.

## Documentación

- [Documento de diseño](Documentation~/DESIGN.md)
- [Changelog](CHANGELOG.md)

## Licencia

Ver [LICENSE.md](LICENSE.md).

---

<a id="english"></a>

# MiradorWorks Settings Wizard

> [🇪🇸 Español](#miradorworks-settings-wizard) · 🇬🇧 English

Game settings system for Unity: pick the settings your game needs, add your own by inheriting from a class, and your UI renders them without changes.

> ⚠️ **Work in progress.** No version has been released yet. The API may change until `v0.1.0`.

## Features

- **Only the settings you need:** each game picks its settings in a `SettingsProfile`. If you only want resolution, textures and shadows, only those three show up.
- **Extensible:** a new setting is a class inheriting from `ToggleSetting`, `RangeSetting` or `ChoiceSetting`. No changes to the package or the UI.
- **No UI imposed:** the package exposes data and events; each game builds its own UI. A sample is included.
- **Apply and revert:** immediate or deferred settings, with confirmation and automatic revert for resolution changes.
- **Presets:** Low, Medium, High, Ultra… and "Custom" when the player changes something.
- **Pluggable persistence:** saves to JSON by default and connects to any save system by implementing `ISettingsStorage`.
- **Localizable:** labels use Unity Localization.
- **URP without touching your assets:** graphics settings modify a runtime copy of the URP Asset; the project's original stays untouched.
- **No singletons or static state:** each project decides who creates the service.

## Included settings

| Display | Graphics (URP) |
| --- | --- |
| Display mode | Render scale |
| Resolution | Shadow quality |
| VSync | Anti-aliasing (MSAA) |
| Frame rate limit | Quality preset |
| Texture quality | |

## Requirements

- Unity `6000.0` or newer.
- `com.unity.localization` `1.5.x` (installed automatically as a dependency).
- Universal Render Pipeline `17.x` for graphics settings (optional: the rest of the package works without URP).
- Platform: PC (Windows, macOS, Linux).

## Installation

In Unity: **Window → Package Manager → + → Install package from git URL…**

```text
https://github.com/goblanch/mw-settings-wizard.git#v0.1.0
```

Or add this line to `Packages/manifest.json`:

```json
"com.miradorworks.settings": "https://github.com/goblanch/mw-settings-wizard.git#v0.1.0"
```

> Localization needs an initial setup: **Edit → Project Settings → Localization → Create**.

## Quick start

**1. Create a profile** with the settings you want: **Assets → Create → Mirador Works → Settings → Default Profile**.

**2. Create the service** at your game's entry point:

```csharp
using MiradorWorks.Settings;
using UnityEngine;
 
public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] SettingsProfile settingsProfile;
 
    SettingsService settings;
 
    void Awake()
    {
        settings = new SettingsService(settingsProfile, new JsonFileSettingsStorage());
        settings.Initialize();
    }
 
    void OnDestroy() => settings?.Dispose();
}
```

**3. Hook up your UI** with `Get`, `Set` and the `ValueChanged` and `ConfirmationRequested` events. The **Basic Menu** sample (Package Manager → Samples) is a complete example.

## Adding your own setting

```csharp
using MiradorWorks.Settings;
using UnityEngine;
 
[CreateAssetMenu(menuName = "Mirador Works/Settings/Motion Blur")]
public sealed class MotionBlurSetting : ToggleSetting
{
    [SerializeField] bool defaultValue = true;
 
    public override bool GetDefault(SettingsContext context) => defaultValue;
 
    public override void Apply(bool value, SettingsApplyContext context)
    {
        // Enable or disable motion blur in your game.
    }
}
```

Create the asset, give it a unique `id` and add it to the profile.

## Connecting your save system

```csharp
public sealed class MySaveSystemStorage : ISettingsStorage
{
    public bool TryLoad(out SettingsData data) { /* read from your save system */ }
    public void Save(SettingsData data) { /* write to your save system */ }
}
```

Pass it to the service instead of `JsonFileSettingsStorage`.

## Documentation

- [Design document](Documentation~/DESIGN.md) (Spanish)
- [Changelog](CHANGELOG.md)

## License

See [LICENSE.md](LICENSE.md).
