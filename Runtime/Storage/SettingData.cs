using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// Saved settings values: setting id -> serialized.value.
    /// This is what every storage reads and writes.
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentVersion = 1;

        [Serializable]
        public struct Entry
        {
            [SerializeField] string id;
            [SerializeField] string value;

            public Entry(string id, string value)
            {
                this.id = id;
                this.value = value;
            }

            public string Id => id;
            public string Value => value;
        }

        [SerializeField] int version = CurrentVersion;
        [SerializeField] List<Entry> entries = new List<Entry>();

        public int Version => version;
        public IReadOnlyList<Entry> Entries => EntriesList;
        public int Count => EntriesList.Count;

        // JsonUtility can leave the list null when the field is missing from the file.
        List<Entry> EntriesList => entries ??= new List<Entry>();

        public bool TryGetValue(string id, out string value)
        {
            int index = IndexOf(id);
            if (index >= 0)
            {
                value = EntriesList[index].Value;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>Adds the entry or replaces its value./// </summary>
        public void Set(string id, string value)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Setting id cannot be null or empty.", nameof(id));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var entry = new Entry(id, value);
            int index = IndexOf(id);
            if (index >= 0)
                EntriesList[index] = entry;
            else
                EntriesList.Add(entry);
        }

        public bool Remove(string id)
        {
            int index = IndexOf(id);
            if (index < 0)
                return false;

            EntriesList.RemoveAt(index);
            return true;
        }

        public SettingsData Clone()
        {
            var copy = new SettingsData { version = version };
            copy.entries = new List<Entry>(EntriesList);
            return copy;
        }

        /// <summary>
        /// Cleans data that came from outside (a hand-edited file, another save system):
        /// drops entries with an empty id or a null value, keeps the last of duplicated ids, 
        /// and fixes missing version.
        /// </summary>
        internal void Normalize()
        {
            if (version <= 0)
                version = CurrentVersion;

            List<Entry> list = EntriesList;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Entry entry = list[i];
                if (string.IsNullOrEmpty(entry.Id) || entry.Value == null || LastIndexOf(entry.Id) != i)
                    list.RemoveAt(i);
            }
        }

        int IndexOf(string id)
        {
            if (id == null)
                return -1;

            List<Entry> list = EntriesList;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Id, id, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        int LastIndexOf(string id)
        {
            List<Entry> list = EntriesList;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (string.Equals(list[i].Id, id, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }
}