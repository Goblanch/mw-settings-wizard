using UnityEngine;
using UnityEngine.Audio;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Volume through an exposed AudioMixer parameter, in decibels.
    /// In the mixer: right click the Volume of a group > Expose, the rename the parameter.
    /// </summary>
    [CreateAssetMenu(menuName = "Mirador Works/Settings/Audio/Mixer Parameter Channel", fileName = "MixerParameterChannel")]
    public sealed class MixerParameterChannel : AudioChannel
    {
        [SerializeField] AudioMixer mixer;
        [Tooltip("Name of the exposed parameter, e.g. MusicVolume")]
        [SerializeField] string exposedParameter = "MasterVolume";

        public AudioMixer Mixer => mixer;
        public string ExposedParameter => exposedParameter;

        public override bool IsAvailable => mixer != null && !string.IsNullOrEmpty(exposedParameter);

        protected override void ApplyVolume(float linear01)
        {
            if (!IsAvailable)
                return;

            if (!mixer.SetFloat(exposedParameter, AudioVolume.ToDecibels(linear01)))
                Debug.LogWarning($"[Settings] Audio Mixer '{mixer.name}' has no exposed parameter '{exposedParameter}'.", this);
        }

        // ClearFloat hands the parameter back to the mixer's current snapshot.
        protected override void RestoreVolume()
        {
            if (IsAvailable)
                mixer.ClearFloat(exposedParameter);
        }
    }
}