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