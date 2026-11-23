using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// "Keep these changes?" for settings marked RequiresConfirmation.
    /// Show it in your UI and call Confirm or Cancel. If neither happens before timeout, 
    /// the settings are reverted automatically. The timer uses real time, so it also runs with timeScale = 0.
    /// </summary>
    public sealed class ConfirmationRequest
    {
        readonly SettingsService service;
        readonly SettingSlot[] slots;
        readonly SettingDefinition[] settings;
        readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        double deadline;
        bool timerStarted;

        internal ConfirmationRequest(SettingsService service, SettingSlot[] slots, float timeoutSeconds)
        {
            this.service = service;
            this.slots = slots;
            TimeOutSeconds = timeoutSeconds;

            settings = new SettingDefinition[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                settings[i] = slots[i].Definition;
        }

        /// <summary>Raised once: true if confirmated, false if cancelled, timed out or replaced by a newer request./// </summary>
        public event Action<bool> Completed;

        public IReadOnlyList<SettingDefinition> Settings => settings;
        public float TimeOutSeconds { get; }
        public bool IsCompleted { get; private set; }
        public bool IsConfirmed { get; private set; }

        public float RemainingSeconds
        {
            get
            {
                if (IsCompleted)
                    return 0f;
                if (!timerStarted)
                    return TimeOutSeconds;
                return Mathf.Max(0f, (float)(deadline - Now));
            }
        }

        internal IReadOnlyList<SettingSlot> Slots => slots;

        static double Now => Time.realtimeSinceStartupAsDouble;

        public void Confirm() => service.CompleteConfirmation(this, confirmed: true);

        public void Cancel() => service.CompleteConfirmation(this, confirmed: false);

        internal void MarkCompleted(bool confirmed)
        {
            if (IsCompleted)
                return;

            IsCompleted = true;
            IsConfirmed = confirmed;
            cancellation.Cancel();

            Action<bool> handlers = Completed;
            Completed = null;
            if (handlers == null)
                return;

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<bool>)handler)(confirmed);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiradorWorks.Settings] A Completed handler threw.\n{e}");
                }
            }
        }

        // async void on purpose: nobody awaits the timer. Exceptions other than cancellation reach Unity's log.
        internal async void StartTimer()
        {
            if (IsCompleted)
                return;

            CancellationToken token = cancellation.Token;
            try
            {
                // Screen.SetResolution takes effect at the end of the frame: start counting from the next one.
                await Awaitable.NextFrameAsync(token);

                deadline = Now + TimeOutSeconds;
                timerStarted = true;

                while (Now < deadline)
                    await Awaitable.NextFrameAsync(token);

                service.CompleteConfirmation(this, confirmed: false);
            }
            catch (OperationCanceledException)
            {
                // Confirmed, cancelled or disposed before timeout.
            }
        }
    }
}