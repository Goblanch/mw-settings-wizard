using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiradorWorks.Settings.Tests.EditMode
{
    /// <summary>
    /// Creates ScriptableObjects for tests and destroys them all in TearDown.
    /// </summary>
    internal sealed class TestAssets
    {
        readonly List<Object> created = new List<Object>();

        public T Setting<T>(string id, string name = null) where T : SettingDefinition
        {
            var setting = ScriptableObject.CreateInstance<T>();
            setting.name = name ?? id ?? typeof(T).Name;

            var so = new SerializedObject(setting);
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();

            created.Add(setting);
            return setting;
        }

        public SettingsProfile Profile(params SettingDefinition[] settings)
        {
            var profile = ScriptableObject.CreateInstance<SettingsProfile>();
            profile.name = "TestProfile";

            var so = new SerializedObject(profile);
            SerializedProperty list = so.FindProperty("settings");
            list.arraySize = settings.Length;
            for (int i = 0; i < settings.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = settings[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            created.Add(profile);
            return profile;
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