using UnityEngine;

namespace MiradorWorks.Settings.Samples
{
    /// <summary>
    /// Example composition root: creates the settings service, initializes it and gives it to the menu.
    /// In your game this usually lives in your bootstrap scene or entry point.
    /// </summary>
    public sealed class BasicMenuDemo : MonoBehaviour
    {
        [SerializeField] SettingsProfile profile;
        [SerializeField] SettingsMenu menu;

        SettingsService settings;

        void Awake()
        {
            if (profile == null)
            {
                Debug.LogError("[BasicMenuDemo] Assign a Settings Profile (Create > Mirador Works > Settings > Default Profile).", this);
                enabled = false;
                return;
            }

            settings = new SettingsService(profile, new JsonFileSettingsStorage());
            settings.Initialize();
        }

        // Start, not Awake: the menu's own Awake must have run before binding.
        void Start()
        {
            if (settings != null && menu != null)
                menu.Bind(settings);
        }

        void OnDestroy() => settings?.Dispose();
    }
}