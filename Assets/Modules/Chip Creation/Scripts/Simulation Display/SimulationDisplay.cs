using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.Simulation;
using DLS.ChipData;
using System.Linq;

namespace DLS.ChipCreation
{
	public class SimulationDisplay : MonoBehaviour
	{
		[SerializeField] Palette palette;

		// Updates the pins and wires in the currently-viewed chip to reflect the state of the simulation
		public void UpdateDisplay(ChipEditor chipEditor, SimChip simChip)
		{

			// Set subchip pin states
			foreach (SimChip simSubChip in simChip.subChips)
			{
				SetPinStatesFromSimChip(simSubChip, isSubChip: true);
			}

			// Set input/output pin states
			// Note: input pins states are normally set by user, but do need to be set in the case of viewing a subChip
			SetPinStatesFromSimChip(simChip, isSubChip: false);
			// Update i/o pin displays
			foreach (EditablePin pin in chipEditor.PinPlacer.AllPins)
			{
				pin.UpdateDisplayState();
			}

			// Update wire displays
			foreach (Wire wire in chipEditor.WireEditor.AllWires)
			{
				wire.UpdateDisplayState();
			}

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
	}
}