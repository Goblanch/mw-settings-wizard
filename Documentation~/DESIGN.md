# MiradorWorks Settings — Documento de diseño

> Versión del documento: 0.1 (9 nov 2026)
> Estado: diseño cerrado para v0.1.0
> Repositorio: `mw-settings-wizard` · Package id: `com.miradorworks.settings`

## Índice

- [MiradorWorks Settings — Documento de diseño](#miradorworks-settings--documento-de-diseño)
  - [Índice](#índice)
  - [1. Contexto y objetivos](#1-contexto-y-objetivos)
    - [Objetivos](#objetivos)
    - [Alcance de v0.1.0](#alcance-de-v010)
  - [2. Decisiones](#2-decisiones)
  - [3. Arquitectura](#3-arquitectura)
    - [3.1 Visión general](#31-visión-general)
    - [3.2 Assemblies](#32-assemblies)
    - [3.3 Estructura del repo](#33-estructura-del-repo)
  - [4. Especificación](#4-especificación)
    - [4.1 Definiciones](#41-definiciones)
    - [4.2 Tipos base para la UI](#42-tipos-base-para-la-ui)
    - [4.3 Perfil y categorías](#43-perfil-y-categorías)
    - [4.4 Servicio](#44-servicio)
    - [4.5 Confirmación](#45-confirmación)
    - [4.6 Aplicación y lotes](#46-aplicación-y-lotes)
    - [4.7 Presets](#47-presets)
    - [4.8 Persistencia](#48-persistencia)
    - [4.9 URP](#49-urp)
  - [5. Ajustes incluidos en v0.1.0](#5-ajustes-incluidos-en-v010)
  - [6. Cómo añadir un ajuste nuevo](#6-cómo-añadir-un-ajuste-nuevo)
  - [7. Tests](#7-tests)
    - [EditMode](#editmode)
    - [PlayMode](#playmode)
  - [8. Riesgos](#8-riesgos)
  - [9. Hoja de ruta de v0.1.0](#9-hoja-de-ruta-de-v010)
  - [10. Fuera de alcance y futuro](#10-fuera-de-alcance-y-futuro)
  - [11. Glosario](#11-glosario)
  - [12. Historial](#12-historial)

---

## 1. Contexto y objetivos

El equipo necesita un sistema de ajustes de juego reutilizable entre proyectos. Es la segunda pieza de la suite Mirador Works, después de `com.miradorworks.pooling`.

### Objetivos

- **Elegir solo los ajustes que se necesitan.** Si un juego quiere resolución, texturas y sombras, solo aparecen esos tres.
- **Ampliable.** Añadir un ajuste nuevo es heredar de una clase base, sin tocar el core ni la UI.
- **Sin UI propia.** El paquete expone los datos y los eventos; la UI la hace cada juego. Se incluye un sample de prueba.
- **Funciona sin el sistema de guardado del equipo** y se puede conectar a él con un adaptador.
- **Localizable** desde el primer día.

### Alcance de v0.1.0

- Plataforma: **PC (Windows, macOS, Linux)**.
- Render pipeline: **URP** (17.x, Unity 6.6).
- Categoría: **gráficos y pantalla**. Audio y controles quedan fuera (ver [10](#10-fuera-de-alcance-y-futuro)).

---

## 2. Decisiones

| Id | Decisión | Motivo |
| --- | --- | --- |
| D1 | Un repo por paquete UPM. Repo `mw-settings-wizard`, package id `com.miradorworks.settings`, namespace `MiradorWorks.Settings`. | Mismo criterio que el pooler. Cada paquete se versiona y se instala por separado. Unity solo usa el `name` del `package.json`, así que el nombre del repo no tiene por qué coincidir con el id. |
| D2 | La raíz del repo es la raíz del paquete. No hay proyecto Unity dentro del repo; se prueba desde un proyecto sandbox aparte que lo referencia con `file:`. | Instalación directa por URL de Git. El sandbox no contamina el paquete. |
| D3 | Cada ajuste es un ScriptableObject que hereda de `SettingDefinition<T>`. La definición **describe** el ajuste (id, etiqueta, valor por defecto, cómo se aplica) pero **no guarda el valor actual**. | Los cambios a un ScriptableObject en Play Mode se quedan escritos en el asset dentro del Editor. Si el valor viviera ahí, cada partida de prueba cambiaría los defaults. |
| D4 | El valor actual vive en `SettingsService`, una clase C# normal (no MonoBehaviour). | Se puede testear sin escena, y su ciclo de vida lo decide quien la crea. |
| D5 | Tres tipos base forman el contrato con la UI: `ToggleSetting` (bool), `RangeSetting` (float) y `ChoiceSetting` (string, la clave de la opción). | La UI solo necesita saber pintar tres controles. Un ajuste nuevo que herede de uno de los tres aparece en cualquier UI sin cambios. |
| D6 | Los `ChoiceSetting` guardan la **clave** de la opción (`"1920x1080@144"`), nunca su índice. | La lista de opciones puede cambiar entre ejecuciones (otro monitor, otra versión del juego). Un índice guardado acabaría apuntando a otra opción. |
| D7 | Cada ajuste tiene un `id` escrito a mano (`"graphics.shadows"`) y es la clave de persistencia. No se deriva del nombre del asset. Los ids duplicados en un perfil son un error. | Renombrar un asset no rompe los guardados. |
| D8 | Los ajustes que se incluyen en un juego se eligen en un `SettingsProfile` (ScriptableObject). | Responde al objetivo principal: el perfil es la lista de lo que aparece. |
| D9 | Cada ajuste define su modo de aplicación: `Immediate` (se aplica al cambiarlo) o `Deferred` (queda pendiente hasta `ApplyPending()`). `RevertPending()` descarta los pendientes. | Hay ajustes que se quieren ver al instante (render scale) y otros que es mejor agrupar (resolución). |
| D10 | Los ajustes con `RequiresConfirmation` lanzan una petición de confirmación con tiempo límite. Si no se confirma, se revierte solo. El temporizador usa **tiempo no escalado**. | Es el patrón estándar para la resolución: si el monitor se queda en negro, el jugador no puede pulsar nada. Con tiempo escalado, un menú con `timeScale = 0` no revertiría nunca. |
| D11 | Los ajustes que comparten una llamada al motor (resolución y modo de pantalla usan `Screen.SetResolution`) escriben en un **lote** (`IApplyBatch`), y el servicio lo confirma una sola vez al final. | Evita aplicar dos veces seguidas, con el parpadeo que eso provoca, y que el orden de aplicación importe. |
| D12 | Los presets son ScriptableObjects (`SettingsPreset`) con pares ajuste → valor. El selector de preset es a su vez un `ChoiceSetting` (`PresetSetting`), con la opción extra "Personalizado". | La UI no necesita código especial para los presets: el selector es un desplegable más. |
| D13 | Un preset está activo si todos sus valores coinciden con los actuales. Si el jugador cambia un ajuste que forma parte de algún preset, el selector pasa a "Personalizado". Los ajustes que no están en ningún preset (resolución, por ejemplo) no afectan. | La detección se basa en los valores, no en un flag, así que nunca se desincroniza. |
| D14 | Persistencia mediante la interfaz `ISettingsStorage`. El paquete trae `JsonFileSettingsStorage` (por defecto) e `InMemorySettingsStorage` (tests). El sistema de guardado del equipo se conecta con un adaptador propio. | La dependencia va en una sola dirección: el juego conoce el paquete; el paquete no conoce el sistema de guardado. |
| D15 | Los ajustes son **globales** (un único conjunto por instalación) y no van por slot de guardado. | Es lo que espera el jugador: cambiar de partida no cambia la resolución. |
| D16 | Los valores se guardan como `id → string`. Cada ajuste se serializa a sí mismo. Los ids que aparecen en el archivo pero no en el perfil se conservan al volver a guardar. | El formato es simple y cualquier sistema de guardado lo acepta. Quitar un ajuste del perfil y volver a ponerlo no pierde el valor. |
| D17 | Al cargar, todos los valores pasan por `Sanitize` (por ejemplo, una resolución que ya no existe se cambia por la más cercana). Un valor que no se puede leer usa el default y se registra un warning. | Un archivo corrupto o antiguo nunca debe impedir arrancar el juego. |
| D18 | URP en modo granular: al iniciar, el servicio **clona** el URP Asset activo, asigna la copia a la calidad actual y los ajustes modifican la copia. Al hacer `Dispose` se restaura el original. | Control fino sin escribir en el asset del proyecto. En URP 17.x las propiedades que necesitamos tienen setter público, así que no hace falta reflexión. |
| D19 | El código de URP va en un assembly aparte (`MiradorWorks.Settings.URP`) que solo compila si URP está instalado (`versionDefines` + `defineConstraints`). | El core funciona en cualquier pipeline. Si algún día se usa HDRP, se añade su propio assembly. |
| D20 | Etiquetas y descripciones con `LocalizedString` (`com.unity.localization` 1.5.x) como dependencia obligatoria. | El equipo va a usar Localization. Hacerlo opcional obligaría a duplicar los campos con `#if`. |
| D21 | Inicialización **explícita**: el composition root del juego crea el servicio y llama a `Initialize()`. No hay inicialización automática en v0.1.0. | Una inicialización automática necesitaría estado estático y no sabría qué storage usar si el juego tiene su adaptador. Unity ya restaura la resolución por su cuenta al arrancar el player, así que el parpadeo inicial es mínimo. |
| D22 | Sin singletons, service locator ni estado estático mutable. Se permiten cachés `static readonly` inmutables. | Mismo criterio que el pooler. Unity 6 trae Domain Reload desactivado por defecto en las plantillas, así que el estado estático sobrevive entre sesiones de Play. |
| D23 | Nada generado en la raíz del repo puede ser visible para Unity: la documentación va en `Documentation~/`, los samples en `Samples~/`. | Lo que no termina en `~` Unity lo importa y le genera `.meta`. Lo aprendimos en el CI del pooler. |
| D24 | UI de prueba en `Samples~/BasicMenu` con UGUI, generada a partir del perfil. No forma parte de la API. | Sirve como ejemplo de uso y para probar. La UI final la hace el equipo. |
| D25 | Mismo flujo que el pooler: `main` (releases), `develop` (rama por defecto), `feature/*` → PR a `develop`. Conventional Commits. CI con GameCI (tests EditMode y PlayMode). | Coherencia en toda la suite. |

---

## 3. Arquitectura

### 3.1 Visión general

```
                    ┌──────────────────────┐
                    │   SettingsProfile    │  (SO) qué ajustes y presets tiene el juego
                    └──────────┬───────────┘
                               │ lista de
              ┌────────────────┼──────────────────┐
              ▼                ▼                  ▼
     ┌────────────────┐ ┌──────────────┐ ┌────────────────┐
     │ ToggleSetting  │ │RangeSetting  │ │ ChoiceSetting  │   (SO) definiciones
     └────────┬───────┘ └──────┬───────┘ └────────┬───────┘
              └────── SettingDefinition<T> ───────┘
                               ▲
                               │ lee definiciones, guarda valores
                    ┌──────────┴───────────┐        ┌─────────────────┐
  UI del juego ───▶ │   SettingsService    │ ─────▶ │ ISettingsStorage│
  (eventos/API)     └──────────┬───────────┘        └────────┬────────┘
                               │ Apply(value, ctx)           │
                               ▼                    JsonFileSettingsStorage
                    ┌──────────────────────┐        Adaptador del equipo
                    │  SettingsApplyContext │
                    │  ├ DisplayBatch       │ → Screen.SetResolution (una vez)
                    │  └ UrpAssetHandle     │ → copia del URP Asset
                    └──────────────────────┘
```

### 3.2 Assemblies

| Assembly | Contenido | Referencias |
| --- | --- | --- |
| `MiradorWorks.Settings` | Core (definiciones base, servicio, perfil, presets, storage) y ajustes independientes del pipeline (pantalla, VSync, FPS, texturas). | `Unity.Localization` |
| `MiradorWorks.Settings.URP` | `UrpAssetHandle` y ajustes de URP. Solo compila con `MW_SETTINGS_URP`. | Core, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime` |
| `MiradorWorks.Settings.Editor` | Inspectores, validación de ids, drawer de valores de presets, menú de creación. | Core, URP (opcional) |
| `MiradorWorks.Settings.Tests.EditMode` | Tests EditMode. | Core, Test Framework |
| `MiradorWorks.Settings.Tests.PlayMode` | Tests PlayMode. | Core, URP, Test Framework |

`versionDefines` del assembly URP:

```json
"versionDefines": [
  { "name": "com.unity.render-pipelines.universal", "expression": "17.0.0", "define": "MW_SETTINGS_URP" }
],
"defineConstraints": [ "MW_SETTINGS_URP" ]
```

`versionDefines` define el símbolo solo si el paquete está instalado en esa versión o superior. `defineConstraints` hace que el assembly entero se ignore si el símbolo no existe. Juntos dan una dependencia opcional sin un solo `#if` en el código.

### 3.3 Estructura del repo

```
mw-settings-wizard/                  (raíz del repo = raíz del paquete)
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md                       (si el repo es público)
├── .gitignore
├── .github/workflows/               (CI GameCI)
├── Documentation~/
│   └── DESIGN.md
├── Runtime/
│   ├── MiradorWorks.Settings.asmdef
│   ├── Core/                        SettingDefinition, SettingsService, SettingsProfile...
│   ├── Storage/                     ISettingsStorage, SettingsData, JsonFile, InMemory
│   ├── Presets/                     SettingsPreset, PresetSetting
│   ├── Applying/                    SettingsApplyContext, IApplyBatch, DisplayBatch
│   └── Settings/                    DisplayMode, Resolution, VSync, FrameRateLimit, TextureQuality
├── Runtime.URP/
│   ├── MiradorWorks.Settings.URP.asmdef
│   ├── UrpAssetHandle.cs
│   └── Settings/                    RenderScale, ShadowQuality, Msaa
├── Editor/
│   └── MiradorWorks.Settings.Editor.asmdef
├── Tests/
│   ├── EditMode/
│   └── PlayMode/
└── Samples~/
    └── BasicMenu/
```

---

## 4. Especificación

> Las firmas son orientativas. Pueden cambiar detalles durante la implementación, pero no el contrato.

### 4.1 Definiciones

```csharp
public enum ApplyMode { Immediate, Deferred }
 
public abstract class SettingDefinition : ScriptableObject
{
    [SerializeField] string id;
    [SerializeField] LocalizedString label;
    [SerializeField] LocalizedString description;
    [SerializeField] SettingCategory category;
    [SerializeField] ApplyMode applyMode = ApplyMode.Immediate;
    [SerializeField] bool requiresConfirmation;
 
    public string Id => id;
    public LocalizedString Label => label;
    public LocalizedString Description => description;
    public SettingCategory Category => category;
    public ApplyMode ApplyMode => applyMode;
    public bool RequiresConfirmation => requiresConfirmation;
    public abstract Type ValueType { get; }
 
    // Is the setting usable on this platform/configuration? (e.g. exclusive fullscreen outside Windows)
    public virtual bool IsSupported(SettingsContext context) => true;
}
 
public abstract class SettingDefinition<T> : SettingDefinition
{
    public override Type ValueType => typeof(T);
 
    public abstract T GetDefault(SettingsContext context);       // can depend on runtime (native resolution)
    public virtual T Sanitize(T value, SettingsContext context) => value;
    public abstract void Apply(T value, SettingsApplyContext context);
 
    public abstract string Serialize(T value);
    public abstract bool TryDeserialize(string raw, out T value);
 
    public virtual bool AreEqual(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);
}
```

### 4.2 Tipos base para la UI

```csharp
public abstract class ToggleSetting : SettingDefinition<bool> { /* serialize "true"/"false" */ }
 
public abstract class RangeSetting : SettingDefinition<float>
{
    [SerializeField] float min, max, step;
    public float Min => min; public float Max => max; public float Step => step;
    // Sanitize clamps and snaps to step. AreEqual uses Mathf.Approximately.
}
 
public readonly struct ChoiceOption
{
    public string Key { get; }
    public LocalizedString LocalizedLabel { get; }   // static options
    public string RawLabel { get; }                  // generated options ("1920 × 1080 @ 144 Hz")
}
 
public abstract class ChoiceSetting : SettingDefinition<string>
{
    public abstract IReadOnlyList<ChoiceOption> GetOptions(SettingsContext context);
    // Sanitize: if the key is not among the options -> FindClosest() -> default.
    protected virtual string FindClosest(string key, IReadOnlyList<ChoiceOption> options) => null;
}
```

### 4.3 Perfil y categorías

```csharp
public sealed class SettingCategory : ScriptableObject
{
    [SerializeField] string id;
    [SerializeField] LocalizedString label;
    [SerializeField] int order;
}
 
public sealed class SettingsProfile : ScriptableObject
{
    [SerializeField] List<SettingDefinition> settings;
    [SerializeField] List<SettingsPreset> presets;
 
    public IReadOnlyList<SettingDefinition> Settings => settings;
    public IReadOnlyList<SettingsPreset> Presets => presets;
 
    // Returns errors: null entries, empty ids, duplicate ids, presets referencing settings outside the profile.
    public IReadOnlyList<string> Validate();
}
```

El servicio llama a `Validate()` al inicializarse y lanza `InvalidOperationException` si hay errores. El Editor los muestra en el inspector del perfil.

### 4.4 Servicio

```csharp
public sealed class SettingsService : IDisposable
{
    public SettingsService(SettingsProfile profile, ISettingsStorage storage);
 
    public void Initialize();                        // validate -> load -> sanitize -> apply all
 
    public T Get<T>(SettingDefinition<T> setting);          // value the player sees (pending if any)
    public T GetApplied<T>(SettingDefinition<T> setting);   // value actually applied
    public void Set<T>(SettingDefinition<T> setting, T value);
 
    public bool HasPendingChanges { get; }
    public void ApplyPending();
    public void RevertPending();
 
    public void ResetToDefaults(SettingCategory category = null);
 
    public SettingsContext Context { get; }          // for ChoiceSetting.GetOptions in the UI
 
    public event Action<SettingDefinition> ValueChanged;               // Get() changed
    public event Action<SettingDefinition> ValueApplied;               // applied to the engine
    public event Action<ConfirmationRequest> ConfirmationRequested;
 
    public void Dispose();                           // restores the original URP Asset, cancels confirmations
}
```

**Flujo de `Set`:**

1. `Sanitize` del valor.
2. Si es igual al actual, no hace nada.
3. `Immediate` → aplica, guarda, `ValueChanged` y `ValueApplied`.
4. `Deferred` → lo marca como pendiente y lanza `ValueChanged`.
5. Recalcula el preset activo (D13).
**Flujo de `ApplyPending`:**

6. Aplica todos los pendientes con un único `SettingsApplyContext`.
7. Confirma los lotes (`IApplyBatch.Commit`).
8. Si alguno tiene `RequiresConfirmation`, lanza `ConfirmationRequested` y **no guarda** hasta confirmar.
9. Si no, guarda.
**Guardado:** el servicio guarda tras cada aplicación confirmada. No hay botón de "Guardar" separado.

### 4.5 Confirmación

```csharp
public sealed class ConfirmationRequest
{
    public float TimeoutSeconds { get; }
    public float RemainingSeconds { get; }   // unscaled time
    public IReadOnlyList<SettingDefinition> Settings { get; }
    public void Confirm();                   // keep and save
    public void Cancel();                    // revert to previous values now
    public event Action<bool> Completed;     // true = confirmed, false = reverted (manual or timeout)
}
```

El temporizador es un `Awaitable` que comprueba `Time.unscaledTime` cada frame y se cancela con un `CancellationToken` en `Dispose`. Solo puede haber una petición activa; si llega otra, la anterior se cancela (se revierte).

### 4.6 Aplicación y lotes

```csharp
public interface IApplyBatch { void Commit(); }
 
public sealed class SettingsApplyContext
{
    public SettingsContext Settings { get; }
    public TBatch GetBatch<TBatch>() where TBatch : class, IApplyBatch, new();   // one per context
}
 
public sealed class DisplayBatch : IApplyBatch
{
    public FullScreenMode? Mode;
    public Resolution? Resolution;   // width, height, refreshRateRatio
    public void Commit();            // a single Screen.SetResolution with the final values
}
```

`SettingsContext` da acceso de solo lectura a los valores actuales de otros ajustes (`TryGet<T>(id, out T)`) y a servicios compartidos que añaden los assemblies opcionales (por ejemplo `UrpAssetHandle`).

### 4.7 Presets

```csharp
public sealed class SettingsPreset : ScriptableObject
{
    [SerializeField] string key;                  // "low", "medium"...
    [SerializeField] LocalizedString label;
    [SerializeField] List<Entry> entries;
 
    [Serializable] public struct Entry
    {
        public SettingDefinition setting;
        public string serializedValue;            // edited with a typed drawer in the Editor
    }
}
 
public sealed class PresetSetting : ChoiceSetting
{
    public const string CustomKey = "custom";
    // Options: the profile's presets + "Custom".
    // Apply(preset): calls Set() on each entry (respects Immediate/Deferred of each one).
    // Choosing "Custom" by hand does nothing.
}
```

El valor de `PresetSetting` se recalcula siempre (D13). Se guarda solo para que la UI lo muestre igual antes de inicializar.

### 4.8 Persistencia

```csharp
[Serializable]
public sealed class SettingsData
{
    public int version = 1;
    public List<Entry> entries = new();          // JsonUtility does not serialize Dictionary
    [Serializable] public struct Entry { public string id; public string value; }
}
 
public interface ISettingsStorage
{
    bool TryLoad(out SettingsData data);
    void Save(SettingsData data);
}
```

- **`JsonFileSettingsStorage`**: `Application.persistentDataPath/settings.json` (nombre configurable). Escritura atómica: escribe en `settings.json.tmp` y luego reemplaza. Si el archivo está corrupto, `TryLoad` devuelve `false` y deja una copia `settings.json.bak`.
- **`InMemorySettingsStorage`**: para tests.
- **Adaptador del equipo**: implementa `ISettingsStorage` guardando `SettingsData` como una entrada global de su sistema.
`version` permite migraciones futuras. En v0.1.0 solo existe la 1.

### 4.9 URP

```csharp
public sealed class UrpAssetHandle : IDisposable
{
    public UniversalRenderPipelineAsset Asset { get; }   // the runtime copy
    // Create: Instantiate(original) and assign it with QualitySettings.renderPipeline.
    // Dispose: restores the original and destroys the copy.
}
```

Si al iniciar no hay un URP Asset activo, los ajustes de URP devuelven `IsSupported = false` y no se aplican. Se registra un warning, sin excepción.

---

## 5. Ajustes incluidos en v0.1.0

| Ajuste | Tipo | Id por defecto | Aplica | API | Notas |
| --- | --- | --- | --- | --- | --- |
| Modo de pantalla | Choice | `display.mode` | Deferred + confirmación | `DisplayBatch` | Ventana, sin bordes, exclusiva (solo Windows). |
| Resolución | Choice | `display.resolution` | Deferred + confirmación | `DisplayBatch` | Opciones de `Screen.resolutions`, deduplicadas. Clave `WxH@Hz`. Si falta, la más cercana. |
| VSync | Toggle | `display.vsync` | Immediate | `QualitySettings.vSyncCount` | |
| Límite de FPS | Choice | `display.fps` | Immediate | `Application.targetFrameRate` | 30/60/120/144/240/ilimitado (configurable). Unity lo ignora con VSync activo; la UI de ejemplo lo deshabilita. |
| Calidad de texturas | Choice | `graphics.textures` | Immediate | `QualitySettings.globalTextureMipmapLimit` | Completa, media, baja, muy baja (0–3). |
| Render scale | Range | `graphics.renderscale` | Immediate | `UniversalRenderPipelineAsset.renderScale` | 0.5–2.0 por defecto. |
| Calidad de sombras | Choice | `graphics.shadows` | Immediate | `shadowDistance`, `shadowCascadeCount`, `mainLightShadowmapResolution`, `additionalLightsShadowmapResolution` | Niveles definidos en el inspector. "Desactivadas" = distancia 0. |
| Antialiasing (MSAA) | Choice | `graphics.msaa` | Immediate | `msaaSampleCount` | Off, 2x, 4x, 8x. FXAA/SMAA quedan para después (ver 10). |
| Preset | Choice | `graphics.preset` | Según cada ajuste | `PresetSetting` | Bajo/Medio/Alto/Ultra se crean como assets de ejemplo. |

Los ids por defecto vienen en los assets que crea el menú del Editor. Cada juego puede cambiarlos antes de su primera release; después, no.

---

## 6. Cómo añadir un ajuste nuevo

Ejemplo: un toggle de "motion blur" que activa un override del Volume.

```csharp
[CreateAssetMenu(menuName = "Mirador Works/Settings/Motion Blur")]
public sealed class MotionBlurSetting : ToggleSetting
{
    [SerializeField] bool defaultValue = true;
 
    public override bool GetDefault(SettingsContext context) => defaultValue;
 
    public override void Apply(bool value, SettingsApplyContext context)
    {
        // Whatever the game needs: a Volume profile, a global shader keyword...
    }
}
```

1. Crear el asset, ponerle `id`, etiqueta y categoría.
2. Añadirlo al `SettingsProfile`.
3. Listo: el servicio lo carga, lo guarda y la UI lo pinta como un toggle.
Si el ajuste necesita algo que el contexto todavía no ofrece (una referencia a las cámaras, por ejemplo), se añade un servicio al contexto. No se pone un singleton dentro del ajuste.

---

## 7. Tests

### EditMode

- Serialización ida y vuelta de cada tipo base; `TryDeserialize` con datos inválidos.
- `Sanitize`: clamp y step en Range; clave inexistente en Choice.
- `SettingsProfile.Validate`: nulls, ids vacíos y duplicados, presets que apuntan fuera del perfil.
- Servicio con ajustes falsos (`FakeToggleSetting`, que registra las llamadas a `Apply`) e `InMemorySettingsStorage`:
  - Immediate frente a Deferred, `ApplyPending` y `RevertPending`.
  - Los ids desconocidos se conservan al guardar.
  - Un archivo corrupto no rompe `Initialize`.
  - Los lotes se confirman una sola vez por aplicación.
- Presets: aplicar uno, detectarlo como activo y pasar a "Personalizado" al cambiar un ajuste.
- `JsonFileSettingsStorage` en una carpeta temporal: escritura atómica y `.bak` con un archivo corrupto.

### PlayMode

- Confirmación: confirmar, cancelar y agotar el tiempo con `Time.timeScale = 0`.
- Ajustes reales de `QualitySettings` (VSync, texturas): se aplican y se restauran al terminar el test.
- `UrpAssetHandle`: clona, modifica la copia sin tocar el original y restaura en `Dispose`. Necesita URP en el proyecto del CI.
La resolución y el modo de pantalla no se pueden probar en batchmode (no hay ventana). Se prueban a mano en el sandbox y en build.

---

## 8. Riesgos

| Id | Riesgo | Mitigación |
| --- | --- | --- |
| R1 | Cambiar `QualitySettings` (VSync, mipmap limit, `renderPipeline`) en Play Mode dentro del Editor podría quedarse escrito en `ProjectSettings/QualitySettings.asset`. | Verificarlo en T2.2. Si pasa, el assembly Editor guarda una copia de los valores al entrar en Play Mode y la restaura al salir. |
| R2 | Una API de URP cambia en una versión futura. | Todo el acceso a URP está en `Runtime.URP`. Un test PlayMode por propiedad avisa en el CI. |
| R3 | Shader stripping: si el URP Asset original no tiene sombras (o sombras suaves) activadas, la build no incluye esas variantes y la copia no podrá activarlas. | El asset original debe tener activadas las capacidades máximas que se ofrecen. Se documenta en el README y el Editor avisa si el perfil incluye sombras y el asset no las soporta. |
| R4 | Las sombras suaves no tienen setter público en URP 17.x. | Fuera de v0.1.0. Se decide en el futuro: per-light o swap de asset. |
| R5 | Localization trae Addressables como dependencia y requiere configurar Localization Settings. | Documentarlo en el README. El sample incluye una tabla de strings de ejemplo. |
| R6 | `Screen.SetResolution` se aplica al final del frame, no en el momento. | La cuenta atrás de la confirmación empieza un frame después del commit. |

---

## 9. Hoja de ruta de v0.1.0

| Tarea | Descripción | Hecho cuando |
| --- | --- | --- |
| T0.1 | Crear el repo `mw-settings-wizard`, ramas `main` y `develop` (por defecto), `.gitignore`, `LICENSE.md` (si es público) y este documento en `Documentation~/`. | Repo en GitHub con ambas ramas y el diseño subido. |
| T0.2 | `README.md` (ES + EN) y `CHANGELOG.md`. | PR a `develop` mergeado. |
| T0.3 | Esqueleto: `package.json`, asmdefs (con `versionDefines` de URP) y carpetas. Sandbox `SettingsSandbox` referenciando por `file:`. | El paquete importa sin errores en el sandbox. |
| T0.4 | CI con GameCI (adaptado del pooler), con URP en el proyecto de tests. | Pipeline verde con un test trivial. |
| T1.1 | `SettingDefinition`, `SettingDefinition<T>` y tipos base (`Toggle`, `Range`, `Choice`, `ChoiceOption`). `SettingCategory`. | Tests de serialización y sanitize. |
| T1.2 | `SettingsData`, `ISettingsStorage`, `InMemorySettingsStorage`, `JsonFileSettingsStorage`. | Tests de storage. |
| T1.3 | `SettingsProfile` y `Validate`. | Tests de validación. |
| T1.4 | `SettingsService`: Initialize, Get/Set, Immediate/Deferred, Apply/Revert, Reset, eventos y guardado. | Tests del servicio con fakes. |
| T1.5 | `SettingsApplyContext`, `IApplyBatch` y `SettingsContext`. | Test de commit único. |
| T1.6 | `ConfirmationRequest` y temporizador no escalado. | Tests PlayMode de confirmación. |
| T1.7 | `SettingsPreset`, `PresetSetting` y detección del preset activo. | Tests de presets. |
| T2.1 | `DisplayBatch`, `DisplayModeSetting`, `ResolutionSetting`. | Prueba manual en sandbox y build. |
| T2.2 | `VSyncSetting`, `FrameRateLimitSetting`, `TextureQualitySetting`. Verificar R1. | Tests PlayMode y R1 resuelto. |
| T2.3 | `UrpAssetHandle`. | Test PlayMode de clonado y restauración. |
| T2.4 | `RenderScaleSetting`, `ShadowQualitySetting`, `MsaaSetting`. | Tests PlayMode y prueba visual en sandbox. |
| T3.1 | Editor: inspector del perfil con errores, drawer tipado para valores de presets, menú "Create Default Profile" y aviso de R3. | Perfil por defecto creado desde el menú. |
| T3.2 | Sample `BasicMenu` (UGUI) con tabla de localización de ejemplo. | El sample funciona importado en un proyecto limpio. |
| T3.3 | Revisión de docs, CHANGELOG y release `v0.1.0` (merge a `main` y tag). | Instalable por URL `#v0.1.0`. |

---

## 10. Fuera de alcance y futuro

- **Audio**: volúmenes por canal. Se hará con un assembly por backend (`Settings.Audio.Unity` con AudioMixer, `Settings.Audio.FMOD`) cuando el equipo elija.
- **Controles**: remapeo con Input System. Probablemente un paquete aparte.
- **FXAA/SMAA, HDR, upscaling filter**: FXAA/SMAA van por cámara (`UniversalAdditionalCameraData`), así que necesitan un servicio de cámaras en el contexto.
- **Sombras suaves, calidad de post-procesado, brillo/gamma.**
- **Consola y móvil**: los ajustes de pantalla devolverían `IsSupported = false`; faltan ajustes propios de cada plataforma.
- **HDRP**: un assembly `Runtime.HDRP` equivalente.
- **Inicialización automática** antes de la primera escena, si el equipo la necesita.

---

## 11. Glosario

| Término | Significado |
| --- | --- |
| Definición | ScriptableObject que describe un ajuste. No guarda su valor. |
| Perfil | Lista de definiciones y presets que usa un juego. |
| Pendiente | Valor cambiado en un ajuste `Deferred` que todavía no se ha aplicado. |
| Lote | Objeto que acumula cambios de varios ajustes para aplicarlos al motor de una vez. |
| Preset | Conjunto de valores con nombre (Bajo, Alto...). |
| Composition root | Punto del juego donde se crean y conectan los servicios (un bootstrap o una escena inicial). |
| Instalación | `https://github.com/<owner>/mw-settings-wizard.git#v0.1.0` en el Package Manager. |

---

## 12. Historial

| Versión | Fecha | Cambios |
|---|---|---|
| 0.1 | 2026-11-09 | Primera versión: decisiones D1–D25, arquitectura, especificación y hoja de ruta de v0.1.0. |
