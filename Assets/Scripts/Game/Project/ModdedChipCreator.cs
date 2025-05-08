using System;
using System.Collections.Generic;
using System.Linq;
using DLS.Description;
using DLS.ModdingAPI;
using UnityEngine;
using PinDescription = DLS.Description.PinDescription;

namespace DLS.Game
{
    public static class ModdedChipCreator
    {
		static List<ChipBuilder> unbuiltChips = new();
		public static List<ChipDescription> ModdedChips = new();
        private static readonly Dictionary<ChipDescription, Action<uint[], uint[]>> ModdedChipFunctions = new();

		public static ChipDescription[] CreateAllModdedChipDescriptions()
		{
			unbuiltChips = Registry.moddedChips;
			foreach (ChipBuilder chip in unbuiltChips)
			{
				RegisterChip(chip.modID, chip.name, chip.size, chip.color, ConvertToDescriptionPins(chip.inputs), ConvertToDescriptionPins(chip.outputs), chip.displays != null ? ModdedDisplayCreator.RegisterDisplays(chip.displays) : null, chip.hideName, chip.simulationFunction);
			}
			return ModdedChips.ToArray();
		}

        static void RegisterChip(
			string modID,
			string name,
            Vector2 size,
            Color col,
            PinDescription[] inputs = null,
            PinDescription[] outputs = null,
            DisplayDescription[] displays = null,
            bool hideName = false,
            Action<uint[], uint[]> simulationFunction = null)
        {
            // Register the chip description
            ChipDescription chipDescription = CreateModdedChipDescription(name, ChipType.Modded, size, col, inputs, outputs, displays, hideName);
			chipDescription.DependsOnModIDs.Add(modID);
            ModdedChips.Add(chipDescription);

            // Register the simulation function
            if (simulationFunction != null)
            {
                ModdedChipFunctions[chipDescription] = simulationFunction;
            }
        }

		static ChipDescription CreateModdedChipDescription(string name, ChipType type, Vector2 size, Color col, PinDescription[] inputs = null, PinDescription[] outputs = null, DisplayDescription[] displays = null, bool hideName = false)
		{
			ValidatePinIDs(inputs, outputs, name);

			return new ChipDescription
			{
				Name = name,
				NameLocation = hideName ? NameDisplayLocation.Hidden : NameDisplayLocation.Centre,
				Colour = col,
				Size = new Vector2(size.x, size.y),
				InputPins = inputs ?? Array.Empty<PinDescription>(),
				OutputPins = outputs ?? Array.Empty<PinDescription>(),
				SubChips = Array.Empty<SubChipDescription>(),
				Wires = Array.Empty<WireDescription>(),
				Displays = displays,
				ChipType = type
			};
		}

		static PinDescription[] ConvertToDescriptionPins(ModdingAPI.PinDescription[] moddingPins)
		{
			if (moddingPins == null) return null;

			return moddingPins.Select(pin => new PinDescription(
				pin.Name,
				pin.ID,
				pin.Position,
                (Description.PinBitCount) pin.BitCount,
                (Description.PinColour) pin.Colour,
                (Description.PinValueDisplayMode) pin.ValueDisplayMode
            )).ToArray();
		}

        public static bool TryGetSimulationFunction(ChipDescription chipDescription, out Action<uint[], uint[]> simulationFunction)
        {
            return ModdedChipFunctions.TryGetValue(chipDescription, out simulationFunction);
        }
		
		static void ValidatePinIDs(PinDescription[] inputs, PinDescription[] outputs, string chipName)
		{
			HashSet<int> pinIDs = new();

			AddPins(inputs);
			AddPins(outputs);
			return;

			void AddPins(PinDescription[] pins)
			{
				if (pins == null) return;
				foreach (PinDescription pin in pins)
				{
					if (!pinIDs.Add(pin.ID))
					{
						throw new Exception($"Pin has duplicate ID ({pin.ID}) in modded chip: {chipName}");
					}
				}
			}
		}
    }
}