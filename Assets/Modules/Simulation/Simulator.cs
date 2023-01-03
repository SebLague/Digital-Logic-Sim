using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;

namespace DLS.Simulation
{
	public class Simulator
	{

		public SimChip Chip => chip;

		SimulatedChipCreator simChipCreator;
		SimChip chip;

		public int numFloatingInputs_debug { get; private set; }
		public int numCycleInputs_debug { get; private set; }

		public static float Time { get; private set; }
		public static int FrameCount { get; private set; }

		public Simulator(IList<ChipDescription> allChips)
		{
			simChipCreator = new SimulatedChipCreator(allChips);
		}

		public void SetSimulationChip(ChipDescription chipDescription)
		{
			chip = simChipCreator.Load(chipDescription, id: 0);
			chip.description.CycleDataUpToDate = false;
		}

		public void UpdateChipFromDescription(ChipDescription desc)
		{
			UpdateChipsFromDescriptions(new ChipDescription[] { desc });
		}

		public void UpdateChipsFromDescriptions(ChipDescription[] desc)
		{
			simChipCreator.UpdateChipsFromDescriptions(desc);
		}

		public void Simulate(PinState[] inputStates, float time)
		{
			Time = time;
			FrameCount++;

			Debug.Assert(inputStates.Length == chip.inputPins.Length, $"Num inputs ({inputStates.Length}) does not match num pins ({chip.inputPins.Length})");
			if (!chip.description.CycleDataUpToDate)
			{
				Debug.Log("Rebuild cycle data");
				CycleDetector.MarkCycles(chip.description);
				chip.UpdateCyclesFromDescription();
			}

			// Clear frame debug info
			numFloatingInputs_debug = 0;
			numCycleInputs_debug = 0;

			// Process floating inputs. These are input pins which don't have any input, so no determined value.
			// In this simulation, these are treated as LOW.
			ProcessFloatingAndCyclic(chip);

			// Forward inputs through chip
			for (int i = 0; i < inputStates.Length; i++)
			{
				chip.inputPins[i].ReceiveInput(inputStates[i]);
			}
		}

		void ProcessFloatingAndCyclic(SimChip chip)
		{
			// Process floating pins. These are pins which don't have any input, so no determined value.
			// In this simulation, these are just treated as LOW.
			foreach (var floating in chip.subChipFloatingInputPins)
			{
				numFloatingInputs_debug++;
				floating.ReceiveInput(PinState.FLOATING);
			}
			// Handle floating outputs (todo: handle better; maybe same approach as floating inputs)
			if (!chip.IsBuiltin)
			{
				foreach (var p in chip.outputPins)
				{
					if (p.numInputs == 0)
					{
						p.ReceiveInput(PinState.FLOATING);
					}
				}
			}
			foreach (var cycle in chip.subChipCyclePins)
			{
				numCycleInputs_debug++;
				cycle.PropagateSignal();
			}

			// Recurse
			foreach (var sub in chip.subChips)
			{
				ProcessFloatingAndCyclic(sub);
			}
		}

		public void AddConnection(PinAddress source, PinAddress target)
		{
			chip.AddConnection(source, target);
		}

		// Remove a connection from the simulated chip
		public void RemoveConnection(PinAddress source, PinAddress target)
		{
			chip.RemoveConnection(source, target);
		}

		public void AddChip(ChipDescription chipDescription, int id)
		{
			SimChip newChip = simChipCreator.Load(chipDescription, id);
			chip.AddSubChip(newChip);

		}

		public void RemoveChip(int chipID)
		{
			chip.RemoveSubChip(chipID);

		}

		public void AddPin(PinAddress pinAddress)
		{
			chip.AddPin(pinAddress);
		}

		public void RemovePin(PinAddress pinAddress)
		{
			chip.RemovePin(pinAddress);
		}
	}
}