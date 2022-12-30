using DLS.ChipData;
using System.Collections.Generic;
using System.Linq;

namespace DLS.Simulation
{
	// Representation of a custom chip inside the simulation.
	// Note: these are used for setting up the simulation and to enable easily navigating through the results for updating a graphical display.
	// The simulation itself deals only with SimPins and BuiltinChips.
	public class SimChip
	{
		public string name;
		public SimChipDescription description;

		public bool IsBuiltin => builtinChip is not null;
		// Null if not a builtin chip
		public BuiltinSimChip builtinChip;

		public SimChip[] subChips;
		public SimPin[] inputPins;
		public SimPin[] outputPins;
		public readonly int ID;

		// List of all sub chips' input pins which are floating (have no connections to them)
		public List<SimPin> subChipFloatingInputPins;
		public List<SimPin> subChipCyclePins;

		public SimChip(int id)
		{
			this.ID = id;
			subChipCyclePins = new();
			subChipFloatingInputPins = new();
		}


		public SimPin GetPin(PinAddress address)
		{
			SimChip c = address.BelongsToSubChip ? GetSubChip(address.SubChipID) : this;
			SimPin[] pins = address.IsInputPin ? c.inputPins : c.outputPins;
			var x = pins.FirstOrDefault(p => p.ID == address.PinID);
			return x;
		}

		public SimChip GetChipOrSubChip(int id)
		{
			if (this.ID == id)
			{
				return this;
			}
			return GetSubChip(id);
		}

		SimChip GetSubChip(int id)
		{
			foreach (SimChip subChip in subChips)
			{
				if (subChip.ID == id)
				{
					return subChip;
				}
			}
			return null;
		}

		public void SetInputPin(int index, SimPin pin)
		{
			inputPins[index] = pin;
			if (IsBuiltin)
			{
				builtinChip.SetInputPin(index, pin);
			}
		}

		public void SetOutputPin(int index, SimPin pin)
		{
			outputPins[index] = pin;
			if (IsBuiltin)
			{
				builtinChip.SetOutputPin(index, pin);
			}
		}

		public void AddSubChip(SimChip subChip)
		{
			subChips = subChips.Append(subChip).ToArray();

			subChipFloatingInputPins.AddRange(subChip.inputPins);
			//floatingPins.AddRange(subChip.outputPins);

			description.AddSubChip(subChip);
		}



		public void RemoveSubChip(int id)
		{
			SimChip subChipToRemove = GetSubChip(id);



			for (int i = 0; i < subChipToRemove.inputPins.Length; i++)
			{
				SimPin floatingInput = subChipToRemove.inputPins[i];
				if (subChipFloatingInputPins.Contains(floatingInput))
				{
					if (!floatingInput.isFloating)
					{
						UnityEngine.Debug.Log("floating flag wrong");
					}
					subChipFloatingInputPins.Remove(floatingInput);

				}
				else if (floatingInput.isFloating)
				{
					UnityEngine.Debug.Log("floating flag wrong");
				}

			}

			subChips = subChips.Where(s => s != subChipToRemove).ToArray();
			description.RemoveSubChip(id);
		}

		public void AddPin(PinAddress address)
		{
			SimPin pin = new SimPin(null, address.IsInputPin, "added pin", address.PinID);

			if (address.IsInputPin)
			{
				inputPins = inputPins.Append(pin).ToArray();
			}
			else
			{
				outputPins = outputPins.Append(pin).ToArray();
			}

			description.AddPin(address);
		}

		public void RemovePin(PinAddress address)
		{
			if (address.IsInputPin)
			{
				inputPins = inputPins.Where(pin => pin.ID != address.PinID).ToArray();
			}
			else
			{
				outputPins = outputPins.Where(pin => pin.ID != address.PinID).ToArray();
			}
			description.RemovePin(address);
		}

		public void AddConnection(PinAddress source, PinAddress target)
		{
			SimPin pin = GetPin(source);
			SimPin targetPin = GetPin(target);

			if (targetPin.isFloating)
			{
				subChipFloatingInputPins.Remove(targetPin);
			}

			pin.AddConnectedPin(targetPin);
			description.AddConnection(source, target);
		}

		public void RemoveConnection(PinAddress source, PinAddress target)
		{
			SimPin pin = GetPin(source);
			SimPin targetPin = GetPin(target);


			pin.RemoveConnectedPin(targetPin);
			if (targetPin.isFloating)
			{
				subChipFloatingInputPins.Add(targetPin);
			}
			description.RemoveConnection(source, target);

		}

		public void UpdateCyclesFromDescription()
		{
			// Clear
			foreach (var pin in subChipCyclePins)
			{
				pin.cycleFlag = false;
			}
			subChipCyclePins.Clear();

			// Rebuild
			foreach (var connection in description.AllConnections)
			{
				if (connection.TargetIsCyclePin)
				{
					SimPin pin = GetPin(connection.Target);
					pin.MarkCyclic();
					subChipCyclePins.Add(pin);
				}
			}
		}

	}
}