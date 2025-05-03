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
        static readonly List<LoadedMod> loadedMods = new();
        static ModLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("DLSModdingAPI"))
            {
                return Assembly.GetExecutingAssembly();
            }

            return null; // Let the default resolver handle other assemblies
        }
        public static void InitializeMods(string modsDirectory)
        {
            UnityEngine.Debug.Log("Loading mods...");
            foreach (string dllPath in Directory.GetFiles(modsDirectory, "*.dls"))
            {
                Assembly modAssembly = Assembly.LoadFrom(dllPath);
                try
                {
                    foreach (Type type in modAssembly.GetTypes().Where(t => !t.IsAbstract))
                    {
                        // Check if the type has the same structure as IMod
                        var nameProperty = type.GetProperty("Name");
                        var versionProperty = type.GetProperty("Version");
                        var initializeMethod = type.GetMethod("Initialize");
                        var onProjectLoadMethod = type.GetMethod("OnProjectLoad");

                        if (nameProperty != null && versionProperty != null && initializeMethod != null && onProjectLoadMethod != null)
                        {
                            // Ugly, but ModLoader.IMod would not be the same as ModdingAPI.IMod
                            object modInstance = Activator.CreateInstance(type);

                            string name = (string) nameProperty.GetValue(modInstance);
                            string version = (string) versionProperty.GetValue(modInstance);
                            LoadedMod mod = new(name, version, modInstance, initializeMethod, onProjectLoadMethod);
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
            }
        }

        public static void InvokeModsOnProjectLoad()
        {
            foreach (LoadedMod mod in loadedMods)
            {
                mod.OnProjectLoad();
            }
        }
    }
}