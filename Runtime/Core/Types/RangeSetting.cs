using System.Globalization;
using UnityEngine;

namespace MiradorWorks.Settings
{
    public abstract class RangeSetting : SettingDefinition<float>
    {
        [SerializeField] float min;
        [SerializeField] float max;
        [Tooltip("0 = continuous")]
        [SerializeField] float step;
        [SerializeField] float defaultValue = 1f;

        public float Min => min;
        public float Max => max;
        public float Step => step;

        public override float GetDefault(SettingsContext context) => defaultValue;

        public override float Sanitize(float value, SettingsContext context)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                value = GetDefault(context);

            value = Mathf.Clamp(value, min, max);

            if (step > 0f)
            {
                value = min + Mathf.Round((value - min) / step) * step;
                value = Mathf.Min(value, max);
            }

            return value;
        }

        // Invariant culture: with es-ES, 1.5 would be written as "1,5" and break the file on another machine.
        public override string Serialize(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        public override bool TryDeserialize(string raw, out float value)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value) && !float.IsInfinity(value))
                return true;

            value = default;
            return false;
        }

        public override bool AreEqual(float a, float b) => Mathf.Approximately(a, b);

        protected virtual void OnValidate()
        {
            if (max < min) max = min;
            if (step < 0f) step = 0f;
            defaultValue = Mathf.Clamp(defaultValue, min, max);
        }
    }
}