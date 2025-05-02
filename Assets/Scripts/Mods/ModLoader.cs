using System;
using System.IO;
using System.Linq;
using System.Reflection;
namespace DLS.Mods
{    
    public static class ModLoader
    {
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
        public static void LoadMods(string modsDirectory)
        {
            UnityEngine.Debug.Log("Loading mods...");
            foreach (string dllPath in Directory.GetFiles(modsDirectory, "*.dls"))
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

                        string name = (string)nameProperty.GetValue(modInstance);
                        string version = (string)versionProperty.GetValue(modInstance);
                        initializeMethod.Invoke(modInstance, null);

                        UnityEngine.Debug.Log($"Loaded {name}, v{version}");
                    }
                }
            }
        }
    }
}