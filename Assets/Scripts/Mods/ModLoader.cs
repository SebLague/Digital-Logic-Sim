using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DLS.ModdingAPI;
using UnityEngine;

namespace DLS.Mods
{
    public static class ModLoader
    {
        public static readonly List<IMod> loadedMods = new();

        public static void NotifyMods<T>(Action<IMod, T> action, T arg)
        {
            foreach (IMod mod in loadedMods)
            {
                action(mod, arg);
            }
        }

        public static void NotifyMods(Action<IMod> action)
        {
            foreach (IMod mod in loadedMods)
            {
                action(mod);
            }
        }

        public static void InitializeMods(string modsDirectory)
        {
            Debug.Log("Loading mods...");
            foreach (string dllPath in Directory.GetFiles(modsDirectory, "*.dls"))
            {
                try
                {
                    Assembly modAssembly = Assembly.LoadFrom(dllPath);

                    foreach (Type type in modAssembly.GetTypes().Where(t => typeof(IMod).IsAssignableFrom(t) && !t.IsAbstract))
                    {
                        IMod modInstance = (IMod) Activator.CreateInstance(type);
                        loadedMods.Add(modInstance);
                        modInstance.Initialize();
                        Debug.Log($"Loaded mod: {modInstance.Name} v{modInstance.Version}");
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (Exception inner in ex.LoaderExceptions)
                    {
                        Debug.LogError(inner.Message);
                    }
                }
                catch (FileLoadException ex)
                {
                    Debug.LogError(ex.Message);
                }
            }
        }

        public static bool IsModLoaded(string modId)
        {
            foreach (IMod mod in loadedMods)
            {
                if (mod.ModID == modId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}