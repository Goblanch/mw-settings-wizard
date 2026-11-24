using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiradorWorks.Settings.Editor
{
    /// <summary>Creates a ready-to-use profile with every available setting, its categories and presets.</summary>
    public static class DefaultProfileFactory
    {
        /// <summary>Creates every asset inside <paramref name="folder"/>, which must exist in the project.</summary>
        public static SettingsProfile Create(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                throw new ArgumentException($"'{folder}' is not a folder in the project.", nameof(folder));

            var builder = new DefaultProfileBuilder(folder);

            foreach (DefaultProfileContributor contributor in FindContributors())
            {
                try
                {
                    contributor.Contribute(builder);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiradorWorks.Settings] {contributor.GetType().Name} failed while creating the default profile.\n{e}");
                }
            }

            SettingsProfile profile = builder.Build();
            AssetDatabase.SaveAssets();
            return profile;
        }

        static List<DefaultProfileContributor> FindContributors()
        {
            var contributors = new List<DefaultProfileContributor>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<DefaultProfileContributor>())
            {
                if (type.IsAbstract)
                    continue;

                try
                {
                    contributors.Add((DefaultProfileContributor)Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiradorWorks.Settings] Could not create {type.Name}: {e.Message}");
                }
            }

            // Order first, then type name, so the result is the same on every machine.
            contributors.Sort((a, b) =>
            {
                int byOrder = a.Order.CompareTo(b.Order);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.GetType().FullName, b.GetType().FullName);
            });

            return contributors;
        }
    }
}