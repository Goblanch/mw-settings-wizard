using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MiradorWorks.Settings.Samples
{
    /// <summary>
    /// Builds the menu from the service's profile: a header per category and a row per supported setting,
    /// chosen by its base type. Override <see cref="PrefabFor"/> to show your own setting types.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] RectTransform content;

        [Header("Prefabs")]
        [SerializeField] CategoryHeader categoryHeaderPrefab;
        [SerializeField] ToggleRow toggleRowPrefab;
        [SerializeField] SliderRow sliderRowPrefab;
        [SerializeField] DropdownRow dropdownRowPrefab;

        [Header("Controls")]
        [SerializeField] ConfirmationDialog confirmationDialog;
        [SerializeField] Button applyButton;
        [SerializeField] Button revertButton;
        [SerializeField] Button resetButton;

        readonly List<GameObject> spawned = new List<GameObject>();
        SettingsService service;

        protected virtual void Awake()
        {
            // Explicit null checks: an unassigned serialized field can be a Unity "fake null", which ?. does not catch.
            if (applyButton != null) applyButton.onClick.AddListener(() => service?.ApplyPending());
            if (revertButton != null) revertButton.onClick.AddListener(() => service?.RevertPending());
            if (resetButton != null) resetButton.onClick.AddListener(() => service?.ResetToDefaults());
        }

        public void Bind(SettingsService settingsService)
        {
            Unbind();
            service = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            BuildRows();

            service.ValueChanged += OnValueChanged;
            service.ConfirmationRequested += OnConfirmationRequested;
            UpdateButtons();
        }

        /// <summary>Row prefab for a setting, by its base type. Return null to hide it.</summary>
        protected virtual SettingRow PrefabFor(SettingDefinition setting) => setting switch
        {
            ToggleSetting => toggleRowPrefab,
            RangeSetting => sliderRowPrefab,
            ChoiceSetting => dropdownRowPrefab,
            _ => null
        };

        protected virtual void OnDestroy() => Unbind();

        void BuildRows()
        {
            bool first = true;
            SettingCategory currentCategory = null;

            foreach (SettingDefinition setting in OrderedSettings())
            {
                if (!service.IsSupported(setting))
                    continue;

                SettingRow prefab = PrefabFor(setting);
                if (prefab == null)
                {
                    Debug.LogWarning($"[SettingsMenu] No row for {setting.GetType().Name} '{setting.name}'. Override PrefabFor to show it.");
                    continue;
                }

                if (first || setting.Category != currentCategory)
                {
                    first = false;
                    currentCategory = setting.Category;
                    if (currentCategory != null && categoryHeaderPrefab != null)
                    {
                        CategoryHeader header = Instantiate(categoryHeaderPrefab, content);
                        header.Bind(currentCategory);
                        spawned.Add(header.gameObject);
                    }
                }

                SettingRow row = Instantiate(prefab, content);
                row.Bind(service, setting);
                spawned.Add(row.gameObject);
            }
        }

        // Categories by their order; settings keep the profile's order inside each one (OrderBy is stable).
        IEnumerable<SettingDefinition> OrderedSettings() =>
            service.Profile.Settings
                .Where(setting => setting != null)
                .OrderBy(setting => setting.Category != null ? setting.Category.Order : int.MaxValue);

        void OnValueChanged(SettingDefinition setting) => UpdateButtons();

        void OnConfirmationRequested(ConfirmationRequest request)
        {
            if (confirmationDialog != null)
                confirmationDialog.Show(request);
        }

        void UpdateButtons()
        {
            bool pending = service != null && service.HasPendingChanges;
            if (applyButton != null) applyButton.interactable = pending;
            if (revertButton != null) revertButton.interactable = pending;
        }

        void Unbind()
        {
            if (service != null)
            {
                service.ValueChanged -= OnValueChanged;
                service.ConfirmationRequested -= OnConfirmationRequested;
            }

            foreach (GameObject obj in spawned)
            {
                if (obj != null)
                    Destroy(obj);
            }

            spawned.Clear();
            service = null;
        }
    }
}