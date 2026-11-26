# MiradorWorks Settings Wizard — Manual

> Versión del paquete: 0.1.0 · Unity 6000.0+ · URP 17 (opcional)

Este manual explica cómo instalar el paquete, cómo usarlo en un juego y cómo ampliarlo con ajustes propios. Si quieres saber **por qué** está hecho así, consulta el [documento de diseño](DESIGN.md).

## Índice

1. [Qué es](#1-qué-es)
2. [Instalación](#2-instalación)
3. [Inicio rápido](#3-inicio-rápido)
4. [Conceptos](#4-conceptos)
5. [Ajustes incluidos](#5-ajustes-incluidos)
6. [Usar el servicio](#6-usar-el-servicio)
7. [Hacer tu propia UI](#7-hacer-tu-propia-ui)
8. [Guardado](#8-guardado)
9. [Crear ajustes propios](#9-crear-ajustes-propios)
10. [Extender el Editor](#10-extender-el-editor)
11. [Probar tus ajustes](#11-probar-tus-ajustes)
12. [Problemas frecuentes](#12-problemas-frecuentes)
13. [Limitaciones](#13-limitaciones)

---

## 1. Qué es

Un sistema de ajustes de juego (pantalla y gráficos) que:

- **Solo incluye los ajustes que eliges.** Cada juego tiene un *perfil* con sus ajustes; lo que no está en el perfil no existe.
- **Se amplía heredando de una clase.** Un ajuste nuevo es una clase de pocas líneas, y la UI lo pinta sin cambios.
- **No impone UI.** El paquete da los datos y los eventos; la UI la haces tú. Trae un menú de ejemplo.
- **Guarda en un JSON** por defecto y se conecta a cualquier sistema de guardado.
- **Es localizable** con Unity Localization.

---

## 2. Instalación

### Requisitos

- Unity **6000.0** o superior.
- **Unity Localization** 1.5.x. Se instala solo como dependencia.
- **URP 17** para los ajustes gráficos. Es opcional: sin URP, el resto del paquete funciona y los ajustes de URP no aparecen.
- Plataforma: **PC** (Windows, macOS, Linux).

### Pasos

1. **Window → Package Manager → + → Install package from git URL…** y pega:

   ```
   https://github.com/Goblanch/mw-settings-wizard.git#v0.1.0
   ```

   El `#v0.1.0` fija la versión. Sin él, instalarías lo último de la rama por defecto, que puede cambiar en cualquier momento.

2. **Configura Localization**, si tu proyecto no lo tiene todavía: **Edit → Project Settings → Localization → Create**. Sin este paso, las etiquetas vacías se muestran con el nombre del asset.

3. (Opcional) Importa el sample: **Package Manager → MiradorWorks Settings Wizard → Samples → Basic Menu → Import**.

---

## 3. Inicio rápido

### 3.1 Crea el perfil

En el Project, clic derecho en la carpeta que quieras → **Create → Mirador Works → Settings → Default Profile**.

Se crea una carpeta **Game Settings** con:

- un asset por cada ajuste disponible, con su id y su modo ya configurados;
- las categorías **Display** y **Graphics**;
- los presets **Low**, **Medium** y **High**, y el selector de preset;
- el **Settings Profile** que lo agrupa todo.

Selecciona el perfil: abajo del inspector debería poner **"The profile is valid."**. Puedes borrar de la lista los ajustes que tu juego no necesite.

### 3.2 Crea el servicio

El servicio es el objeto que guarda los valores y los aplica. Créalo **una vez**, en el punto de arranque de tu juego:

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

- **`Initialize()`** carga lo guardado, corrige los valores que no sean válidos y aplica todos los ajustes.
- **`Dispose()`** libera lo que el servicio haya creado (por ejemplo, la copia del URP Asset).

El paquete **no tiene singletons**: tú decides cómo llega el servicio a quien lo necesita. Puede ser con una referencia serializada a `GameBootstrap`, pasándolo en un método `Init(...)` o con tu contenedor de dependencias.

### 3.3 Muestra un menú

La forma más rápida es usar el sample:

1. Abre la escena `BasicMenuDemo` del sample importado.
2. Selecciona el objeto **Demo** y arrastra tu perfil a **Profile**.
3. Dale a Play.

Para tu propia UI, sigue la [sección 7](#7-hacer-tu-propia-ui).

---

## 4. Conceptos

### Definición y valor

Cada ajuste tiene dos partes que el paquete mantiene separadas:

| | Dónde vive | Qué contiene |
| --- | --- | --- |
| **Definición** | Un asset (`ScriptableObject`) | Id, etiqueta, categoría, valor por defecto, límites, cómo se aplica. No cambia mientras se juega. |
| **Valor** | El `SettingsService` | Lo que ha elegido el jugador. |

Están separadas porque, en el Editor, lo que cambias en un ScriptableObject durante el Play Mode se queda escrito en el asset. Si el valor viviera en la definición, cada prueba cambiaría los valores por defecto.

### Tipos de ajuste

Hay tres tipos base, y cada uno corresponde a un control de UI:

| Tipo | Valor | Control |
| --- | --- | --- |
| `ToggleSetting` | `bool` | Toggle |
| `RangeSetting` | `float` (con mínimo, máximo y paso) | Slider |
| `ChoiceSetting` | `string` (la clave de la opción) | Desplegable |

Los `ChoiceSetting` guardan la **clave** de la opción (`"2560x1440@144"`), nunca su posición en la lista: la lista puede cambiar si el jugador cambia de monitor.

Para casos especiales puedes heredar directamente de `SettingDefinition<T>` con cualquier tipo (ver [9.5](#95-un-tipo-de-valor-propio)).

### El id

Cada ajuste tiene un **id** escrito a mano (`graphics.shadows`). Es la clave con la que se guarda.

- Formato: 1 a 64 caracteres entre letras ASCII, dígitos, `.`, `_` y `-`.
- **No lo cambies después de publicar el juego.** Si lo cambias, el valor guardado de los jugadores se pierde.
- Renombrar el asset no afecta, porque el id no se deriva del nombre.

### Perfil

El `SettingsProfile` es la lista de ajustes y presets de tu juego. El inspector muestra los errores en el momento: ids vacíos, repetidos o con caracteres no válidos, ajustes repetidos, presets mal configurados… Si el perfil tiene errores, `Initialize()` lanza una excepción con la lista completa.

### Modos de aplicación

- **Immediate:** el cambio se aplica en cuanto el jugador lo hace. Es lo adecuado para VSync, texturas o render scale.
- **Deferred:** el cambio queda **pendiente** hasta que se llama a `ApplyPending()` (el botón "Aplicar"). `RevertPending()` lo descarta. Es lo adecuado para la resolución o el modo de pantalla.

### Confirmación

Un ajuste con **Requires Confirmation** pide confirmación al aplicarse ("¿Mantener estos cambios?"). Si el jugador no confirma antes de que se acabe el tiempo (15 s por defecto), se vuelve al valor anterior.

- La cuenta atrás usa **tiempo real**: funciona aunque el juego esté pausado con `Time.timeScale = 0`.
- Mientras se espera la confirmación, el archivo guardado **conserva el valor anterior**. Si el juego se cuelga con una resolución que el monitor no admite, el siguiente arranque usa la última que funcionaba.

### Presets

Un `SettingsPreset` es un asset con valores para varios ajustes (Bajo, Medio, Alto…). El `PresetSetting` es un desplegable con los presets del perfil más la opción **custom**:

- Al elegir un preset, se aplican sus valores (respetando el modo de cada ajuste).
- El selector **se calcula solo**: muestra el primer preset que coincide con los valores actuales, o `custom` si no coincide ninguno. Si el jugador toca un ajuste del preset, pasa a `custom` automáticamente.

### Pasadas

Cada operación del servicio (`Set`, `ApplyPending`, un reset…) es una **pasada**. Todo lo que provoca se hace al terminar la pasada, en este orden:

1. Se aplican los lotes (por ejemplo, una sola llamada a `Screen.SetResolution` aunque hayan cambiado la resolución y el modo).
2. Se recalcula el selector de preset.
3. Se guarda, una sola vez.
4. Se lanzan los eventos.

Por eso, cuando tu UI recibe un evento, el cambio ya es real.

---

## 5. Ajustes incluidos

| Ajuste | Clase | Id sugerido | Tipo | Modo sugerido | Qué hace |
| --- | --- | --- | --- | --- | --- |
| Modo de pantalla | `DisplayModeSetting` | `display.mode` | Choice | Deferred + confirmación | Sin bordes, pantalla completa exclusiva (solo Windows), ventana maximizada (solo macOS) o ventana. |
| Resolución | `ResolutionSetting` | `display.resolution` | Choice | Deferred + confirmación | Las resoluciones del monitor, sin duplicados y de mayor a menor. Por defecto, la nativa. |
| VSync | `VSyncSetting` | `display.vsync` | Toggle | Immediate | `QualitySettings.vSyncCount`. |
| Límite de FPS | `FrameRateLimitSetting` | `display.fps` | Choice | Immediate | `Application.targetFrameRate`. Se ignora con el VSync activo. |
| Texturas | `TextureQualitySetting` | `graphics.textures` | Choice | Immediate | Resolución de las texturas: completa, media, baja o muy baja. |
| Render scale | `RenderScaleSetting` | `graphics.renderscale` | Range | Immediate | Resolución interna de render (0.5–2). |
| Sombras | `ShadowQualitySetting` | `graphics.shadows` | Choice | Immediate | Distancia, cascadas y resolución de los shadowmaps por nivel. |
| MSAA | `MsaaSetting` | `graphics.msaa` | Choice | Immediate | Antialiasing multisample: off, 2×, 4× u 8×. |
| Preset | `PresetSetting` | `graphics.preset` | Choice | — | Selector de presets. |

Los tres últimos de gráficos son de URP (`MiradorWorks.Settings.URP`).

### Opciones de cada ajuste

**Modo de pantalla.** Claves `borderless`, `exclusive`, `maximized` y `windowed`. Cada plataforma muestra solo las suyas. Si un jugador de Windows tiene guardado `exclusive` y abre el juego en Linux, se usa el valor por defecto.

**Resolución.**

- **Clave:** `WxH@Hz`, por ejemplo `2560x1440@144`. Los Hz se redondean: 143,998 Hz se guarda como 144.
- **Minimum Height** (720 por defecto): oculta las resoluciones más bajas. Si el monitor no tiene ninguna resolución de esa altura, se muestran todas.
- **Include Refresh Rate:** si lo desactivas, cada tamaño sale una sola vez, con su frecuencia más alta.
- **Si la resolución guardada ya no existe** (porque el jugador ha cambiado de monitor), se elige la más cercana.

**Límite de FPS.**

- **Rates** (30, 60, 120, 144 y 240 por defecto), más la opción `unlimited`, que es también el valor por defecto.
- **Con VSync activado, Unity ignora este límite.** Lo lógico es desactivar el control en tu UI mientras el VSync esté activo (ver [7.4](#74-ajustes-que-dependen-de-otros)).

**Texturas.** Claves `full`, `half`, `quarter` y `eighth`. Las texturas marcadas con *Ignore Mipmap Limit* en su importador no se ven afectadas, lo que es útil para la UI y los iconos.

**Sombras.**

- Los niveles se editan en el inspector. Por defecto son:

| Clave | Distancia | Cascadas | Luz principal | Luces adicionales |
| --- | --- | --- | --- | --- |
| `off` | 0 | 1 | 256 | 256 |
| `low` | 25 | 1 | 1024 | 512 |
| `medium` | 50 | 2 | 2048 | 1024 |
| `high` | 100 | 4 | 4096 | 2048 |

- Si no indicas una clave por defecto, se usa el último nivel.
- **Importante:** el URP Asset de tu proyecto debe tener activado *Lighting → Main Light → Cast Shadows*. Si está desactivado, la build no incluye los shaders de sombras y ningún nivel podrá encenderlas. El inspector del perfil te avisa.

**MSAA.** Claves `off`, `2x`, `4x` y `8x`.

### Los ajustes de URP no modifican tu URP Asset

Al primer uso, el servicio hace una **copia en memoria** del URP Asset activo y la pone como pipeline. Los ajustes modifican esa copia. Al hacer `Dispose()`, se restaura el asset original. El asset de tu proyecto nunca cambia, ni en el Editor ni en build.

---

## 6. Usar el servicio

### Leer y cambiar valores

```csharp
bool vsyncOn = settings.Get(vsync);          // what the player sees (pending value if there is one)
bool applied = settings.GetApplied(vsync);   // what is actually applied

settings.Set(vsync, true);
settings.Set(resolution, "2560x1440@144");
settings.Set(renderScale, 0.85f);
```

- **`Set` corrige el valor** si no es válido: lo limita al rango, lo ajusta al paso, o, si la opción no existe, usa la más cercana o el valor por defecto.
- **Si el valor no cambia, `Set` no hace nada:** no aplica, no guarda y no lanza eventos.
- **El ajuste debe estar en el perfil.** Si no, `Set` lanza `ArgumentException`.

### Cambios pendientes

```csharp
settings.Set(resolution, "1920x1080@60");   // Deferred: stays pending

bool any = settings.HasPendingChanges;       // true
bool resoultionPending = settings.HasPending(resolution); // true

settings.ApplyPending();   // apply every pending change (and save once)
settings.RevertPending();  // or discard them
```

### Restablecer valores

```csharp
settings.ResetToDefaults();          // everything
settings.ResetToDefaults(graphics);  // only one category
```

Cada ajuste respeta su modo: los diferidos quedan pendientes.

### Presets

```csharp
settings.ApplyPreset(highPreset);
SettingsPreset active = settings.ActivePreset;   // null = "custom"

// With the selector (same thing, through the dropdown):
settings.Set(presetSelector, "high");
```

### Eventos

```csharp
settings.ValueChanged += setting => { /* what Get returns changed: refresh that control */ };
settings.ValueApplied += setting => { /* the value became the applied one */ };
settings.ConfirmationRequested += request => confirmationDialog.Show(request);
```

- **`ValueChanged`** salta con cualquier cambio de lo que devuelve `Get`: un valor aplicado, un pendiente, una reversión o un reset.
- **`ValueApplied`** salta cuando el valor pasa a estar aplicado. No se lanza durante `Initialize()`.
- **Si un handler lanza una excepción,** se registra en la consola y los demás handlers se siguen ejecutando.

### Ajustes no soportados

```csharp
if (!settings.IsSupported(shadows))
    shadowsRow.SetActive(false);
```

Un ajuste no está soportado cuando no tiene sentido en la configuración actual. Por ejemplo, los de URP en un proyecto sin URP. Su valor se sigue guardando, pero no se aplica.

### Tiempo de confirmación

```csharp
settings.ConfirmationTimeoutSeconds = 10f;   // default: 15
```

---

## 7. Hacer tu propia UI

### 7.1 La idea

Tu UI solo tiene que saber pintar **tres controles**: toggle, slider y desplegable. Recorre el perfil y elige el control según el tipo base del ajuste:

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

Así, un ajuste nuevo que herede de uno de los tres tipos aparece en tu menú sin cambiar la UI.

### 7.2 Los tres controles

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

**Desplegable:**

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

- **Usa `...WithoutNotify`** cuando muestres un valor que viene del servicio. Si no, el control dispararía su propio evento, volvería a llamar a `Set` y entrarías en un bucle.
- **Las etiquetas de las opciones** pueden ser localizadas (`LocalizedLabel`, en las opciones fijas como "Sin bordes") o texto plano (`RawLabel`, en las generadas como "2560 × 1440 @ 144 Hz").
- **La etiqueta del ajuste** está en `setting.Label`, y la de su categoría en `setting.Category.Label`.

### 7.3 Mantener la UI al día

```csharp
settings.ValueChanged += changed =>
{
    if (changed == setting)
        toggle.SetIsOnWithoutNotify(settings.Get(setting));

    applyButton.interactable = settings.HasPendingChanges;
};
```

Es necesario porque hay cambios que no vienen de ese control: un preset, un reset, una confirmación que se revierte o un ajuste que cambia el selector de preset.

### 7.4 Ajustes que dependen de otros

Unity ignora el límite de FPS mientras el VSync está activo, así que conviene desactivar ese control:

```csharp
void RefreshFrameRateRow() =>
    frameRateDropdown.interactable = !settings.Get(vsync);

settings.ValueChanged += changed =>
{
    if (changed == vsync)
        RefreshFrameRateRow();
};
```

### 7.5 Diálogo de confirmación

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

`settings.ActiveConfirmation` devuelve la petición en curso, o `null` si no hay ninguna.

### 7.6 El sample Basic Menu

Es un menú completo con UGUI y TextMeshPro, y la forma más rápida de partir:

| Pieza | Qué hace |
| --- | --- |
| `SettingsMenu` | Recorre el perfil, crea una cabecera por categoría y una fila por ajuste soportado. Conecta Aplicar, Revertir y Restablecer. |
| `SettingRow` | Base de las filas: pone la etiqueta (localizada) y se refresca con `ValueChanged`. |
| `ToggleRow`, `SliderRow`, `DropdownRow` | Una fila por tipo base. |
| `CategoryHeader` | Título de categoría. |
| `ConfirmationDialog` | "¿Mantener estos cambios?" con cuenta atrás. |
| `BasicMenuDemo` | Ejemplo de punto de arranque: crea el servicio y se lo da al menú. |

- **Para cambiar el aspecto,** edita los prefabs.
- **Para tus propios tipos de ajuste,** hereda de `SettingsMenu` y sobrescribe `PrefabFor` (ver [9.5](#95-un-tipo-de-valor-propio)).
- **Es un ejemplo:** cópialo y adáptalo a tu juego, no lo uses tal cual.

---

## 8. Guardado

### 8.1 Por defecto: un archivo JSON

`JsonFileSettingsStorage` guarda en `Application.persistentDataPath/settings.json`. En Windows eso es `%USERPROFILE%\AppData\LocalLow\<Empresa>\<Producto>\settings.json`:

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

- **Escritura atómica:** escribe primero en `settings.json.tmp` y después sustituye el archivo. Un cierre a mitad de guardado no deja el archivo corrupto.
- **Archivos corruptos:** si no se puede leer, se hace una copia en `settings.json.bak` y se usan los valores por defecto.
- **Ruta propia:** `new JsonFileSettingsStorage("ruta/completa/al/archivo.json")`.
- **Ids que ya no están en el perfil:** se conservan al guardar. Si quitas un ajuste y vuelves a ponerlo, su valor sigue ahí.

### 8.2 Conectar tu sistema de guardado

Implementa `ISettingsStorage`:

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

**El contrato:**

- `TryLoad` **nunca** lanza: si algo falla, devuelve `false`.
- `Save` **sí** puede lanzar. El servicio lo registra y el valor se queda aplicado durante la sesión.
- **Los ajustes son globales.** Guárdalos en una entrada común, no dentro de cada partida.

Si tu sistema no guarda texto, puedes recorrer `data.Entries` (cada una con `Id` y `Value`) y reconstruir los datos con `new SettingsData()` y `data.Set(id, value)`.

### 8.3 En memoria

`InMemorySettingsStorage` no escribe a disco. Sirve para tests y prototipos (ver [11](#11-probar-tus-ajustes)).

---

## 9. Crear ajustes propios

En todos los casos, los pasos son tres:

1. Crea la clase, heredando del tipo base adecuado.
2. Crea el asset con el menú de tu `CreateAssetMenu` y ponle **id**, etiqueta, categoría y modo.
3. Añádelo al perfil.

El servicio lo carga, lo guarda y lo aplica, y tu UI (o el sample) lo pinta sin cambios.

### 9.1 Un toggle

Un ajuste de gameplay que el juego lee cuando lo necesita. No toca el motor, así que `Apply` queda vacío:

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

El valor por defecto se pone en el campo **Default Value** del inspector del asset.

### 9.2 Un slider con su propio rango

Volumen de un canal del Audio Mixer. El audio no está incluido en el paquete, pero es un buen ejemplo de ampliación:

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

- **El constructor con `base(...)`** fija el rango y el valor por defecto de los assets nuevos. Si después cambias los valores en el inspector, mandan los del inspector.
- **`AudioMixer.SetFloat` no tiene efecto si se llama en el `Awake` de la primera escena.** Si usas este ajuste, inicializa el servicio en `Start`.

### 9.3 Un desplegable con opciones fijas

Filtrado anisótropo con tres opciones localizables:

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

- **Las claves no se pueden cambiar después de publicar,** porque son lo que se guarda. Las etiquetas sí.
- **El valor por defecto** es el **Default Key** del inspector o, si está vacío, la primera opción.

### 9.4 Un desplegable con opciones generadas

Cuando las opciones dependen del sistema (monitores, idiomas instalados…):

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

- **`ChoiceOption.Text`** sirve para etiquetas generadas que no hace falta traducir.
- **`FindClosest`** es opcional. Si no lo sobrescribes, una clave que ya no existe pasa al valor por defecto.
- **`GetOptions` se llama a menudo.** Si construir las opciones es caro, guárdalas en caché.

`ResolutionSetting` es el ejemplo completo de este caso.

### 9.5 Un tipo de valor propio

Si ninguno de los tres tipos encaja, hereda de `SettingDefinition<T>`. Por ejemplo, el color de la mira:

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

Los métodos que tienes que implementar:

| Método | Para qué | Regla |
| --- | --- | --- |
| `GetDefault` | Valor inicial. | Puede depender del runtime. |
| `Sanitize` | Convertir cualquier valor en uno válido. | No lanza excepciones. |
| `Apply` | Aplicarlo al motor o al juego. | Puede quedar vacío. |
| `Serialize` / `TryDeserialize` | Pasarlo a texto y leerlo. | `TryDeserialize` no lanza excepciones. Usa `CultureInfo.InvariantCulture` con números. |
| `AreEqual` (opcional) | Comparar dos valores. | Por defecto usa `Equals`. |

**Tu UI no sabe pintar este tipo,** así que necesita una fila propia. Con el sample:

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

### 9.6 Ajustes que dependen de otros

Desde `GetDefault`, `Sanitize`, `IsSupported` o `Apply` puedes leer el valor de otros ajustes:

```csharp
[SerializeField] VSyncSetting vsync;

public override void Apply(string value, SettingsApplyContext context)
{
    if (context.Settings.TryGetValue(vsync, out bool vsyncOn) && vsyncOn)
        return; // nothing to do while VSync is on
    // ...
}
```

- `TryGetValue` (el valor que ve el jugador) y `TryGetApplied` (el valor aplicado).
- También por id: `context.Settings.TryGetValue<bool>("display.vsync", out bool on)`. Es mejor usar la referencia al asset, porque sigue funcionando aunque se cambie el id.
- **Durante `Initialize()`, los ajustes se cargan en el orden del perfil.** Si el otro ajuste va después, `TryGetValue` devuelve `false`. Si lo necesitas, pon primero en el perfil el ajuste del que dependes.

### 9.7 Lotes: varios ajustes, una sola llamada

Si varios ajustes acaban en **la misma llamada al motor**, no la hagas en cada `Apply`. Usa un lote: cada ajuste apunta su parte, y la llamada se hace una vez al final de la pasada.

```csharp
public sealed class BloomBatch : IApplyBatch
{
    public bool? Enabled { get; set; }
    public float? Intensity { get; set; }

    public void Commit(SettingsContext context)
    {
        // One call with both values. What no setting provided in this pass keeps its current value.
    }
}

// In BloomSetting.Apply:
context.GetBatch<BloomBatch>().Enabled = value;

// In BloomIntensitySetting.Apply:
context.GetBatch<BloomBatch>().Intensity = value;
```

- **Todos los ajustes de la misma pasada reciben el mismo lote,** porque se busca por tipo.
- **El lote necesita un constructor sin parámetros.** El servicio lo crea la primera vez que alguien lo pide.
- **No guardes el `SettingsApplyContext`** para después de `Apply`: al terminar la pasada deja de servir.

`DisplayBatch` (resolución y modo de pantalla) es el ejemplo del paquete.

### 9.8 Objetos compartidos

Si varios ajustes necesitan **el mismo objeto** (una copia de un asset, un handle a un sistema…), pídelo al contexto. No uses un singleton:

```csharp
RuntimeVolumeHandle handle = context.Settings.GetOrAddService(() => new RuntimeVolumeHandle());
```

- **Se crea la primera vez** que alguien lo pide, y los demás reciben el mismo objeto.
- **Vive lo mismo que el servicio.** Si implementa `IDisposable`, se destruye con él.

`UrpAssetHandle.Get(context.Settings)` es el ejemplo del paquete.

### 9.9 Dónde poner el código

- **Tus ajustes van en un assembly de runtime,** que referencie `MiradorWorks.Settings` (y `MiradorWorks.Settings.URP` si usas tipos de URP).
- **Lo de la [sección 10](#10-extender-el-editor) va en un assembly solo de Editor,** que referencie también `MiradorWorks.Settings.Editor`.

---

## 10. Extender el Editor

### 10.1 Añadir tus ajustes al perfil por defecto

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

- **Se encuentra automáticamente:** no hay que registrarlo en ningún sitio.
- **`configure`** recibe el `SerializedObject` del asset recién creado, para rellenar sus otros campos.
- **`Category`** devuelve la categoría que ya exista con ese id, o la crea.

### 10.2 Avisos propios en el inspector del perfil

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

Los avisos salen en amarillo, debajo de los errores de validación.

### 10.3 Editar valores de presets

El inspector de un preset muestra el control adecuado para cada valor: toggle, slider o desplegable. Con un tipo propio (9.5) muestra el texto tal cual se guarda; en el ejemplo del color, `#RRGGBBAA`.

---

## 11. Probar tus ajustes

Monta el perfil en código con un storage en memoria:

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

- **`JsonUtility.FromJsonOverwrite`** escribe campos privados serializados (como el id), también fuera del Editor.
- **`InMemorySettingsStorage.SaveCount`** te dice cuántas veces se ha guardado.
- **Los tests que cambian el motor** (`QualitySettings`, `Screen`, URP) van en **PlayMode**, y deben restaurar los valores en el `TearDown`. Unity restaura los Quality Settings al salir de Play Mode, pero en EditMode los cambios se quedarían escritos en el proyecto.
- **En los tests de EditMode, destruye con `DestroyImmediate`,** no con `Destroy`.

---

## 12. Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| El ajuste de sombras no enciende las sombras en la build. | El URP Asset tiene las sombras desactivadas y la build quitó esos shaders. | Activa *Lighting → Main Light → Cast Shadows* en el URP Asset. El inspector del perfil lo avisa. |
| La resolución o el modo de pantalla no cambian en el Editor. | La Game view no es una ventana real. | Pruébalo en una build. |
| El VSync no limita los FPS en el Editor. | Unity solo usa el VSync en el player. | Pruébalo en una build. |
| El límite de FPS no hace nada. | El VSync está activado. | Desactívalo (o desactiva el control en tu UI). |
| Excepción al iniciar: *"Settings profile … is invalid"*. | El perfil tiene errores. | Mira el inspector del perfil: muestra cada error. |
| *"'X' is not part of settings profile"*. | Ese ajuste no está en el perfil del servicio. | Añádelo al perfil o usa el asset que sí está. |
| Las etiquetas muestran el nombre del asset. | El `LocalizedString` está vacío o Localization no está configurado. | Configura Localization y rellena las etiquetas. |
| Los ajustes no se guardan. | Error al escribir (permisos, disco). | Busca *"Could not save settings"* en la consola. |
| Los gráficos de URP vuelven a sus valores tras cambiar de nivel de calidad. | `QualitySettings.SetQualityLevel` cambia el pipeline y deja de usarse la copia. | No cambies de nivel de calidad con el sistema activo (ver [13](#13-limitaciones)). |
| *"Saved value … is invalid. Using the default."* | El archivo guardado tiene un valor que no se puede leer. | Es un aviso: se usa el valor por defecto y el juego sigue. |

---

## 13. Limitaciones

Lo que no hace la v0.1.0:

- **Audio y controles.** El audio se puede añadir con un ajuste propio (ver 9.2).
- **FXAA/SMAA, HDR, upscaling, sombras suaves y post-procesado.**
- **Consolas, móvil y HDRP.** Los ajustes de pantalla se marcan como no soportados fuera de PC.
- **Cambiar de nivel de calidad** mientras el sistema está activo: la copia del URP Asset deja de usarse.
- **Cambio de monitor en caliente:** las opciones de resolución se calculan al pedirlas, pero no hay un evento que avise del cambio.
- **Inicialización automática:** el servicio se crea siempre desde tu código.
