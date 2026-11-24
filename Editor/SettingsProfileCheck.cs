using System.Collections.Generic;

namespace MiradorWorks.Settings.Editor
{
    /// <summary>
    /// Extra checks shown as warnings in the SettingsProfile inspector, for problems Validate() cannot see
    /// (e.g. project settings). Subclasses are found automatically: they need a parameterless constructor.
    /// </summary>
    public abstract class SettingsProfileCheck
    {
        public abstract void Collect(SettingsProfile profile, List<string> warnings);
    }
}