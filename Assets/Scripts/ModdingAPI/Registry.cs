using System.Linq;
using DLS.Game;

namespace DLS.ModdingAPI
{
    public static class Registry
    {
        public static void RegisterChips(params ChipBuilder[] chips)
        {
            foreach (ChipBuilder chip in chips)
            {
                ModdedChipCreator.RegisterChip(chip.name, chip.size, chip.color, chip.inputs, chip.outputs, chip.displays, chip.hideName, chip.simulationFunction);
            }
        }

        public static void RegisterCollections(params CollectionBuilder[] collections)
        {
            foreach(CollectionBuilder collection in collections)
            {
                ModdedCollectionCreator.RegisterCollection(collection.name, collection.chips.Select(chip => chip.name).ToArray());
            }
        }
    }
}