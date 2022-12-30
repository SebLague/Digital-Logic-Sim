using System.Collections;
using System.Collections.Generic;
using DLS.ChipData;
using System.Linq;

namespace DLS.Simulation
{
	public class SimChipDescription
	{
		public string Name;
		public int NumInputs => InputPinIDs.Length;
		public int NumOutputs => OutputPinIDs.Length;
		public int[] InputPinIDs;
		public int[] OutputPinIDs;
		public bool IsBuiltin;

		public List<string> SubChipNames;
		public List<int> SubChipIDs;
		public List<SimPinConnection> AllConnections;
		public bool CycleDataUpToDate;

		public int NumSubChips => SubChipNames.Count;


		public void AddPin(PinAddress pinAddress)
		{
			if (pinAddress.IsInputPin)
			{
				InputPinIDs = InputPinIDs.Append(pinAddress.PinID).ToArray();
			}
			else
			{
				OutputPinIDs = OutputPinIDs.Append(pinAddress.PinID).ToArray();
			}
		}

		// Remove an input/output pin from a chip.
		// Note: this doesn't automatically remove connections associated with that pin, that must be done manually.
		public void RemovePin(PinAddress pinAddress)
		{
			if (pinAddress.IsInputPin)
			{
				InputPinIDs = InputPinIDs.Where(id => id != pinAddress.PinID).ToArray();
			}
			else
			{
				OutputPinIDs = OutputPinIDs.Where(id => id != pinAddress.PinID).ToArray();
			}
		}

		public void AddSubChip(SimChip newSubChip)
		{
			SubChipNames.Add(newSubChip.name);
			SubChipIDs.Add(newSubChip.ID);
		}


		// Note: All connections to and from this chip must be removed prior to calling this function
		public void RemoveSubChip(int subChipID)
		{
			int index = SubChipIDs.IndexOf(subChipID);
			SubChipNames.RemoveAt(index);
			SubChipIDs.RemoveAt(index);
		}

		// Note: adding a connection will result in cycle flags potentially no longer being correct,
		// and so they must be rebuilt before running the simulation.
		public void AddConnection(PinAddress source, PinAddress target)
		{
			SimPinConnection connection = new SimPinConnection() { Source = source, Target = target };
			AllConnections.Add(connection);
			CycleDataUpToDate = false;
		}

		// Note: removing a connection will result in cycle flags potentially no longer being correct,
		// and so they must be rebuilt before running the simulation.
		public void RemoveConnection(PinAddress source, PinAddress target)
		{
			for (int i = 0; i < AllConnections.Count; i++)
			{
				SimPinConnection connection = AllConnections[i];
				if (PinAddress.AreSame(connection.Source, source) && PinAddress.AreSame(connection.Target, target))
				{
					AllConnections.RemoveAt(i);
					break;
				}
			}
			CycleDataUpToDate = false;
		}

	}

	public class SimPinConnection
	{
		public PinAddress Source;
		public PinAddress Target;
		public bool TargetIsCyclePin;
	}

}