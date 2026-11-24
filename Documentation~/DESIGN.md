# MiradorWorks Settings — Documento de diseño

> Versión del documento: 0.3 (24 nov 2026)
> Estado: núcleo (T1) y todos los ajustes de v0.1.0 (T2) implementados. Siguiente: Editor y sample (T3).
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
    - [4.2 Tipos base](#42-tipos-base)
    - [4.3 Perfil](#43-perfil)
    - [4.4 Servicio](#44-servicio)
    - [4.5 Confirmación](#45-confirmación)
    - [4.6 Contexto y lotes](#46-contexto-y-lotes)
    - [4.7 Presets](#47-presets)
    - [4.8 Persistencia](#48-persistencia)
    - [4.9 URP](#49-urp)
  - [5. Ajustes incluidos en v0.1.0](#5-ajustes-incluidos-en-v010)
  - [6. Cómo añadir un ajuste nuevo](#6-cómo-añadir-un-ajuste-nuevo)
  - [7. Tests](#7-tests)
    - [Infraestructura](#infraestructura)
    - [EditMode](#editmode)
    - [PlayMode](#playmode)
    - [Manual (build)](#manual-build)
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
- **Sin UI propia.** El paquete expone datos y eventos; la UI la hace cada juego. Se incluye un sample de prueba.
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
| D1 | Un repo por paquete UPM. Repo `mw-settings-wizard`, package id `com.miradorworks.settings`, namespace `MiradorWorks.Settings`. | Cada paquete se versiona y se instala por separado. Unity solo usa el `name` del `package.json`, así que el repo no tiene por qué llamarse igual que el id. |
| D2 | La raíz del repo es la raíz del paquete. No hay proyecto Unity dentro del repo; se prueba desde un sandbox aparte que lo referencia con `file:`. El CI usa un proyecto propio en `.ci/TestProject`. | Instalación directa por URL de Git. El sandbox no contamina el paquete. |
| D3 | Cada ajuste es un ScriptableObject que hereda de `SettingDefinition<T>`. La definición **describe** el ajuste pero **no guarda el valor actual**. `SettingDefinition` tiene constructor `private protected`: solo `SettingDefinition<T>` hereda de ella directamente. | En el Editor, los cambios a un ScriptableObject en Play Mode se quedan en el asset. Así, además, todo ajuste tiene `Apply` y `Serialize`. |
| D4 | El valor actual vive en `SettingsService`, en un *slot* tipado (`SettingSlot<T>`) que crea cada definición con `CreateSlot()` (interno). | Se puede testear sin escena, y los valores nunca se convierten a `object` (sin boxing). |
| D5 | Tres tipos base forman el contrato con la UI: `ToggleSetting` (bool), `RangeSetting` (float) y `ChoiceSetting` (string, la clave de la opción). Cada uno trae su valor por defecto serializado (`defaultValue` o `defaultKey`); `GetDefault` se puede sobrescribir cuando depende del runtime. | La UI solo necesita pintar tres controles. Un ajuste nuevo suele implementar solo `Apply`. |
| D6 | Los `ChoiceSetting` guardan la **clave** de la opción, nunca su índice. | La lista de opciones puede cambiar entre ejecuciones (otro monitor, otra versión). |
| D7 | Cada ajuste tiene un `id` escrito a mano, clave de persistencia. Formato (`SettingId`): 1–64 caracteres ASCII entre letras, dígitos, `.`, `_` y `-`. Ids vacíos, inválidos o duplicados son errores del perfil. | Renombrar un asset no rompe los guardados, y el id viaja sin problemas a cualquier sistema de guardado. |
| D8 | Los ajustes y presets de un juego se eligen en un `SettingsProfile`. `SettingsProfile.Create` permite montarlo en runtime (tests, herramientas). | El perfil es la lista de lo que existe para el servicio y la UI. |
| D9 | Cada ajuste define su modo de aplicación: `Immediate` (se aplica al cambiarlo) o `Deferred` (pendiente hasta `ApplyPending()`). `RevertPending()` descarta los pendientes. Si un diferido vuelve a su valor aplicado, deja de estar pendiente. | Hay ajustes que se quieren ver al instante (render scale) y otros que es mejor agrupar (resolución). |
| D10 | `RequiresConfirmation` lanza una `ConfirmationRequest`. Si no se confirma antes del timeout (`ConfirmationTimeoutSeconds`, 15 s por defecto), se revierte sola. El temporizador usa **tiempo real** y empieza un frame después de aplicar. Mientras espera, **el archivo guarda el último valor confirmado**. Una confirmación nueva revierte la que estaba en curso. `Dispose` la completa sin revertir. | Si el monitor se queda en negro, el jugador no puede pulsar nada; con `timeScale = 0` un temporizador escalado no avanzaría. Si el juego se cuelga durante la espera, el siguiente arranque usa la última configuración buena. |
| D11 | Cada operación del servicio es una **pasada**. Los ajustes de una pasada comparten un `SettingsApplyContext`, y sus lotes (`IApplyBatch`, uno por tipo) se confirman una vez al final. Fin de pasada: lotes → selector de preset → guardado → eventos → confirmación. Las pasadas se anidan; solo cuenta la exterior. | Una sola llamada al motor por pasada (resolución y modo de pantalla), un solo guardado, y eventos cuando el cambio ya es real. |
| D12 | Presets: `SettingsPreset` (pares ajuste → valor serializado). `PresetSetting` es opcional (como mucho uno por perfil): un `ChoiceSetting` con los presets del perfil más `custom`. Seleccionar un preset lo aplica respetando el modo de cada ajuste. `ApplyPreset` y `ActivePreset` sirven para UIs con botones. | La UI no necesita código especial para presets. |
| D13 | El valor del selector **se calcula** al final de cada pasada: el primer preset cuyos valores coinciden con los actuales (`Current`), o `custom`. Un preset vacío nunca está activo. `ResetToDefaults` se salta el selector. | Nunca se desincroniza. Resetear el selector aplicaría un preset y pisaría los defaults. |
| D14 | Persistencia mediante `ISettingsStorage`. `TryLoad` nunca lanza (si falla, defaults); `Save` puede lanzar, y el servicio lo registra. El paquete trae `JsonFileSettingsStorage` (escritura atómica, copia `.bak` de archivos corruptos, ruta resuelta al usarla) e `InMemorySettingsStorage`. | La dependencia va en una sola dirección: el juego conoce el paquete, no al revés. Un archivo roto nunca impide arrancar. |
| D15 | Los ajustes son **globales** (un único conjunto por instalación). | Cambiar de partida no cambia la resolución. |
| D16 | `SettingsData` guarda `id → string` y está encapsulado (`Set`, `TryGetValue`, `Remove`, `Clone`). `Normalize` limpia los datos que vienen de fuera (ids vacíos, duplicados: gana el último). Los ids que no están en el perfil se conservan al guardar. | Formato simple para cualquier sistema de guardado. Quitar un ajuste y volver a ponerlo no pierde su valor. |
| D17 | Al cargar, todo valor pasa por `Sanitize`. Un valor ilegible usa el default y deja un warning. | Un archivo editado a mano o antiguo nunca rompe el juego. |
| D18 | URP granular: `UrpAssetHandle` copia el URP Asset activo y lo asigna a `QualitySettings.renderPipeline`; `Dispose` lo restaura. Los ajustes lo obtienen con `UrpAssetHandle.Get(context)`: una copia por servicio, creada al primer uso y destruida con él. | Control fino sin escribir en el asset del proyecto. En URP 17 las propiedades necesarias tienen setter público. |
| D19 | El código de URP va en `MiradorWorks.Settings.URP`, que solo compila si URP 17+ está instalado (`versionDefines` + `defineConstraints`). | El core funciona en cualquier pipeline. |
| D20 | Etiquetas y descripciones con `LocalizedString` (`com.unity.localization` 1.5.x), dependencia obligatoria. | El equipo va a usar Localization. |
| D21 | Inicialización **explícita**: el composition root crea el servicio y llama a `Initialize()`. | Una automática necesitaría estado estático y no sabría qué storage usar. |
| D22 | Sin singletons, service locator ni estado estático mutable. Se permiten `const`, `static readonly` inmutables y funciones estáticas puras. | Con Domain Reload desactivado, el estado estático sobrevive entre sesiones de Play. |
| D23 | Nada generado en la raíz puede ser visible para Unity: `Documentation~/`, `Samples~/`, `.ci/`. | Lo que no termina en `~` o empieza por `.` se importa y genera `.meta`. |
| D24 | UI de prueba en `Samples~/BasicMenu` con UGUI. | Ejemplo de uso; la UI final la hace el equipo. |
| D25 | Flujo `main` / `develop` / `feature/*`, Conventional Commits, CI con GameCI (EditMode y PlayMode). | Coherencia en toda la suite. |
| D26 | Ninguna API marcada como incompatible por el analizador UAC (`AppDomain.GetAssemblies`, `Assembly.Location`…): usar `CurrentAssemblies`, `TypeCache`. | Unity carga y descarga assemblies con `AssemblyLoadContext`; esas APIs pueden devolver assemblies descargándose. |
| D27 | Los tests que cambian el motor (`QualitySettings`, `Application`, URP) van **solo en PlayMode** y restauran los valores en el TearDown. | Unity restaura los Quality Settings al salir de Play Mode (R1). En EditMode se escribirían en el proyecto. |
| D28 | `SettingsContext` da acceso a los valores de otros ajustes (por referencia o por id, solo si ya están cargados) y a servicios compartidos (`GetOrAddService`, destruidos con el servicio). | Ajustes que dependen de otros y objetos compartidos (la copia del URP Asset) sin singletons. |
| D29 | Un `Apply`, un `Save` o un handler que falla se registra y no detiene al resto. Cada handler se ejecuta aislado. | Un ajuste roto no deja el juego sin configurar, y un handler de la UI no oculta el cambio a los demás. |
| D30 | La lógica pura se separa del motor en helpers internos (`DisplayModes`, `ResolutionOptions`). | Se testea sin pantalla; lo que toca el motor se prueba en PlayMode o en build. |

---

## 3. Arquitectura

### 3.1 Visión general

```
                    ┌────────────────────────┐
                    │    SettingsProfile     │  (SO) ajustes y presets del juego
                    └───────────┬────────────┘
                                │
           ┌────────────────────┼─────────────────────┐
           ▼                    ▼                     ▼
   ToggleSetting         RangeSetting          ChoiceSetting      (SO) definiciones
           └────────── SettingDefinition<T> ──────────┘
                                ▲ CreateSlot()
                                │
                    ┌───────────┴────────────┐          ┌──────────────────┐
  UI del juego ───▶ │    SettingsService     │ ───────▶ │ ISettingsStorage │
  Get/Set/eventos   │  SettingSlot<T> × N    │          └──────────────────┘
                    └───────────┬────────────┘           JsonFile · InMemory · adaptador
                                │ pasada
                                ▼
                    ┌────────────────────────┐
                    │  SettingsApplyContext  │ → lotes (DisplayBatch → Screen.SetResolution)
                    │  SettingsContext       │ → servicios (UrpAssetHandle → copia del URP Asset)
                    └────────────────────────┘
```

### 3.2 Assemblies

| Assembly | Contenido | Referencias |
| --- | --- | --- |
| `MiradorWorks.Settings` | Core, storage, presets y ajustes independientes del pipeline. | `Unity.Localization` |
| `MiradorWorks.Settings.URP` | `UrpAssetHandle` y ajustes de URP. Solo con `MW_SETTINGS_URP`. | Core, Localization, URP Runtime, Core RP Runtime |
| `MiradorWorks.Settings.Editor` | Inspectores, validación y menús (T3.1). | Core, Localization |
| `MiradorWorks.Settings.Tests.EditMode` | Tests EditMode. | Core, Localization, Test Framework |
| `MiradorWorks.Settings.Tests.PlayMode` | Tests PlayMode (URP dentro de `#if MW_SETTINGS_URP`). | Core, URP, Localization, Test Framework |

### 3.3 Estructura del repo

```
mw-settings-wizard/
├── package.json · README.md · CHANGELOG.md · LICENSE · .gitignore
├── .github/workflows/test.yml
├── .ci/TestProject/            proyecto de tests del CI (URP, Domain Reload desactivado)
├── Documentation~/DESIGN.md
├── Runtime/
│   ├── Core/                   SettingDefinition(T), SettingId, SettingCategory, SettingsProfile, SettingsContext, ApplyMode
│   │   └── Types/              ToggleSetting, RangeSetting, ChoiceSetting, ChoiceOption
│   ├── Applying/               SettingsApplyContext, IApplyBatch, DisplayBatch
│   ├── Storage/                SettingsData, ISettingsStorage, JsonFileSettingsStorage, InMemorySettingsStorage
│   ├── Service/                SettingsService, SettingSlot, ConfirmationRequest
│   ├── Presets/                SettingsPreset, PresetSetting
│   └── Settings/
│       ├── Display/            DisplayModeSetting, ResolutionSetting, VSyncSetting, FrameRateLimitSetting (+ helpers)
│       └── Graphics/           TextureQualitySetting
├── Runtime.URP/                UrpAssetHandle (+ ajustes de URP, T2.4)
├── Editor/                     (T3.1)
├── Tests/
│   ├── EditMode/               Fakes/, Support/, Storage/, Display/, Quality/…
│   └── PlayMode/               Fakes/, Support/, Urp/…
└── Samples~/BasicMenu/         (T3.2)
```

---

## 4. Especificación

> Resumen de la API pública. El código es la referencia; aquí se explica el contrato.

### 4.1 Definiciones

```csharp
public abstract class SettingDefinition : ScriptableObject
{
    public string Id { get; }
    public LocalizedString Label { get; }
    public LocalizedString Description { get; }
    public SettingCategory Category { get; }
    public ApplyMode ApplyMode { get; }
    public bool RequiresConfirmation { get; }
    public abstract Type ValueType { get; }
    public virtual bool IsSupported(SettingsContext context);
}

public abstract class SettingDefinition<T> : SettingDefinition
{
    public abstract T GetDefault(SettingsContext context);
    public virtual T Sanitize(T value, SettingsContext context);      // must not throw
    public abstract void Apply(T value, SettingsApplyContext context);
    public abstract string Serialize(T value);
    public abstract bool TryDeserialize(string raw, out T value);     // must not throw
    public virtual bool AreEqual(T a, T b);
}
```

### 4.2 Tipos base

- **`ToggleSetting`:** `defaultValue`. Se guarda como `"true"`/`"false"`.
- **`RangeSetting`:** `min`, `max`, `step` (0 = continuo), `defaultValue`. `Sanitize` limita al rango y ajusta al paso; NaN/infinito pasa al default. Se guarda con `InvariantCulture`. `AreEqual` usa `Mathf.Approximately`.
- **`ChoiceSetting`:** `defaultKey` (protegido como `DefaultKey`), `GetOptions(context)`, `FindClosest` sobrescribible y `IndexOf` estático. `Sanitize`: clave existente → `FindClosest` → default. Sin opciones, `IsSupported` es `false`.
- **`ChoiceOption`:** `Key` y etiqueta `Localized(...)` (opciones fijas) o `Text(...)` (generadas, como `1920 × 1080 @ 144 Hz`).

### 4.3 Perfil

`Settings`, `Presets`, `TryGet(id)`, `Create(settings, presets)` y `Validate()`, que devuelve **todos** los errores:

- entradas vacías, assets repetidos, ids vacíos/inválidos/duplicados;
- más de un `PresetSetting`;
- presets vacíos, claves inválidas, reservadas (`custom`) o duplicadas;
- entradas de preset vacías, fuera del perfil, que apuntan al selector, repetidas o con un valor que el ajuste no sabe leer.

`Initialize` lanza `InvalidOperationException` con la lista si hay errores.

### 4.4 Servicio

```csharp
public sealed class SettingsService : IDisposable
{
    public SettingsService(SettingsProfile profile, ISettingsStorage storage);
    public void Initialize();

    public T Get<T>(SettingDefinition<T> setting);          // pending if any
    public T GetApplied<T>(SettingDefinition<T> setting);
    public void Set<T>(SettingDefinition<T> setting, T value);
    public bool IsSupported(SettingDefinition setting);
    public bool HasPending(SettingDefinition setting);
    public bool HasPendingChanges { get; }

    public void ApplyPending();
    public void RevertPending();
    public void ResetToDefaults(SettingCategory category = null);

    public void ApplyPreset(SettingsPreset preset);
    public SettingsPreset ActivePreset { get; }

    public ConfirmationRequest ActiveConfirmation { get; }
    public float ConfirmationTimeoutSeconds { get; set; }

    public SettingsProfile Profile { get; }
    public SettingsContext Context { get; }

    public event Action<SettingDefinition> ValueChanged;     // Get changed
    public event Action<SettingDefinition> ValueApplied;     // became applied (not during Initialize)
    public event Action<ConfirmationRequest> ConfirmationRequested;

    public void Dispose();
}
```

- **`Initialize`:** valida el perfil, carga con `Normalize`, deserializa y sanitiza cada valor, y los aplica todos en una pasada. No guarda ni lanza eventos de valor.
- **`Set`:** sanitiza; si no cambia, no hace nada. Inmediato: aplica y guarda. Diferido: lo deja pendiente.
- **Fin de pasada:** ver D11.

### 4.5 Confirmación

```csharp
public sealed class ConfirmationRequest
{
    public IReadOnlyList<SettingDefinition> Settings { get; }
    public float TimeoutSeconds { get; }
    public float RemainingSeconds { get; }   // real time
    public bool IsCompleted { get; }
    public bool IsConfirmed { get; }
    public event Action<bool> Completed;     // once
    public void Confirm();
    public void Cancel();
}
```

El temporizador usa `Awaitable.NextFrameAsync` con un `CancellationToken` y `Time.realtimeSinceStartupAsDouble`. Si en un mismo `ApplyPending` cambian varios ajustes con confirmación, van en una sola petición.

### 4.6 Contexto y lotes

```csharp
public sealed class SettingsContext
{
    public SettingsProfile Profile { get; }
    public bool TryGetValue<T>(SettingDefinition<T> setting, out T value);
    public bool TryGetApplied<T>(SettingDefinition<T> setting, out T value);
    public bool TryGetValue<T>(string id, out T value);
    public TService GetOrAddService<TService>(Func<TService> factory) where TService : class;
}

public sealed class SettingsApplyContext
{
    public SettingsContext Settings { get; }
    public TBatch GetBatch<TBatch>() where TBatch : class, IApplyBatch, new();
}

public interface IApplyBatch { void Commit(SettingsContext context); }
```

Durante `Initialize` los ajustes se cargan en el orden del perfil: `TryGetValue` devuelve `false` si el otro aún no está cargado. Un `SettingsApplyContext` no se puede usar después de su pasada.

`DisplayBatch` (`Mode`, `Resolution`) completa lo que falte con el estado actual de la pantalla, y no llama a `Screen.SetResolution` si nada cambia.

### 4.7 Presets

- **`SettingsPreset`:** `Key`, `Label`, `Entries` (`Setting`, `Value`) y `Create(key, entries)`.
- **`PresetSetting`:** `CustomKey = "custom"` y `CustomLabel`. Sus opciones son los presets del perfil más `custom`. Su `Apply` no hace nada: es el servicio el que aplica los presets.

### 4.8 Persistencia

```csharp
[Serializable] public sealed class SettingsData
{
    public const int CurrentVersion = 1;
    public int Version { get; }
    public IReadOnlyList<Entry> Entries { get; }   // Entry: Id, Value
    public bool TryGetValue(string id, out string value);
    public void Set(string id, string value);
    public bool Remove(string id);
    public SettingsData Clone();
}

public interface ISettingsStorage
{
    bool TryLoad(out SettingsData data);   // never throws
    void Save(SettingsData data);          // may throw
}
```

`JsonFileSettingsStorage`: `persistentDataPath/settings.json` por defecto o la ruta que se le pase. Escribe en `.tmp` y lo reemplaza con `File.Replace` (si el sistema de archivos no lo admite, usa una copia). Un archivo corrupto se copia a `.bak`. Un archivo de una versión más nueva se carga y deja un warning.

### 4.9 URP

```csharp
public sealed class UrpAssetHandle : IDisposable
{
    public UniversalRenderPipelineAsset Original { get; }   // read only
    public UniversalRenderPipelineAsset Asset { get; }      // runtime copy
    public static bool IsUrpActive { get; }
    public static UrpAssetHandle Get(SettingsContext context);
    public static UrpAssetHandle CreateFromActive();
    public void Dispose();
}
```

La copia se marca `HideFlags.DontSave`. `Dispose` solo restaura el pipeline si sigue siendo la copia (ver R7). Los ajustes de URP comprueban `IsUrpActive` en `IsSupported`.

---

## 5. Ajustes incluidos en v0.1.0

| Ajuste | Clase | Id sugerido | Modo sugerido | API | Notas |
| --- | --- | --- | --- | --- | --- |
| Modo de pantalla | `DisplayModeSetting` | `display.mode` | Deferred + confirmación | `DisplayBatch` | Por plataforma: Windows sin bordes/exclusiva/ventana; macOS sin bordes/maximizada/ventana; Linux sin bordes/ventana. |
| Resolución | `ResolutionSetting` | `display.resolution` | Deferred + confirmación | `DisplayBatch` | Clave `WxH@Hz` (Hz redondeados). Nativa por defecto. Oculta alturas < `minimumHeight` (720). `includeRefreshRate` opcional. |
| VSync | `VSyncSetting` | `display.vsync` | Immediate | `QualitySettings.vSyncCount` | Sin efecto en el Editor. |
| Límite de FPS | `FrameRateLimitSetting` | `display.fps` | Immediate | `Application.targetFrameRate` | `rates` configurables + `unlimited` (−1, por defecto). Ignorado con VSync activo. |
| Calidad de texturas | `TextureQualitySetting` | `graphics.textures` | Immediate | `QualitySettings.globalTextureMipmapLimit` | `full`/`half`/`quarter`/`eighth` (0–3). |
| Render scale | `RenderScaleSetting` | `graphics.renderscale` | Immediate | `renderScale` de la copia | Rango 0.5–2, paso 0.05, defecto 1. |
| Calidad de sombras | `ShadowQualitySetting` | `graphics.shadows` | Immediate | `shadowDistance`, `shadowCascadeCount`, `mainLightShadowmapResolution`, `additionalLightsShadowmapResolution` de la copia | Niveles editables `off`/`low`/`medium`/`high`; `off` = distancia 0. Por defecto, el último nivel. |
| Antialiasing (MSAA) | `MsaaSetting` | `graphics.msaa` | Immediate | `msaaSampleCount` de la copia | `off`/`2x`/`4x`/`8x`. |
| Preset | `PresetSetting` | `graphics.preset` | — | servicio | Derivado (D13). |

Los ids sugeridos son los que pondrá el menú del Editor (T3.1). Cada juego puede cambiarlos antes de su primera release; después, no.

---

## 6. Cómo añadir un ajuste nuevo

```csharp
[CreateAssetMenu(menuName = "Mirador Works/Settings/Motion Blur")]
public sealed class MotionBlurSetting : ToggleSetting
{
    public override void Apply(bool value, SettingsApplyContext context)
    {
        // Whatever the game needs: a Volume override, a global shader keyword...
    }
}
```

1. Crear el asset y ponerle `id`, etiqueta, categoría y valor por defecto.
2. Añadirlo al `SettingsProfile`.
3. El servicio lo carga, lo guarda y lo aplica, y la UI lo pinta como un toggle.

- **Si el valor por defecto depende del runtime,** se sobrescribe `GetDefault`.
- **Si necesita otro ajuste,** se usa `context.Settings.TryGetValue(...)`.
- **Si necesita un objeto compartido,** se usa `GetOrAddService`. Nunca un singleton.

---

## 7. Tests

### Infraestructura

- **`TestAssets` (EditMode):** crea ajustes, perfiles, categorías y presets, y los destruye en el TearDown. Edita los campos con `SerializedObject`.
- **`PlayModeAssets` (PlayMode):** lo mismo con `JsonUtility.FromJsonOverwrite`, porque `SerializedObject` es solo de Editor.
- **Fakes:** ajustes que cuentan las llamadas a `Apply` (y pueden fallar o depender del contexto), lotes que registran sus commits, un storage que falla al guardar y un servicio desechable.

### EditMode

Tipos base (serialización, sanitize, cultura), `SettingId`, perfil y validación de presets, `SettingsData` y los dos storages (con carpeta temporal), el servicio (inmediato y diferido, reset, guardado agrupado, ids desconocidos, aislamiento de fallos), contexto y lotes, presets, lógica de pantalla (`DisplayModes`, `ResolutionOptions`), opciones del límite de FPS y de las texturas.

### PlayMode

Confirmación (confirmar, cancelar, timeout con `timeScale = 0`, sustitución, `Dispose`), ajustes que tocan `QualitySettings` y `Application`, y `UrpAssetHandle` (crea su propio URP Asset en el `SetUp`).

### Manual (build)

Modo de pantalla, resolución, confirmación y VSync: el Editor no los aplica de verdad y el CI no tiene pantalla. Se prueban en el sandbox con un script `OnGUI` de depuración.

---

## 8. Riesgos

| Id | Riesgo | Estado y mitigación |
| --- | --- | --- |
| R1 | Cambiar `QualitySettings` en Play Mode se queda escrito en el proyecto. | **Resuelto** (24 nov 2026). En Unity 6.6, VSync, Global Mipmap Limit y `QualitySettings.renderPipeline` vuelven a su valor al salir de Play Mode, y el URP Asset del proyecto no cambia (los ajustes modifican la copia). En EditMode sí se escribiría: D27. |
| R2 | Una API de URP cambia en una versión futura. | Todo el acceso a URP está en `Runtime.URP`, con tests PlayMode por propiedad. |
| R3 | Shader stripping: si el URP Asset original no tiene sombras, la build no incluye esas variantes y la copia no podrá activarlas. | El asset original debe tener activadas las capacidades máximas. Aviso en el Editor (T3.1) y en el README. |
| R4 | Las sombras suaves no tienen setter público en URP 17. | Fuera de v0.1.0. |
| R5 | Localization trae Addressables y requiere configurar Localization Settings. | README y sample con tabla de ejemplo. |
| R6 | `Screen.SetResolution` se aplica al final del frame. | **Resuelto**: el temporizador empieza un frame después. |
| R7 | Cambiar de nivel de calidad (`QualitySettings.SetQualityLevel`) sustituye el pipeline y la copia deja de usarse. | No cambiar de nivel con este sistema activo. `Dispose` no restaura si el pipeline ya no es la copia. A futuro: escuchar el cambio de nivel y volver a aplicar. |
| R8 | El VSync y `Screen.SetResolution` no tienen efecto en el Editor. | Pruebas manuales en build. |

---

## 9. Hoja de ruta de v0.1.0

| Tarea | Descripción | Estado |
| --- | --- | --- |
| T0.1 | Repo, ramas, `.gitignore` y este documento. | ✅ |
| T0.2 | README (ES + EN) y CHANGELOG. | ✅ |
| T0.3 | `package.json`, asmdefs y sandbox. | ✅ |
| T0.4 | CI con GameCI y proyecto de tests con URP. | ✅ |
| T1.1 | Definiciones y tipos base. | ✅ |
| T1.2 | `SettingsData` y storages. | ✅ |
| T1.3 | `SettingsProfile`, `SettingId` y validación. | ✅ |
| T1.4 | `SettingsService`. | ✅ |
| T1.5 | `SettingsContext` y lotes. | ✅ |
| T1.6 | Confirmación con temporizador. | ✅ |
| T1.7 | Presets y selector. | ✅ |
| T2.1 | Modo de pantalla, resolución y `DisplayBatch`. | ✅ |
| T2.2 | VSync, límite de FPS y calidad de texturas. Comprobar R1. | ✅ |
| T2.3 | `UrpAssetHandle` y actualización de este documento. | ✅ |
| T2.4 | Render scale, sombras y MSAA. Comprobar R1 para el pipeline. | ✅ |
| T3.1 | Editor: inspector del perfil, drawer tipado para valores de presets, menú "Create Default Profile", aviso de R3. | ⬜ |
| T3.2 | Sample `BasicMenu` (UGUI) con tabla de localización. | ⬜ |
| T3.3 | Revisión de docs y release `v0.1.0`. | ⬜ |

---

## 10. Fuera de alcance y futuro

- **Audio:** un assembly por backend (`Settings.Audio.Unity`, `Settings.Audio.FMOD`) cuando el equipo elija.
- **Controles:** remapeo con Input System, probablemente en un paquete aparte.
- **FXAA/SMAA, HDR, upscaling filter:** van por cámara o tienen implicaciones de rendimiento; necesitan un servicio de cámaras.
- **Sombras suaves, post-procesado, brillo/gamma.**
- **Cambio de monitor en caliente:** refrescar las opciones de resolución.
- **Cambio de nivel de calidad** compatible con la copia del URP Asset (R7).
- **Consola, móvil y HDRP.**
- **Inicialización automática** antes de la primera escena.

---

## 11. Glosario

| Término | Significado |
| --- | --- |
| Definición | ScriptableObject que describe un ajuste. No guarda su valor. |
| Slot | Estado de un ajuste dentro del servicio: valor aplicado, pendiente y confirmado. |
| Perfil | Ajustes y presets que usa un juego. |
| Pendiente | Valor de un ajuste `Deferred` que todavía no se ha aplicado. |
| Pasada | Una operación del servicio. Los efectos (lotes, guardado, eventos) ocurren al terminarla. |
| Lote | Objeto que junta los cambios de varios ajustes para aplicarlos al motor de una vez. |
| Preset | Conjunto de valores con nombre (Bajo, Alto…). |
| Composition root | Punto del juego donde se crean y conectan los servicios. |
| Instalación | `https://github.com/<owner>/mw-settings-wizard.git#v0.1.0` en el Package Manager. |

---

## 12. Historial

| Versión | Fecha | Cambios |
| --- | --- | --- |
| 0.1 | 2026-11-09 | Primera versión: decisiones D1–D25, arquitectura, especificación y hoja de ruta. |
| 0.2 | 2026-11-24 | Diseño actualizado con lo implementado en T1–T2.2: D3–D5, D7, D8, D10–D14, D16, D18 y D22 revisadas; nuevas D26–D30. Especificación reescrita según el código. R1 y R6 resueltos; nuevos R7 y R8. Estado en la hoja de ruta. |
| 0.3 | 2026-11-24 | Ajustes de URP en la sección 5, R1 cerrado también para el pipeline, T2 completa. |
