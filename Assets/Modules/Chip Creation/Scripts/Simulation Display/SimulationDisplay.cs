using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.Simulation;
using DLS.ChipData;

namespace DLS.ChipCreation
{
	public class SimulationDisplay : MonoBehaviour
	{
		[SerializeField] Palette palette;
		Dictionary<Pin, Palette.VoltageColour> busColourLookup;
		Dictionary<Pin, Wire> busLookup;

		ChipEditor chipEditor;
		SimChip simChip;
		System.Random rng;

		public void Init()
		{
			busColourLookup = new Dictionary<Pin, Palette.VoltageColour>();
			rng = new System.Random();
			busLookup = new Dictionary<Pin, Wire>();
		}

		// Updates the pins and wires in the currently-viewed chip to reflect the state of the simulation
		public void UpdateDisplay(ChipEditor chipEditor, SimChip simChip)
		{
			this.chipEditor = chipEditor;
			this.simChip = simChip;

			UpdatePinStates();
			UpdateEditablePinDisplays();
			UpdateWireDisplays();
			UpdateBusWireColours();


		}

		void UpdatePinStates()
		{
			// Set subchip pin states
			foreach (SimChip simSubChip in simChip.subChips)
			{
				SetPinStatesFromSimChip(simSubChip, isSubChip: true);
			}
			// Set input/output pin states
			// Note: input pins states are normally set by user, but do need to be set in the case of viewing a subChip
			SetPinStatesFromSimChip(simChip, isSubChip: false);

			void SetPinStatesFromSimChip(SimChip simChip, bool isSubChip)
			{
				foreach (var inputSimPin in simChip.inputPins)
				{
					PinType pinType = isSubChip ? PinType.SubChipInputPin : PinType.ChipInputPin;
					PinAddress address = new PinAddress(simChip.ID, inputSimPin.ID, pinType);
					SetState(address, inputSimPin);
				}

				foreach (var outputSimPin in simChip.outputPins)
				{
					PinType pinType = isSubChip ? PinType.SubChipOutputPin : PinType.ChipOutputPin;
					PinAddress address = new PinAddress(simChip.ID, outputSimPin.ID, pinType);
					SetState(address, outputSimPin);
				}

				void SetState(PinAddress address, SimPin simPin)
				{
					Pin displayPin = chipEditor.GetPin(address);
					displayPin.State = simPin.State;
					if (Application.isEditor)
					{
						displayPin.UpdateDebugInfo(simPin);
					}
				}
			}
		}

		void UpdateEditablePinDisplays()
		{
			// Update i/o pin displays
			foreach (EditablePin pin in chipEditor.PinPlacer.AllPins)
			{
				pin.UpdateDisplayState();
			}

		}

		void UpdateWireDisplays()
		{
			busColourLookup.Clear();
			busLookup.Clear();
			foreach (Wire wire in chipEditor.WireEditor.AllWires)
			{
				wire.UpdateDisplayState();
				if (wire.IsBusWire)
				{
					busLookup.Add(wire.SourcePin, wire);
					busLookup.Add(wire.TargetPin, wire);
				}
				// If outputting a value onto the bus, then record the colour of the wire so bus can display that colour
				if (!wire.IsBusWire && wire.TargetPin.IsBusPin && wire.SourcePin.State != PinState.FLOATING && wire.SourcePin.State == wire.TargetPin.State)
				{
					if (!busColourLookup.ContainsKey(wire.TargetPin))
					{
						busColourLookup.Add(wire.TargetPin, wire.ColourTheme);
					}
					// Two or more wires are outputting the same value onto the bus simultaneously. Choose randomly which wire's colour to display.
					else if (rng.NextDouble() < 0.5)
					{
						busColourLookup[wire.TargetPin] = wire.ColourTheme;
					}
				}
			}
		}

		void UpdateBusWireColours()
		{
			if (busColourLookup.Count > 0)
			{
				foreach (Wire wire in chipEditor.WireEditor.AllWires)
				{
					// Bus inherits colour from input wire
					if (wire.IsBusWire && busColourLookup.TryGetValue(wire.TargetPin, out Palette.VoltageColour theme))
					{
						wire.SetColourTheme(theme);
					}
				}

				foreach (Wire wire in chipEditor.WireEditor.AllWires)
				{
					// Wires that get value from bus inherit its colour
					if (wire.SourcePin.IsBusPin && busLookup.TryGetValue(wire.SourcePin, out Wire busWire))
					{
						wire.SetColourTheme(busWire.ColourTheme);
					}
				}
			}
		}
	}
}