using System.Collections.Generic;
using System.Linq;
using DLS.Description;

namespace DLS.Game
{
    public static class ModdedCollectionCreator
    {
        public static List<ChipCollection> ModdedCollections = new();

        public static ChipCollection[] CreateModdedChipCollections()
        {
            return ModdedCollections.ToArray();
        }
        public static void RegisterCollection(string name, string[] chipNames)
        {
            ModdedCollections.Add(new ChipCollection(name, chipNames));
        }
    }
}