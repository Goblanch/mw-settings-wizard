using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.PlayMode
{
    /// <summary>
    /// Creates settings and profiles for PlayMode tests. SerializedObject is editor-only,
    /// so private serialized fields are set with JsonUtility.FromJsonOverwrite, which works at runtime.
    /// </summary>
    internal sealed class PlayModeAssets
    {
        readonly List<Object> created = new List<Object>();

        public T Setting<T>(string id, ApplyMode applyMode = ApplyMode.Immediate, bool requiresConfirmation = false)
            where T : SettingDefinition
        {
            var setting = ScriptableObject.CreateInstance<T>();
            setting.name = id;

            string json =
                $"{{\"id\":\"{id}\",\"applyMode\":{(int)applyMode},\"requiresConfirmation\":{(requiresConfirmation ? "true" : "false")}}}";
            JsonUtility.FromJsonOverwrite(json, setting);

            created.Add(setting);
            return setting;
        }

        public SettingsProfile Profile(params SettingDefinition[] settings)
        {
            SettingsProfile profile = SettingsProfile.Create(settings);
            created.Add(profile);
            return profile;
        }

        public void DestroyAll()
        {
            foreach (Object obj in created)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }

            created.Clear();
        }
    }
}