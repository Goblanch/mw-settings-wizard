using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Holds the current values of a profiles's settings, applies them, saves them and notifies changes.
    /// Create one on your game0s entry point, call <see cref="Initialize"/>, and dispose it on shutdown.
    /// </summary>
    public sealed class SettingsService : IDisposable
    {
        const string LogPrefix = "[MiradorWorks.Settings] ";

        readonly SettingsProfile profile;
        readonly ISettingsStorage storage;
        readonly List<SettingSlot> slots = new List<SettingSlot>();
        readonly Dictionary<SettingDefinition, SettingSlot> slotsByDefinition = new Dictionary<SettingDefinition, SettingSlot>();

        SettingsData data;
        bool initialized;
        bool disposed;
        int saveBatchDepth;
        bool saveRequested;

        public SettingsService(SettingsProfile profile, ISettingsStorage storage)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            this.profile = profile;
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Context = new SettingsContext();
        }

        /// <summary>The value returned by Get changed (applied, pending, reverted or reset)./// </summary>
        public event Action<SettingDefinition> ValueChanged;

        /// <summary>The value became the applied value and was saved. Not raised during Initialize./// </summary>
        public event Action<SettingDefinition> ValueApplied;

        public SettingsProfile Profile => profile;
        public SettingsContext Context { get; }
        public bool IsInitialized => initialized && !disposed;

        public bool HasPendingChanges
        {
            get
            {
                ThrowIfNotReady();
                foreach (SettingSlot slot in slots)
                {
                    if (slot.HasPending)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Validates the profile, loads saved values and applies every setting.
        /// </summary>
        public void Initialize()
        {
            ThrowIfDisposed();
            if (initialized)
                throw new InvalidOperationException("The settings service is already initialized.");

            IReadOnlyList<string> errors = profile.Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Settings profile '{profile.name}' is invalid:\n- " + string.Join("\n- ", errors)
                );
            }

            foreach (SettingDefinition definition in profile.Settings)
            {
                SettingSlot slot = definition.CreateSlot();
                slots.Add(slot);
                slotsByDefinition.Add(definition, slot);
            }

            data = LoadData();
            foreach (SettingSlot slot in slots)
                slot.Load(data, Context);

            var applyContext = new SettingsApplyContext(Context);
            foreach (SettingSlot slot in slots)
                ApplySafely(slot, applyContext);

            initialized = true;
        }

        public bool IsSupported(SettingDefinition setting) => GetSlot(setting).IsSupported;

        /// <summary>
        /// Value the player sees: the peding one if there is one, else the applied one.
        /// </summary>
        public T Get<T>(SettingDefinition<T> setting) => GetSlot(setting).Current;

        public T GetApplied<T>(SettingDefinition<T> setting) => GetSlot(setting).Applied;

        public bool HasPending(SettingDefinition setting) => GetSlot(setting).HasPending;

        /// <summary>
        /// Sanitizes and sets a value. Immediate settings are applied and save now;
        /// deferred ones stay pending until <see cref="ApplyPending"/>.
        /// </summary>
        public void Set<T>(SettingDefinition<T> setting, T value) => SetValue(GetSlot(setting), value);

        public void ApplyPending()
        {
            ThrowIfNotReady();

            List<SettingDefinition> committed = null;
            var applyContext = new SettingsApplyContext(Context);

            foreach (SettingSlot slot in slots)
            {
                if (!slot.CommitPending())
                    continue;

                ApplySafely(slot, applyContext);
                (committed ??= new List<SettingDefinition>()).Add(slot.Definition);
            }

            if (committed == null)
                return;

            RequestSave();
            foreach (SettingDefinition definition in committed)
                Raise(ValueApplied, definition, nameof(ValueApplied));
        }

        public void RevertPending()
        {
            ThrowIfNotReady();

            List<SettingDefinition> reverted = null;
            foreach (SettingSlot slot in slots)
            {
                if (slot.RevertPending())
                    (reverted ??= new List<SettingDefinition>()).Add(slot.Definition);
            }

            if (reverted == null)
                return;

            foreach (SettingDefinition definition in reverted)
                Raise(ValueChanged, definition, nameof(ValueChanged));
        }

        /// <summary>
        /// Sets every setting (or only those of a category) to its default, following each apply mode.
        /// </summary>
        public void ResetToDefaults(SettingCategory category = null)
        {
            ThrowIfNotReady();

            saveBatchDepth++;
            try
            {
                foreach (SettingSlot slot in slots)
                {
                    if (category == null || slot.Definition.Category == category)
                        slot.ResetToDefault(this);
                }
            }
            finally
            {
                EndSaveBatch();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            ValueChanged = null;
            ValueApplied = null;
            slots.Clear();
            slotsByDefinition.Clear();
        }

        internal void SetValue<T>(SettingSlot<T> slot, T value)
        {
            SettingDefinition<T> definition = slot.TypedDefinition;
            value = definition.Sanitize(value, Context);

            if (definition.AreEqual(value, slot.Current))
                return;

            if (definition.ApplyMode == ApplyMode.Immediate)
            {
                slot.SetApplied(value);
                ApplySafely(slot, new SettingsApplyContext(Context));
                RequestSave();
                Raise(ValueChanged, definition, nameof(ValueChanged));
                Raise(ValueApplied, definition, nameof(ValueApplied));
                return;
            }

            if (definition.AreEqual(value, slot.Applied))
                slot.ClearPending();
            else
                slot.SetPending(value);

            Raise(ValueChanged, definition, nameof(ValueChanged));
        }

        SettingsData LoadData()
        {
            SettingsData loaded = null;

            try
            {
                if (!storage.TryLoad(out loaded))
                    loaded = null;
            }
            catch (Exception e)
            {
                // TryLoad must not throw, but a third-party storage might.
                Debug.LogError($"{LogPrefix}Loading settings failed. Defaults will be used.\n{e}");
                loaded = null;
            }

            loaded ??= new SettingsData();
            loaded.Normalize();
            return loaded;
        }

        void ApplySafely(SettingSlot slot, SettingsApplyContext applyContext)
        {
            try
            {
                slot.ApplyCurrent(applyContext);
            }
            catch (Exception e)
            {
                // One broken setting must not stop the others from applying.
                Debug.LogError($"{LogPrefix}Applying '{slot.Definition.Id}' failed.\n{e}");
            }
        }

        void RequestSave()
        {
            if (saveBatchDepth > 0)
                saveRequested = true;
            else
                Save();
        }

        void EndSaveBatch()
        {
            saveBatchDepth--;
            if (saveBatchDepth == 0 && saveRequested)
            {
                saveRequested = false;
                Save();
            }
        }

        void Save()
        {
            foreach (SettingSlot slot in slots)
                slot.WriteTo(data);

            try
            {
                storage.Save(data.Clone());
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogPrefix}Could not save settings. Values stay applied for this session.\n{e}");
            }
        }

        static void Raise(Action<SettingDefinition> handlers, SettingDefinition setting, string eventName)
        {
            if (handlers == null)
                return;

            // Each handler runs on its own: one failing handler must not hide the change from the rest.
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<SettingDefinition>)handler)(setting);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{LogPrefix}A {eventName} handler threw.\n{e}");
                }
            }
        }

        SettingSlot GetSlot(SettingDefinition setting)
        {
            ThrowIfNotReady();

            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            if (!slotsByDefinition.TryGetValue(setting, out SettingSlot slot))
            {
                throw new ArgumentException(
                    $"'{setting.name}' is not part of settings profile '{profile.name}'.", nameof(setting)
                );
            }

            return slot;
        }

        SettingSlot<T> GetSlot<T>(SettingDefinition<T> setting) => (SettingSlot<T>)GetSlot((SettingDefinition)setting);

        void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SettingsService));
        }

        void ThrowIfNotReady()
        {
            ThrowIfDisposed();
            if (!initialized)
                throw new InvalidOperationException("Call Initialize() before using the settings service.");
        }
    }
}