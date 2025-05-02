using DLS.Description;
using DLS.Game;
using UnityEngine;

namespace DLS.ModdingAPI
{
    public static class ChipCreator
    {
        public static void RegisterChip(string name, Vector2 size, Color col, PinDescription[] inputs = null, PinDescription[] outputs = null, DisplayDescription[] displays = null, bool hideName = false)
        {
            ChipType type = ChipTypeHelper.AddNewModded(name);
            ModdedChipCreator.RegisterChip(type, size, col, inputs, outputs, displays, hideName);
        }

        public static PinDescription CreatePinDescription(string name, int id, PinBitCount bitCount = PinBitCount.Bit1)
        {
            return ModdedChipCreator.CreatePinDescription(name, id, bitCount);
        }
    }
}