using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
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
        public const float DefaultConfirmationTimeoutSeconds = 15f;

        enum NotificationKind { Changed, Applied }

        readonly struct Notification
        {
            public Notification(NotificationKind kind, SettingDefinition setting)
            {
                Kind = kind;
                Setting = setting;
            }

            public NotificationKind Kind { get; }
            public SettingDefinition Setting { get; }
        }

        readonly SettingsProfile profile;
        readonly ISettingsStorage storage;
        readonly List<SettingSlot> slots = new List<SettingSlot>();
        readonly Dictionary<SettingDefinition, SettingSlot> slotsByDefinition = new Dictionary<SettingDefinition, SettingSlot>();
        readonly Dictionary<string, SettingSlot> slotsById = new Dictionary<string, SettingSlot>(StringComparer.Ordinal);
        readonly List<Notification> notifications = new List<Notification>();
        readonly List<SettingSlot> passConfirmationSlots = new List<SettingSlot>();

        SettingsData data;
        bool initialized;
        bool disposed;
        float confirmationTimeoutSeconds = DefaultConfirmationTimeoutSeconds;

        // Every change runs inside a batch: applies share one SettingsApplyContext,
        // and batchges, saving and events happen once, when the outermost batch ends.
        int batchDepth;
        SettingsApplyContext batchApplyContext;
        bool saveRequested;

        public SettingsService(SettingsProfile profile, ISettingsStorage storage)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            this.profile = profile;
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Context = new SettingsContext(this);
        }

        /// <summary>The value returned by Get changed (applied, pending, reverted or reset)./// </summary>
        public event Action<SettingDefinition> ValueChanged;

        /// <summary>
        /// The value became the applied value and was saved. Not raised during Initialize.
        /// Settings that require confirmation are saved once confirmed.
        /// </summary>
        public event Action<SettingDefinition> ValueApplied;

        /// <summary>
        /// Settings marked RequiresConfirmation changed: show a "Keep these changes?" dialog.
        /// </summary>
        public event Action<ConfirmationRequest> ConfirmationRequested;

        public SettingsProfile Profile => profile;
        public SettingsContext Context { get; }
        public bool IsInitialized => initialized && !disposed;

        /// <summary>The confirmation in progress, or null./// </summary>
        public ConfirmationRequest ActiveConfirmation { get; private set; }

        public float ConfirmationTimeoutSeconds
        {
            get => confirmationTimeoutSeconds;
            set
            {
                if (!(value > 0f) || float.IsInfinity(value))
                    throw new ArgumentOutOfRangeException(nameof(value), "The confirmation timeout must be a positive number of seconds.");

                confirmationTimeoutSeconds = value;
            }
        }

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
                slotsById.Add(definition.Id, slot);
            }

            data = LoadData();

            BeginBatch();
            try
            {
                foreach (SettingSlot slot in slots)
                    slot.Load(data, Context);

                foreach (SettingSlot slot in slots)
                    ApplySafely(slot);
            }
            finally
            {
                EndBatch();
            }

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

            BeginBatch();
            try
            {
                foreach (SettingSlot slot in slots)
                {
                    if (!slot.HasPending)
                        continue;

                    if (slot.Definition.RequiresConfirmation)
                        PrepareConfirmation(slot);

                    slot.CommitPending();

                    ApplySafely(slot);
                    saveRequested = true;
                    Notify(NotificationKind.Applied, slot.Definition);
                }
            }
            finally
            {
                EndBatch();
            }
        }

        public void RevertPending()
        {
            ThrowIfNotReady();

            BeginBatch();
            try
            {
                foreach (SettingSlot slot in slots)
                {
                    if (slot.RevertPending())
                        Notify(NotificationKind.Changed, slot.Definition);
                }
            }
            finally
            {
                EndBatch();
            }
        }

        /// <summary>
        /// Sets every setting (or only those of a category) to its default, following each apply mode.
        /// </summary>
        public void ResetToDefaults(SettingCategory category = null)
        {
            ThrowIfNotReady();

            BeginBatch();
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
                EndBatch();
            }
        }

        /// <summary>
        /// Releases shared services. A confirmation in progress completes as not confirmed,
        /// without reverting: the saved file already holds the last confirmed values.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            ConfirmationRequest confirmation = ActiveConfirmation;
            ActiveConfirmation = null;
            confirmation?.MarkCompleted(false);

            ValueChanged = null;
            ValueApplied = null;
            ConfirmationRequested = null;
            Context.DisposeServices();
            slots.Clear();
            slotsByDefinition.Clear();
            slotsById.Clear();
            notifications.Clear();
            passConfirmationSlots.Clear();
        }

        internal void SetValue<T>(SettingSlot<T> slot, T value)
        {
            SettingDefinition<T> definition = slot.TypedDefinition;
            value = definition.Sanitize(value, Context);

            if (definition.AreEqual(value, slot.Current))
                return;

            BeginBatch();
            try
            {
                if (definition.ApplyMode == ApplyMode.Immediate)
                {
                    if (definition.RequiresConfirmation)
                        PrepareConfirmation(slot);

                    slot.SetApplied(value);
                    ApplySafely(slot);
                    saveRequested = true;
                    Notify(NotificationKind.Changed, definition);
                    Notify(NotificationKind.Applied, definition);
                    return;
                }

                if (definition.AreEqual(value, slot.Applied))
                    slot.ClearPending();
                else
                    slot.SetPending(value);

                Notify(NotificationKind.Changed, definition);
            }
            finally
            {
                EndBatch();
            }
        }

        internal void CompleteConfirmation(ConfirmationRequest request, bool confirmed)
        {
            if (disposed || request.IsCompleted || request != ActiveConfirmation)
                return;

            ActiveConfirmation = null;

            BeginBatch();
            try
            {
                foreach (SettingSlot slot in request.Slots)
                {
                    if (!slot.EndConfirmation(keep: confirmed))
                        continue;

                    ApplySafely(slot);
                    Notify(NotificationKind.Changed, slot.Definition);
                    Notify(NotificationKind.Applied, slot.Definition);
                }

                saveRequested = true;
            }
            finally
            {
                EndBatch();
            }

            request.MarkCompleted(confirmed);
        }

        internal bool TryGetLoadedSlot(SettingDefinition setting, out SettingSlot slot)
        {
            if (setting != null && slotsByDefinition.TryGetValue(setting, out slot) && slot.IsLoaded)
                return true;

            slot = null;
            return false;
        }

        internal bool TryGetLoadedSlot(string id, out SettingSlot slot)
        {
            if (id != null && slotsById.TryGetValue(id, out slot) && slot.IsLoaded)
                return true;

            slot = null;
            return false;
        }

        // A new change thar needs confirmation replaces the one in progress: that one is reverted first
        void PrepareConfirmation(SettingSlot slot)
        {
            if (ActiveConfirmation != null)
                CompleteConfirmation(ActiveConfirmation, confirmed: false);

            slot.BeginConfirmation();
            if (!passConfirmationSlots.Contains(slot))
                passConfirmationSlots.Add(slot);
        }

        void BeginBatch()
        {
            if (batchDepth++ == 0)
                batchApplyContext = new SettingsApplyContext(Context);
        }

        void EndBatch()
        {
            if (--batchDepth > 0)
                return;

            SettingsApplyContext applyContext = batchApplyContext;
            batchApplyContext = null;
            applyContext.CommitBatches();

            if (saveRequested)
            {
                saveRequested = false;
                Save();
            }

            ConfirmationRequest request = CreateConfirmationRequest();
            FlushNotifications();

            if (request != null && !request.IsCompleted)
            {
                request.StartTimer();
                RaiseConfirmationRequested(request);
            }
        }

        ConfirmationRequest CreateConfirmationRequest()
        {
            if (passConfirmationSlots.Count == 0)
                return null;

            var request = new ConfirmationRequest(this, passConfirmationSlots.ToArray(), confirmationTimeoutSeconds);
            passConfirmationSlots.Clear();
            ActiveConfirmation = request;
            return request;
        }

        void Notify(NotificationKind kind, SettingDefinition setting) =>
            notifications.Add(new Notification(kind, setting));

        void FlushNotifications()
        {
            if (notifications.Count == 0)
                return;

            // Copy first: a handler call Set, which starts a new batch and adds notifications.
            Notification[] toRaise = notifications.ToArray();
            notifications.Clear();

            foreach (Notification notification in toRaise)
            {
                if (notification.Kind == NotificationKind.Changed)
                    Raise(ValueChanged, notification.Setting, nameof(ValueChanged));
                else
                    Raise(ValueApplied, notification.Setting, nameof(ValueApplied));
            }
        }

        void RaiseConfirmationRequested(ConfirmationRequest request)
        {
            Action<ConfirmationRequest> handlers = ConfirmationRequested;
            if (handlers == null)
                return;

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<ConfirmationRequest>)handler)(request);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{LogPrefix}A {nameof(ConfirmationRequested)} handler threw.\n{e}");
                }
            }
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

        void ApplySafely(SettingSlot slot)
        {
            try
            {
                slot.ApplyCurrent(batchApplyContext);
            }
            catch (Exception e)
            {
                // One broken setting must not stop the others from applying.
                Debug.LogError($"{LogPrefix}Applying '{slot.Definition.Id}' failed.\n{e}");
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