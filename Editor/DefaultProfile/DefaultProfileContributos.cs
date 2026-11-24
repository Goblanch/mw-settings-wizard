namespace MiradorWorks.Settings.Editor
{
    /// <summary>
    /// Adds settings and preset values to the profole created by "Create > Mirador Works > Settings > DefaultProfile".
    /// Subclasses are found automatically: they need a parameterless constructor. Use one for your own settings.
    /// </summary>
    public abstract class DefaultProfileContributor
    {
        /// <summary>
        /// Lower runs first. The built-in settings use -100.
        /// </summary>
        public virtual int Order => 0;

        public abstract void Contribute(DefaultProfileBuilder builder);
    }
}