using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiradorWorks.Settings.Editor
{
    /// <summary>
    /// Creates the assets of a default profile inside one folder. Contributos add settings and preset values;
    /// the profile, the preset selector and the presets are created at the end.
    /// </summary>
    public sealed class DefaultProfileBuilder
    {
        public const string DisplayCategory = "display";
        public const string GraphicsCategory = "graphics";
        public const string AudioCategory = "audio";
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";

        static readonly string[] StandardPresets = { Low, Medium, High };

        readonly List<SettingDefinition> settings = new List<SettingDefinition>();
        readonly Dictionary<string, SettingCategory> categories = new Dictionary<string, SettingCategory>(StringComparer.Ordinal);
        readonly Dictionary<string, List<SettingsPreset.Entry>> presetEntries =
            new Dictionary<string, List<SettingsPreset.Entry>>(StringComparer.Ordinal);
        readonly List<string> presetOrder = new List<string>();

        internal DefaultProfileBuilder(string folder)
        {
            Folder = folder;
        }

        /// <summary>
        /// Project folder where every asset is created.
        /// </summary>
        public string Folder { get; }

        /// <summary>Returns the category with that id, creating its asset the first time.</summary>
        public SettingCategory Category(string id, int order)
        {
            if (categories.TryGetValue(id, out SettingCategory existing))
                return existing;

            var category = ScriptableObject.CreateInstance<SettingCategory>();
            Edit(category, so =>
            {
                so.FindProperty("id").stringValue = id;
                so.FindProperty("order").intValue = order;
            });
            Save(category, "Category " + ObjectNames.NicifyVariableName(id));

            categories.Add(id, category);
            return category;
        }

        /// <summary>Creates a setting asset. <paramref name="configure"/> edits its other serialized fields.</summary>
        public T AddSetting<T>(
            string id,
            SettingCategory category,
            ApplyMode applyMode = ApplyMode.Immediate,
            bool requiresConfirmation = false,
            Action<SerializedObject> configure = null) where T : SettingDefinition
        {
            var setting = ScriptableObject.CreateInstance<T>();
            Edit(setting, so =>
            {
                so.FindProperty("id").stringValue = id;
                so.FindProperty("category").objectReferenceValue = category;
                so.FindProperty("applyMode").enumValueIndex = (int)applyMode;
                so.FindProperty("requiresConfirmation").boolValue = requiresConfirmation;
                configure?.Invoke(so);
            });
            Save(setting, ObjectNames.NicifyVariableName(typeof(T).Name));

            settings.Add(setting);
            return setting;
        }

        public T CreateAsset<T>(string fileName, Action<SerializedObject> configure = null) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            if (configure != null)
                Edit(asset, configure);
            Save(asset, fileName);
            return asset;
        }

        /// <summary>Adds a value to a preset. Presets are created only if they get at least one value.</summary>
        public void AddPresetValue(string presetKey, SettingDefinition setting, string value)
        {
            if (!presetEntries.TryGetValue(presetKey, out List<SettingsPreset.Entry> entries))
            {
                entries = new List<SettingsPreset.Entry>();
                presetEntries.Add(presetKey, entries);
                presetOrder.Add(presetKey);
            }

            entries.Add(new SettingsPreset.Entry(setting, value));
        }

        internal SettingsProfile Build()
        {
            var presets = new List<SettingsPreset>();

            if (presetEntries.Count > 0)
            {
                foreach (string key in OrderedPresetKeys())
                {
                    SettingsPreset preset = SettingsPreset.Create(key, presetEntries[key]);
                    Save(preset, "Preset " + ObjectNames.NicifyVariableName(key));
                    presets.Add(preset);
                }

                SettingCategory graphics = Category(GraphicsCategory, 1);
                var selector = AddSetting<PresetSetting>("graphics.preset", graphics);

                // Show the selector before the graphics settings it controls.
                settings.Remove(selector);
                int firstGraphics = settings.FindIndex(s => s.Category == graphics);
                settings.Insert(firstGraphics >= 0 ? firstGraphics : settings.Count, selector);
            }

            SettingsProfile profile = SettingsProfile.Create(settings, presets);
            Save(profile, "Settings Profile");
            return profile;
        }

        IEnumerable<string> OrderedPresetKeys()
        {
            foreach (string key in StandardPresets)
            {
                if (presetEntries.ContainsKey(key))
                    yield return key;
            }

            foreach (string key in presetOrder)
            {
                if (Array.IndexOf(StandardPresets, key) < 0)
                    yield return key;
            }
        }

        void Save(Object asset, string fileName) =>
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath($"{Folder}/{fileName}.asset"));

        static void Edit(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}