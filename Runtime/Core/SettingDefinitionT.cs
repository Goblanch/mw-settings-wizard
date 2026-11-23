using System;
using System.Collections.Generic;

namespace MiradorWorks.Settings
{
    /// <summary>Typed setting definition. Inherit from this to create a setting with a custom value type./// </summary>
    public abstract class SettingDefinition<T> : SettingDefinition
    {
        public sealed override Type ValueType => typeof(T);

        /// <summary>Default value. May depend on runtime state (e.g. native resolution)./// </summary>
        public abstract T GetDefault(SettingsContext context);

        /// <summary>Turns any value into a valid one (clamp, closest option...). Must not throw./// </summary>
        public virtual T Sanitize(T value, SettingsContext context) => value;

        /// <summary>Applies the value to the engine or the game./// </summary>
        public abstract void Apply(T value, SettingsApplyContext context);

        public abstract string Serialize(T value);

        /// <summary>Returns false if the raw value cannot be read. Must not throw./// </summary>
        public abstract bool TryDeserialize(string raw, out T value);

        public virtual bool AreEqual(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

        internal sealed override SettingSlot CreateSlot() => new SettingSlot<T>(this);

        internal override bool CanDeserialize(string raw) => TryDeserialize(raw, out _);
    }
}