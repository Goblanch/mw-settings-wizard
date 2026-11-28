using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiradorWorks.Settings.Tests.EditMode
{
    /// <summary>Creates ScriptableObjects for tests and destroys them all in TearDown.</summary>
    internal sealed class TestAssets
    {
        readonly List<Object> created = new List<Object>();

        public T Setting<T>(
            string id,
            string name = null,
            ApplyMode applyMode = ApplyMode.Immediate,
            SettingCategory category = null) where T : SettingDefinition
        {
            var setting = ScriptableObject.CreateInstance<T>();
            setting.name = name ?? id ?? typeof(T).Name;

            Configure(setting, so =>
            {
                so.FindProperty("id").stringValue = id;
                so.FindProperty("applyMode").enumValueIndex = (int)applyMode;
                so.FindProperty("category").objectReferenceValue = category;
            });

            created.Add(setting);
            return setting;
        }

        // [T4.1] NEW
        /// <summary>Any other ScriptableObject a test needs (e.g. an audio channel).</summary>
        public T Asset<T>(string name = null) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name ?? typeof(T).Name;

            created.Add(asset);
            return asset;
        }
        // [T4.1] END

        public SettingCategory Category(string id)
        {
            var category = ScriptableObject.CreateInstance<SettingCategory>();
            category.name = id;
            Configure(category, so => so.FindProperty("id").stringValue = id);

            created.Add(category);
            return category;
        }

        public SettingsProfile Profile(params SettingDefinition[] settings)
        {
            var profile = ScriptableObject.CreateInstance<SettingsProfile>();
            profile.name = "TestProfile";

            Configure(profile, so =>
            {
                SerializedProperty list = so.FindProperty("settings");
                list.arraySize = settings.Length;
                for (int i = 0; i < settings.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = settings[i];
            });

            created.Add(profile);
            return profile;
        }

        public SettingsPreset Preset(string key, params (SettingDefinition setting, string value)[] entries)
        {
            var list = new List<SettingsPreset.Entry>();
            foreach ((SettingDefinition setting, string value) in entries)
                list.Add(new SettingsPreset.Entry(setting, value));

            SettingsPreset preset = SettingsPreset.Create(key, list);
            created.Add(preset);
            return preset;
        }

        public SettingsProfile ProfileWithPresets(SettingDefinition[] settings, params SettingsPreset[] presets)
        {
            SettingsProfile profile = SettingsProfile.Create(settings, presets);
            profile.name = "TestProfile";
            created.Add(profile);
            return profile;
        }

        /// <summary>Edits serialized fields the way the inspector does.</summary>
        public void Configure(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void DestroyAll()
        {
            foreach (Object obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            created.Clear();
        }
    }
}