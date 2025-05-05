using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DLS.Game;
namespace DLS.Mods
{    
    public static class ModLoader
    {
        public static readonly List<LoadedMod> loadedMods = new();
        public static void InitializeMods(string modsDirectory)
        {
            UnityEngine.Debug.Log("Loading mods...");
            foreach (string dllPath in Directory.GetFiles(modsDirectory, "*.dls"))
            {
                try
                {
                    Assembly modAssembly = Assembly.LoadFrom(dllPath);
                
                    foreach (Type type in modAssembly.GetTypes().Where(t => !t.IsAbstract))
                    {
                        // Check if the type has the same structure as IMod
                        var nameProperty = type.GetProperty("Name");
                        var versionProperty = type.GetProperty("Version");
                        var initializeMethod = type.GetMethod("Initialize");

                        if (nameProperty != null && versionProperty != null && initializeMethod != null)
                        {
                            // Ugly, but ModLoader.IMod would not be the same as ModdingAPI.IMod
                            object modInstance = Activator.CreateInstance(type);

                            string name = (string) nameProperty.GetValue(modInstance);
                            string version = (string) versionProperty.GetValue(modInstance);
                            LoadedMod mod = new(name, version, modInstance, initializeMethod);
                            loadedMods.Add(mod);
                            mod.Initialize();
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach(Exception inner in ex.LoaderExceptions)
                    {
                        UnityEngine.Debug.LogError(inner.Message);
                    }
                }
                catch (FileLoadException ex)
                {
                    UnityEngine.Debug.LogError(ex.Message);
                }
            }
        }

    }
}