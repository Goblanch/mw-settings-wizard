using System;
using TMPro;
using UnityEngine.Localization;

namespace MiradorWorks.Settings.Samples
{
    /// <summary>
    /// Keeps a text in sync with a LocalizedString (it updates when the language changes).
    /// If the string has no table entry, it shows a fallback instead.
    /// </summary>
    internal sealed class LocalizedLabelBinding : IDisposable
    {
        LocalizedString source;
        TMP_Text target;

        public void Bind(LocalizedString text, string fallback, TMP_Text label)
        {
            Dispose();
            target = label;
            if (target == null)
                return;

            if (text == null || text.IsEmpty)
            {
                target.text = fallback;
                return;
            }

            source = text;
            source.StringChanged += OnStringChanged;
        }

        public void Dispose()
        {
            if (source != null)
                source.StringChanged -= OnStringChanged;

            source = null;
        }

        void OnStringChanged(string value)
        {
            if (target != null)
                target.text = value;
        }
    }
}