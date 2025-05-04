using System.Collections.Generic;

namespace DLS.ModdingAPI
{
    public class CollectionBuilder
    {
        public readonly string name;
        public List<ChipBuilder> chips;

        public CollectionBuilder(string name)
        {
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