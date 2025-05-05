using System.Collections.Generic;

namespace DLS.ModdingAPI
{
    public static class Registry
    {
        public static List<ChipBuilder> moddedChips = new();
        public static List<CollectionBuilder> moddedCollections = new();
        public static void RegisterChips(params ChipBuilder[] chips)
        {
            foreach (ChipBuilder chip in chips)
            {
                moddedChips.Add(chip);
            }
        }

        public static void RegisterCollections(params CollectionBuilder[] collections)
        {
            foreach(CollectionBuilder collection in collections)
            {
                moddedCollections.Add(collection);
            }
        }
    }
}