using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.ModdingAPI;

namespace DLS.Game
{
    public static class ModdedCollectionCreator
    {
        static List<CollectionBuilder> unbuiltCollections = new();
        public static List<ChipCollection> ModdedCollections = new();

        static ModdedCollectionCreator()
        {
            unbuiltCollections = Registry.moddedCollections;
            foreach(CollectionBuilder collection in unbuiltCollections)
            {
                RegisterCollection(collection.name, collection.chips.Select(chip => chip.name).ToArray());
            }
        }
        public static void RegisterCollection(string name, string[] chipNames)
        {
            ModdedCollections.Add(new ChipCollection(name, chipNames));
        }
    }
}