using System.IO;
using UnityEditor;

namespace MiradorWorks.Settings.Editor
{
    internal static class DefaultProfileMenu
    {
        [MenuItem("Assets/Create/Mirador Works/Settings/Default Profile", priority = 0)]
        static void CreateDefaultProfile()
        {
            string parent = SelectedFolder();
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{parent}/Settings");
            string folder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(parent, Path.GetFileName(uniquePath)));

            SettingsProfile profile = DefaultProfileFactory.Create(folder);

            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }

        // The folder selected in the Project window, or Assets. Package folders are read-only.
        static string SelectedFolder()
        {
            string path = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets"))
                return "Assets";

            return AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path).Replace('\\', '/');
        }
    }
}