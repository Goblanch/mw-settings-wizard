using System;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Context for a single apply pass. 
    /// </summary>
    public sealed class SettingsApplyContext
    {
        internal SettingsApplyContext(SettingsContext settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public SettingsContext Settings { get; }
    }
}