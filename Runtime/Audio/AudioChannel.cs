using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Where a volume goes: an AudioMixer parameter, the AudioListener, or another audio backend.
    /// Subclass it to support another backend;
    /// </summary>
    public abstract class AudioChannel : ScriptableObject
    {
        /// <summary>False when the cannel is not set up. Settings that use it are then unsupported./// </summary>
        public virtual bool IsAvailable => true;

        internal void Apply(float linear01) => ApplyVolume(linear01);

        internal void Restore() => RestoreVolume();

        protected abstract void ApplyVolume(float linear01);

        protected abstract void RestoreVolume();
    }
}