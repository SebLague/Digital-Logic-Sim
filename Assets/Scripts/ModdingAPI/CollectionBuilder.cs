using System.Collections.Generic;

namespace DLS.ModdingAPI
{
    public class CollectionBuilder
    {
        public string modID;
        public readonly string name;
        public List<ChipBuilder> chips;

        public CollectionBuilder(string modID, string name)
        {
            this.modID = modID;
            this.name = name;
            chips = new();
        }

        public CollectionBuilder AddChip(ChipBuilder chip)
        {
            chips.Add(chip);
            return this;
        }
    }
}