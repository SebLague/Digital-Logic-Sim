using System.Collections.Generic;
using UnityEngine;

namespace DLS.ModdingAPI
{
    public static class Registry
    {
        public static List<ChipBuilder> moddedChips = new();
        public static List<CollectionBuilder> moddedCollections = new();
        public static readonly Dictionary<string, ShortcutBuilder> ModdedShortcuts = new();
        public static void RegisterChips(params ChipBuilder[] chips)
        {
            foreach (ChipBuilder chip in chips)
            {
                moddedChips.Add(chip);
            }
        }

        public static void RegisterCollections(params CollectionBuilder[] collections)
        {
            foreach (CollectionBuilder collection in collections)
            {
                moddedCollections.Add(collection);
            }
        }

        public static void RegisterShortcuts(params ShortcutBuilder[] shortcuts)
        {
            foreach (ShortcutBuilder shortcut in shortcuts)
            {
                if (!ModdedShortcuts.ContainsKey(shortcut.Name))
                {
                    ModdedShortcuts[shortcut.Name] = shortcut;
                }
                else
                {
                    Debug.LogWarning($"Shortcut with name '{shortcut.Name}' is already registered.");
                }
            }
        }
    }
}