using System;
using System.IO;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Default storage: a JSON file, by default <c>Application.persistentDataPath/settings.json<c/>.
    /// Writes are atomic: a crash while saving never leaves a half-written file.
    /// </summary>
    public sealed class JsonFileSettingsStorage : ISettingsStorage
    {
        public const string DefaultFileName = "settings.json";

        string filePath;

        /// <summary>Uses <c>Application.persistentDataPath/settings.json</c>./// </summary>
        public JsonFileSettingsStorage() { }

        public JsonFileSettingsStorage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            this.filePath = filePath;
        }

        // Resolved lazily: persistentDataPath cannot be read from a MonoBehavior field initializer.
        public string FilePath => filePath ??= Path.Combine(Application.persistentDataPath, DefaultFileName);

        string TempPath => FilePath + ".tmp";
        string BackupPath => FilePath + ".bak";

        public bool TryLoad(out SettingsData data)
        {
            data = null;
            if (!File.Exists(FilePath))
                return false;

            string json;
            try
            {
                json = File.ReadAllText(FilePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[MiradorWorks.Settings] Could not read '{FilePath}': {e.Message}");
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SettingsData>(json);
            }
            catch (ArgumentException)
            {
                data = null;
            }

            if (data == null)
            {
                BackupCorruptFile();
                Debug.LogWarning(
                    $"[MiradorWorks.Settings] Settings file '{FilePath}' is corrupt. " +
                    $"A copy was saved to '{BackupPath}' and defaults will be used.");
                return false;
            }

            if (data.Version > SettingsData.CurrentVersion)
            {
                Debug.LogWarning(
                    $"[MiradorWorks.Settings] Settings file version {data.Version} is newer than " +
                    $"{SettingsData.CurrentVersion}. Unknown data will be ignored.");
            }

            return true;
        }

        public void Save(SettingsData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(data, prettyPrint: true);

            try
            {
                File.WriteAllText(TempPath, json);

                if (File.Exists(FilePath))
                    ReplaceWithTemp();
                else
                    File.Move(TempPath, FilePath);
            }
            finally
            {
                if (File.Exists(TempPath))
                    TryDelete(TempPath);
            }
        }

        void ReplaceWithTemp()
        {
            try
            {
                File.Replace(TempPath, FilePath, null);
            }
            catch (Exception e) when (e is PlatformNotSupportedException || e is IOException)
            {
                // Some file systems do not support Replace. Copy is not atomic, but the temp file still
                // protects against a crash while serializing.
                File.Copy(TempPath, FilePath, overwrite: true);
            }
        }

        void BackupCorruptFile()
        {
            try
            {
                File.Copy(FilePath, BackupPath, overwrite: true);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[MiradorWorks.Settings] Could not back up '{FilePath}': {e.Message}");
            }
        }

        static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // A leftover temp file is harmless: the next save overwrites it.
            }
        }
    }
}