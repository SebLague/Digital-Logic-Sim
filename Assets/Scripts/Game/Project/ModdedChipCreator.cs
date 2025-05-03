using DLS.Description;
using UnityEngine;

namespace DLS.Game
{
    public static class ModdedChipCreator
    {
        public static void RegisterChip(ChipType type, Vector2 size, Color col, PinDescription[] inputs = null, PinDescription[] outputs = null, DisplayDescription[] displays = null, bool hideName = false)
		{
			ChipDescription chipDescription = BuiltinChipCreator.CreateBuiltinChipDescription(type, size, col, inputs, outputs, displays, hideName);
			BuiltinChipCreator.ModdedChips.Add(chipDescription);
		}

        public static PinDescription CreatePinDescription(string name, int id, PinBitCount bitCount = PinBitCount.Bit1) =>
			new(
				name,
				id,
				Vector2.zero,
				bitCount,
				PinColour.Red,
				PinValueDisplayMode.Off
			);
    }
}