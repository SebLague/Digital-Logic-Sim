using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using System.Linq;

namespace DLS.Simulation
{
	public class SimulatedChipCreator
	{
		SimChipDescriptionCreator simDescriptionCreator;

		public SimulatedChipCreator(IList<ChipDescription> allChips)
		{
			simDescriptionCreator = new SimChipDescriptionCreator(allChips);
		}

		public void UpdateChipsFromDescriptions(ChipDescription[] descriptions)
		{
			simDescriptionCreator.UpdateChipsFromDescriptions(descriptions);
		}

		public SimChip Load(ChipDescription description, int id)
		{
			SimChipDescription simDescription = simDescriptionCreator.GetDescription(description.Name);
			SimChip loadedChip = CreateChip(simDescription, id);
			SetFloatingInputs(loadedChip);

			return loadedChip;
		}

		// Create chip from the given description and recursively create all of its sub-chips as well (and all their sub-chips, and so on...)
		SimChip CreateChip(SimChipDescription description, int id)
		{
			SimChip chip = new SimChip(id);
			chip.description = description;
			chip.name = description.Name;

			// Create empty I/O pin arrays
			chip.inputPins = new SimPin[description.NumInputs];
			chip.outputPins = new SimPin[description.NumOutputs];

			// If is builtin chip, then create the builtin chip instance
			if (description.IsBuiltin)
			{
				chip.builtinChip = BuiltinSimChipCreator.CreateFromName(description.Name, chip.inputPins, chip.outputPins);
			}

			// Assign pins
			for (int i = 0; i < chip.inputPins.Length; i++)
			{
				SimPin inputPin = new SimPin(chip.builtinChip, true, DebugPinName(chip, i, true), description.InputPinIDs[i]);
				chip.SetInputPin(i, inputPin);
			}

			for (int i = 0; i < chip.outputPins.Length; i++)
			{
				chip.SetOutputPin(i, new SimPin(null, false, DebugPinName(chip, i, false), description.OutputPinIDs[i]));
			}

			// Create and populate child chip array
			chip.subChips = new SimChip[description.SubChipNames.Count];

			for (int subChipIndex = 0; subChipIndex < chip.subChips.Length; subChipIndex++)
			{
				string subChipName = description.SubChipNames[subChipIndex];
				SimChipDescription subChipDescription = simDescriptionCreator.GetDescription(subChipName);
				// Recursively create sub-chip and all of its sub-chips (and so on)
				SimChip subChip = CreateChip(subChipDescription, description.SubChipIDs[subChipIndex]);
				chip.subChips[subChipIndex] = subChip;
			}

			// Connections
			foreach (SimPinConnection connection in description.AllConnections)
			{
				SimPin source = chip.GetPin(connection.Source);
				SimPin target = chip.GetPin(connection.Target);
				if (connection.TargetIsCyclePin)
				{
					target.MarkCyclic();
				}
				source.AddConnectedPin(target);
			}

			return chip;
		}

		string DebugPinName(SimChip chip, int index, bool inputPin)
		{
			return $"{chip.name} {(inputPin ? "input" : "output")} {index}";
		}

		void SetFloatingInputs(SimChip parentChip)
		{
			foreach (SimChip subChip in parentChip.subChips)
			{
				foreach (SimPin pin in subChip.inputPins)
				{
					if (pin.isFloating)
					{
						parentChip.subChipFloatingInputPins.Add(pin);
					}
					if (pin.cycleFlag)
					{
						parentChip.subChipCyclePins.Add(pin);
					}
				}

				// Recurse
				SetFloatingInputs(subChip);
			}
		}

		(SimPin[] floating, SimPin[] cyclic) GetFloatingInputs(SimChip parentChip, bool includeParentFloatingInput)
		{
			List<SimPin> floatingInputs = new List<SimPin>();
			List<SimPin> cyclicInputs = new List<SimPin>();
			// Note: might not want to add parent's inputs as these are controlled by the user and so not really floating.
			// (the exception is if this is actually a subchip added during edit process)
			AddFloatingInputs(parentChip, includeParentFloatingInput);

			return (floatingInputs.ToArray(), cyclicInputs.ToArray());

			void AddFloatingInputs(SimChip chip, bool addInputs)
			{
				if (addInputs)
				{
					foreach (SimPin pin in chip.inputPins)
					{
						if (pin.isFloating)
						{
							floatingInputs.Add(pin);
						}
						if (pin.cycleFlag)
						{
							cyclicInputs.Add(pin);
						}
					}
				}

				foreach (SimChip subChip in chip.subChips)
				{
					AddFloatingInputs(subChip, true);
				}
			}
		}
	}
}