using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiradorWorks.Settings.Editor
{
    /// <summary>
    /// Draws each preset value with the control of its setting: a toggle, a slider or a dropdown.
    /// The value is still stored as the setting serializes it, so nothing changes at runtime.
    /// </summary>
    [CustomPropertyDrawer(typeof(SettingsPreset.Entry))]
    internal sealed class PresentEntryDrawer : PropertyDrawer
    {
        const float HelpBoxHeight = 32f;

        static float Line => EditorGUIUtility.singleLineHeight;
        static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = Line * 2f + Spacing;
            if (HasInvalidValue(property))
                height += Spacing + HelpBoxHeight;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty settingProperty = property.FindPropertyRelative("setting");
            SerializedProperty valueProperty = property.FindPropertyRelative("value");

            var settingRect = new Rect(position.x, position.y, position.width, Line);
            var valueRect = new Rect(position.x, settingRect.yMax + Spacing, position.width, Line);

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.PropertyField(settingRect, settingProperty, new GUIContent("Setting"));
            var setting = settingProperty.objectReferenceValue as SettingDefinition;
            DrawValue(valueRect, setting, valueProperty);

            if (HasInvalidValue(property))
            {
                var helpRect = new Rect(position.x, valueRect.yMax + Spacing, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(helpRect, $"'{valueProperty.stringValue}' is not a valid value for {setting.name}.", MessageType.Error);
            }

            EditorGUI.EndProperty();
        }

        static void DrawValue(Rect rect, SettingDefinition setting, SerializedProperty valueProperty)
        {
            var context = new SettingsContext();
            var valueLabel = new GUIContent("Value");
            string raw = valueProperty.stringValue;

            switch (setting)
            {
                case null:
                    EditorGUI.LabelField(rect, valueLabel, new GUIContent("Pick a setting first"));
                    return;

                case PresetSetting:
                    EditorGUI.LabelField(rect, valueLabel, new GUIContent("The preset selector cannot be part of a preset."));
                    return;

                case ToggleSetting toggle:
                    {
                        bool current = toggle.TryDeserialize(raw, out bool parsed) && parsed;
                        EditorGUI.BeginChangeCheck();
                        bool next = EditorGUI.Toggle(rect, valueLabel, current);
                        if (EditorGUI.EndChangeCheck())
                            valueProperty.stringValue = toggle.Serialize(next);
                        return;
                    }

                case RangeSetting range:
                    {
                        float current = range.TryDeserialize(raw, out float parsed) ? parsed : range.GetDefault(context);
                        EditorGUI.BeginChangeCheck();
                        float next = EditorGUI.Slider(rect, valueLabel, current, range.Min, range.Max);
                        if (EditorGUI.EndChangeCheck())
                            valueProperty.stringValue = range.Serialize(range.Sanitize(next, context));
                        return;
                    }

                case ChoiceSetting choice:
                    {
                        IReadOnlyList<ChoiceOption> options = choice.GetOptions(context);
                        var labels = new GUIContent[options.Count];
                        for (int i = 0; i < options.Count; i++)
                            labels[i] = new GUIContent(options[1].RawLabel ?? options[i].Key);

                        EditorGUI.BeginChangeCheck();
                        int next = EditorGUI.Popup(rect, valueLabel, ChoiceSetting.IndexOf(options, raw), labels);
                        if (EditorGUI.EndChangeCheck() && next >= 0)
                            valueProperty.stringValue = options[next].Key;
                        return;
                    }

                default:
                    // A custom SettingDefinition<T>: edit the serialized text directly.
                    EditorGUI.PropertyField(rect, valueProperty, valueLabel);
                    return;
            }
        }

        static bool HasInvalidValue(SerializedProperty property)
        {
            var setting = property.FindPropertyRelative("setting").objectReferenceValue as SettingDefinition;
            if (setting == null || setting is PresetSetting)
                return false;

            string raw = property.FindPropertyRelative("value").stringValue;

            // A choice key must be one of the options; resolutions depend on this machine's display
            if (setting is ChoiceSetting choice)
                return ChoiceSetting.IndexOf(choice.GetOptions(new SettingsContext()), raw) < 0;

            return !setting.CanDeserialize(raw);
        }
    }
}