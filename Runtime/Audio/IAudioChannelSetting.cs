namespace MiradorWorks.Settings
{
    /// <summary>
    /// A setting that drives an audio chanel. Used by the editor checks.
    /// </summary>
    internal interface IAudioChannelSetting
    {
        AudioChannel Channel { get; }
    }
}