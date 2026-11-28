using System;
using System.Collections.Generic;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Volume and mute of every channel, shared by all audio settings of one settings service.
    /// It lives in the service (GetOrAddService), not in the channel asset, so nothing survives between
    /// Play sessions when Domain Reload is disabled. Disposed with the service: channels are restored.
    /// </summary>
    internal sealed class AudioChannelStates : IDisposable
    {
        struct State
        {
            public float Volume;
            public bool Muted;
        }

        readonly Dictionary<AudioChannel, State> states = new Dictionary<AudioChannel, State>();

        public static AudioChannelStates Get(SettingsContext context) =>
            context.GetOrAddService(() => new AudioChannelStates());

        public void SetVolume(AudioChannel channel, float volume)
        {
            State state = GetState(channel);
            state.Volume = volume;
            Store(channel, state);
        }

        public void SetMuted(AudioChannel channel, bool muted)
        {
            State state = GetState(channel);
            state.Muted = muted;
            Store(channel, state);
        }

        State GetState(AudioChannel channel) =>
            states.TryGetValue(channel, out State state) ? state : new State { Volume = 1f };

        void Store(AudioChannel channel, State state)
        {
            // State is a struct: GetState returned a copy, so it has to be written back.
            states[channel] = state;
            channel.Apply(state.Muted ? 0f : state.Volume);
        }

        public void Dispose()
        {
            foreach (AudioChannel channel in states.Keys)
            {
                if (channel != null)
                    channel.Restore();
            }

            states.Clear();
        }
    }
}