using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiradorWorks.Settings
{
    /// <summary>
    /// What a definition can reach while it runs: the values of other settings of the same service
    /// and shared services that live as long as the setting service.
    /// </summary>
    public sealed class SettingsContext
    {
        readonly SettingsService service;
        readonly List<object> services = new List<object>();
        readonly Dictionary<Type, object> servicesByType = new Dictionary<Type, object>();

        // Standalone context (tests): it knows no settings.
        internal SettingsContext() { }

        internal SettingsContext(SettingsService service)
        {
            this.service = service;
        }

        /// <summary>Profile of trhe settings service, or null for a standalone context./// </summary>
        public SettingsProfile Profile => service?.Profile;

        /// <summary>
        /// Current value of another setting (pending id there is none).
        /// Returns false if the setting is not in the profile or has not been loaded yet:
        /// during Initialize, settings load in profile order.
        /// </summary>
        public bool TryGetValue<T>(SettingDefinition<T> setting, out T value) => TryGet(setting, current: true, out value);

        /// <summary>
        /// Applied value of another setting. Same rules as TryGetValue.
        /// </summary>
        public bool TryGetApplied<T>(SettingDefinition<T> setting, out T value) => TryGet(setting, current: false, out value);


        public bool TryGetValue<T>(string id, out T value)
        {
            if (service != null && service.TryGetLoadedSlot(id, out SettingSlot slot) && slot is SettingSlot<T> typed)
            {
                value = typed.Current;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns the shared service of that type, creating it with the factory first time.
        /// Services that implement IDisposable are disposed with the settings service, newest first.
        /// </summary>
        public TService GetOrAddService<TService>(Func<TService> factory) where TService : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (servicesByType.TryGetValue(typeof(TService), out object existing))
                return (TService)existing;

            TService created = factory()
                ?? throw new InvalidOperationException($"The factory for {typeof(TService).Name} returned null.");

            servicesByType.Add(typeof(TService), created);
            services.Add(created);
            return created;
        }

        internal void DisposeServices()
        {
            for (int i = services.Count - 1; i >= 0; i--)
            {
                if (services[i] is not IDisposable disposable)
                    continue;

                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MiradorWorks.Settings] Disposing {services[i].GetType().Name} failed.\n{e}");
                }
            }

            services.Clear();
            servicesByType.Clear();
        }

        /// <summary>
        /// Current value by id. Returns false if the id is unknown, not loaded yet or of another type.
        /// </summary>
        bool TryGet<T>(SettingDefinition<T> setting, bool current, out T value)
        {
            if (service != null && service.TryGetLoadedSlot(setting, out SettingSlot slot) && slot is SettingSlot<T> typed)
            {
                value = current ? typed.Current : typed.Applied;
                return true;
            }

            value = default;
            return false;
        }
    }
}