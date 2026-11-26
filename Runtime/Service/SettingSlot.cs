using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Runtime state of one setting inside the service: applied value and optional pending value.
    /// and the last confirmed value while a confirmation is in progress.
    /// The generic subclass keeps values typed, so bools and floats are never boxed.
    /// </summary>
    internal abstract class SettingSlot
    {
        public abstract SettingDefinition Definition { get; }
        public bool IsSupported { get; protected set; }
        public bool IsLoaded { get; protected set; }
        public abstract bool HasPending { get; }
        public abstract bool AwatingConfirmation { get; }

        public abstract void Load(SettingsData data, SettingsContext context);
        public abstract void ApplyCurrent(SettingsApplyContext context);
        public abstract bool CommitPending();
        public abstract bool RevertPending();
        public abstract void WriteTo(SettingsData data);
        public abstract void ResetToDefault(SettingsService service);

        /// <summary>Remembers the applied value as the last confirmed one, before it changes./// </summary>
        public abstract void BeginConfirmation();

        /// <summary>Ends the confirmation. Returns true if reverting changed the applied value./// </summary>
        public abstract bool EndConfirmation(bool keep);

        /// <summary>Whether the current value equals a serialized value (after sanitizing it)./// </summary>
        public abstract bool CurrentEquals(string raw, SettingsContext context);

        /// <summary>Sets a serialized value through the service, following the apply mode./// </summary>
        public abstract void SetFromSerialized(SettingsService service, string raw);

    }

    internal sealed class SettingSlot<T> : SettingSlot
    {
        readonly SettingDefinition<T> definition;
        T applied;
        T pending;
        bool hasPending;
        T confirmed;
        bool awaitingConfirmation;

        public SettingSlot(SettingDefinition<T> definition)
        {
            this.definition = definition;
        }

        public override SettingDefinition Definition => definition;
        public SettingDefinition<T> TypedDefinition => definition;
        public override bool HasPending => hasPending;
        public override bool AwatingConfirmation => awaitingConfirmation;

        /// <summary>What the player sees: the pending value if there is one, else the applied one.</summary>
        public T Current => hasPending ? pending : applied;
        public T Applied => applied;

        public override void Load(SettingsData data, SettingsContext context)
        {
            IsSupported = definition.IsSupported(context);

            T value;
            if (data.TryGetValue(definition.Id, out string raw))
            {
                if (!definition.TryDeserialize(raw, out value))
                {
                    Debug.LogWarning(
                        $"[MiradorWorks.Settings] Saved value '{raw}' for '{definition.Id}' is invalid. Using the default.");
                    value = definition.GetDefault(context);
                }
            }
            else
            {
                value = definition.GetDefault(context);
            }

            applied = definition.Sanitize(value, context);
            ClearPending();
            IsLoaded = true;
        }

        public void SetApplied(T value)
        {
            applied = value;
            ClearPending();
        }

        public void SetPending(T value)
        {
            pending = value;
            hasPending = true;
        }

        public void ClearPending()
        {
            pending = default;
            hasPending = false;
        }

        public override void ApplyCurrent(SettingsApplyContext context)
        {
            if (IsSupported)
                definition.Apply(applied, context);
        }

        public override bool CommitPending()
        {
            if (!hasPending)
                return false;

            applied = pending;
            ClearPending();
            return true;
        }

        public override bool RevertPending()
        {
            if (!hasPending)
                return false;

            ClearPending();
            return true;
        }

        public override void BeginConfirmation()
        {
            if (awaitingConfirmation)
                return;

            confirmed = applied;
            awaitingConfirmation = true;
        }

        public override bool EndConfirmation(bool keep)
        {
            if (!awaitingConfirmation)
                return false;

            bool changed = false;
            if (!keep)
            {
                changed = !definition.AreEqual(applied, confirmed);
                applied = confirmed;
            }

            confirmed = default;
            awaitingConfirmation = false;
            return changed;
        }

        public override bool CurrentEquals(string raw, SettingsContext context) =>
            definition.TryDeserialize(raw, out T value)
            && definition.AreEqual(definition.Sanitize(value, context), Current);

        public override void SetFromSerialized(SettingsService service, string raw)
        {
            if (definition.TryDeserialize(raw, out T value))
            {
                service.SetValue(this, value);
                return;
            }

            Debug.LogWarning($"[MiradorWorks.Settings] Value '{raw}' for '{definition.Id}' is invalid and was skipped.");
        }

        // While waiting confirmation, the file keeps the last confirmed value.
        // A crash with a broken resolution must not make it permanent.
        public override void WriteTo(SettingsData data)
        {
            T toSave = awaitingConfirmation ? confirmed : applied;
            data.Set(definition.Id, definition.Serialize(toSave) ?? string.Empty);
        }

        public override void ResetToDefault(SettingsService service) =>
            service.SetValue(this, definition.GetDefault(service.Context));
    }
}