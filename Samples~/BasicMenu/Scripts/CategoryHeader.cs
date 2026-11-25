using TMPro;
using UnityEngine;

namespace MiradorWorks.Settings.Samples
{
    public sealed class CategoryHeader : MonoBehaviour
    {
        [SerializeField] TMP_Text title;

        readonly LocalizedLabelBinding binding = new LocalizedLabelBinding();

        public void Bind(SettingCategory category) =>
            binding.Bind(category.Label, Capitalize(category.Id ?? category.name), title);

        void OnDestroy() => binding.Dispose();

        static string Capitalize(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}