namespace MiradorWorks.Settings
{
    /// <summary>When a changed value is applied to the engine./// </summary>
    public enum ApplyMode
    {
        /// <summary>Applied as soon as the value changes./// </summary>
        Immediate,
        /// <summary>Kept as pending until the service applies pending changes./// </summary>
        Deferred
    }
}