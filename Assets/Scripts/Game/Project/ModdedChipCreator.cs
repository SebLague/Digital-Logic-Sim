using System;
using System.Collections.Generic;
using DLS.Description;
using DLS.Simulation;
using UnityEngine;

namespace DLS.Game
{
    public static class ModdedChipCreator
    {
		public static List<ChipDescription> ModdedChips = new();
        private static readonly Dictionary<ChipDescription, Action<SimPin[], SimPin[]>> ModdedChipFunctions = new();

		public static ChipDescription[] CreateAllModdedChipDescriptions()
		{
			return ModdedChips.ToArray();
		}

        public static void RegisterChip(
			string name,
            Vector2 size,
            Color col,
            PinDescription[] inputs = null,
            PinDescription[] outputs = null,
            DisplayDescription[] displays = null,
            bool hideName = false,
            Action<SimPin[], SimPin[]> simulationFunction = null)
        {
            // Register the chip description
            ChipDescription chipDescription = CreateModdedChipDescription(name, ChipType.Modded, size, col, inputs, outputs, displays, hideName);
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

        public static bool TryGetSimulationFunction(ChipDescription chipDescription, out Action<SimPin[], SimPin[]> simulationFunction)
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