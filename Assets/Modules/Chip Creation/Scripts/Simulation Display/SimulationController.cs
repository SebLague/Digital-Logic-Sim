using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.Simulation;
using DLS.ChipData;

namespace DLS.ChipCreation
{
	// Responsible for running the simulation, and communicating any changes in the loaded chip
	// (such as new connection being added, etc) to the simulation.
	public class SimulationController : MonoBehaviour
	{
		public bool runSimulation;
		public bool runOnce;

		public bool logSimulateTimer;

		public Palette palette;
		public SimulationDisplay display;
		ChipEditor chipEditor;

		ChipEditor viewedChipEditor;
		SimChip viewedChip;

		[Header("Debug Info")]
		public float avgSimFrameTimeMs;
		public int numFloatingInputs_debug;
		public int numCycleInputs_debug;
		Simulator simulator;


		long simulationEllapsedMs;
		long simFrames;

		public void Init(ChipDescription[] allChips)
		{
			simulator = new Simulator(allChips);
		}

		public void SetEditedChip(ChipEditor chipEditor)
		{
			// If new empty chip, make sure simulator has a 'blank' description it can load
			if (string.IsNullOrEmpty(chipEditor.LastSavedDescription.Name))
			{
				simulator.UpdateChipsFromDescriptions(new ChipDescription[] { chipEditor.LastSavedDescription });
			}
			// Set the chip
			simulator.SetSimulationChip(chipEditor.LastSavedDescription);

			this.chipEditor = chipEditor;

			chipEditor.SubChipAdded += OnChipAdded;
			chipEditor.SubChipDeleted += OnChipDeleted;

			chipEditor.WireEditor.WireCreated += OnWireCreated;
			chipEditor.WireEditor.WireDeleted += OnWireDeleted;

			chipEditor.PinPlacer.PinCreated += OnPinAdded;
			chipEditor.PinPlacer.PinDeleted += OnPinRemoved;

			viewedChipEditor = chipEditor;
			viewedChip = simulator.Chip;
		}


		public void UpdateChipsFromDescriptions(ChipDescription[] desc)
		{
			simulator.UpdateChipsFromDescriptions(desc);
		}


		public void SetView(ChipEditor viewedSubChipEditor, int[] subChipIDViewChain)
		{
			this.viewedChipEditor = viewedSubChipEditor;

			viewedChip = simulator.Chip;
			foreach (int subChipID in subChipIDViewChain)
			{
				viewedChip = viewedChip.GetChipOrSubChip(subChipID);
			}
		}

		void Update()
		{

			if (runSimulation && simulator is not null)
			{
				var sw = System.Diagnostics.Stopwatch.StartNew();
				RunSimulationFrame();
				if (logSimulateTimer)
				{
					Debug.Log($"Simulation completed in {sw.ElapsedMilliseconds} ms.");
				}
				simulationEllapsedMs += sw.ElapsedMilliseconds;
				simFrames++;
				avgSimFrameTimeMs = simulationEllapsedMs / (float)simFrames;


				display.UpdateDisplay(viewedChipEditor, viewedChip);
				numFloatingInputs_debug = simulator.numFloatingInputs_debug;
				numCycleInputs_debug = simulator.numCycleInputs_debug;

				if (runOnce)
				{
					runSimulation = false;
				}
			}
		}

		void RunSimulationFrame()
		{
			PinState[] inputStates = new PinState[chipEditor.PinPlacer.InputPins.Count];
			for (int i = 0; i < inputStates.Length; i++)
			{
				var pin = chipEditor.PinPlacer.InputPins[i];
				inputStates[i] = pin.State;
			}

			simulator.Simulate(inputStates, Time.time);
		}

		void OnWireCreated(Wire wire)
		{
			if (!IgnoreInSimulation(wire))
			{
				PinAddress sourceAddress = chipEditor.GetPinAddress(wire.SourcePin);
				PinAddress targetAddress = chipEditor.GetPinAddress(wire.TargetPin);
				simulator.AddConnection(sourceAddress, targetAddress);
			}
		}

		void OnWireDeleted(Wire wire)
		{
			if (!IgnoreInSimulation(wire))
			{
				PinAddress sourceAddress = chipEditor.GetPinAddress(wire.SourcePin);
				PinAddress targetAddress = chipEditor.GetPinAddress(wire.TargetPin);
				simulator.RemoveConnection(sourceAddress, targetAddress);
			}
		}

		void OnChipAdded(ChipBase chip)
		{
			simulator.AddChip(chip.Description, chip.ID);
		}

		void OnChipDeleted(ChipBase chip, int subChipIndex)
		{
			simulator.RemoveChip(chip.ID);
		}

		void OnPinAdded(EditablePin newPin)
		{
			PinAddress pinAddress = chipEditor.GetPinAddress(newPin.GetPin());
			simulator.AddPin(pinAddress);
		}

		void OnPinRemoved(EditablePin removedPin)
		{
			PinAddress pinAddress = chipEditor.GetPinAddress(removedPin.GetPin());
			simulator.RemovePin(pinAddress);
		}


		bool IgnoreInSimulation(Wire wire)
		{
			return false;
			//bool ignore = wire.SourcePin.Chip is SharedWireDisplay && wire.TargetPin.Chip is SharedWireDisplay;
			//return ignore;
		}

	}
}