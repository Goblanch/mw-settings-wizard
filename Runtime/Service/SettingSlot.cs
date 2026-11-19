using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Runtime state of one setting inside the service: applied value and optional pending value.
    /// The generic subclass keeps values typed, so bools and floats are never boxed.
    /// </summary>
    internal abstract class SettingSlot
    {
        public abstract SettingDefinition Definition { get; }
        public bool IsSupported { get; protected set; }
        public abstract bool HasPending { get; }

        public abstract void Load(SettingsData data, SettingsContext context);
        public abstract void ApplyCurrent(SettingsApplyContext context);
        public abstract bool CommitPending();
        public abstract bool RevertPending();
        public abstract void WriteTo(SettingsData data);
        public abstract void ResetToDefault(SettingsService service);
    }

    internal sealed class SettingSlot<T> : SettingSlot
    {
        readonly SettingDefinition<T> definition;
        T applied;
        T pending;
        bool hasPending;

        public SettingSlot(SettingDefinition<T> definition)
        {
            this.definition = definition;
        }

        public override SettingDefinition Definition => definition;
        public SettingDefinition<T> TypedDefinition => definition;
        public override bool HasPending => hasPending;

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
                        $"[MiradorWorks.Setting] Saved value '{raw}' for '{definition.Id}' is invalid. Using the default."
                    );
                    value = definition.GetDefault(context);
                }
            }
            else
            {
                value = definition.GetDefault(context);
            }

            applied = definition.Sanitize(value, context);
            ClearPending();
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
            if (hasPending)
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

        public override void WriteTo(SettingsData data) =>
            data.Set(definition.Id, definition.Serialize(applied) ?? string.Empty);

        public override void ResetToDefault(SettingsService service) =>
            service.SetValue(this, definition.GetDefault(service.Context));
    }
}