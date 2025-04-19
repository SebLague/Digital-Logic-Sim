using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DLS.Description.Types;
using DLS.SaveSystem;
using Game.ModLoader.Types;
using UnityEngine;

namespace Game.ModLoader
{
  public class ModLoader
  {
    public static ModDescription[] activeModDescriptions;
    private static List<Mod> activeMods = new List<Mod>();

    public static void Load()
    {
      foreach (var mod in activeMods)
      {
        mod.OnUnload();
        Debug.Log("Unloading mod: " + mod.GetType());
      }
      activeMods.Clear();
      
      List<String> modAssemblies = new List<string>(); 
      foreach (var mod in activeModDescriptions)
      {
        string path = Path.Combine(SavePaths.ModDirectory, mod.ModName);
        Debug.Log(path);
        if (Directory.Exists(Path.Combine(SavePaths.ModDirectory, mod.ModName)))
        {
          modAssemblies.AddRange(
            Directory.GetFiles(
              Path.Combine(SavePaths.ModDirectory, mod.ModName),
              "*.dll",
              SearchOption.TopDirectoryOnly
            )
          );
        }
      }
      
      foreach (var mod in modAssemblies)
      {
          var assembly = Assembly.LoadFrom(mod);
          foreach (var type in assembly.GetTypes())
          {
            Debug.Log(type);

            if (typeof(Mod).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
            {
              Mod instance = (Mod)Activator.CreateInstance(type);
              instance.OnLoad();     
              activeMods.Add(instance);
            }
          }
      }
    }
  }
}