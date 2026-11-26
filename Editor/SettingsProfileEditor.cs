using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiradorWorks.Settings.Editor
{
    /// <summary>
    /// Shows the profile's validation errors and the warnings of every SettingsProfileCheck.
    /// </summary>
    [CustomEditor(typeof(SettingsProfile))]
    internal sealed class SettingsProfileEditor : UnityEditor.Editor
    {
        readonly List<SettingsProfileCheck> checks = new List<SettingsProfileCheck>();
        readonly List<string> warnings = new List<string>();

        void OnEnable()
        {
            checks.Clear();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<SettingsProfileCheck>())
            {
                if (type.IsAbstract) continue;

                try
                {
                    checks.Add((SettingsProfileCheck)Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiradorWorks.Settings] Could not create profile check {type.Name}: {e.Message}");
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var profile = (SettingsProfile)target;
            IReadOnlyList<string> errors = profile.Validate();

            warnings.Clear();
            foreach (SettingsProfileCheck check in checks)
            {
                try
                {
                    check.Collect(profile, warnings);
                }
                catch (Exception e)
                {
                    warnings.Add($"{check.GetType().Name} failed: {e.Message}");
                }
            }

            EditorGUILayout.Space();

            if (errors.Count == 0 && warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("The profile is valid.", MessageType.Info);
                return;
            }

            foreach (string error in errors)
                EditorGUILayout.HelpBox(error, MessageType.Error);

            foreach (string warning in warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }
    }
}