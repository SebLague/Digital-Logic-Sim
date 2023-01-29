using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using System.Linq;

namespace DLS.Simulation
{
	public static class CycleDetector
	{

		public static void MarkCycles(SimChipDescription simChipDescription)
		{
			Dictionary<int, SimPinConnection[]> connectionsOutByChipID = new();

			// Only interested in connections between the subchips of the current chip (i.e. no connections involving chip's own input/output pins)
			SimPinConnection[] interSubChipConnections = simChipDescription.AllConnections.Where(c => c.Source.BelongsToSubChip && c.Target.BelongsToSubChip).ToArray();

			for (int i = 0; i < simChipDescription.NumSubChips; i++)
			{
				int subChipID = simChipDescription.SubChipIDs[i];
				// Group connections by the subchip that they're being output from (to make cycle detection easier).
				SimPinConnection[] connectionsOut = interSubChipConnections.Where(c => c.Source.SubChipID == subChipID).ToArray();
				connectionsOutByChipID.Add(subChipID, connectionsOut);
			}


			// Reset cycle flags in case they've been set previously.
			foreach (var connection in simChipDescription.AllConnections)
			{
				connection.TargetIsCyclePin = false;
			}


			for (int i = 0; i < simChipDescription.NumSubChips; i++)
			{
				if (simChipDescription.SubChipNames[i] != BuiltinChipNames.BusName)
				{
					HashSet<int> chipsOnPath = new HashSet<int>();
					int id = simChipDescription.SubChipIDs[i];
					MarkChipInputCycles(id, id, connectionsOutByChipID, chipsOnPath);
				}
			}

			simChipDescription.CycleDataUpToDate = true;

		}

		// Given an initialSubChipIndex, this function recursively loops at all paths, and if any of them loop back around to the initial chip, marks
		// the input pin to the chip as cycle.
		// Note: if a cyclic pin is encountered then the current path is abandoned so that only one pin will be marked as cyclic in any given loop
		static void MarkChipInputCycles(int initialSubChipID, int subChipID, Dictionary<int, SimPinConnection[]> connectionsOutByChipID, HashSet<int> chipsOnPath)
		{
			// If this chip has already been seen on this path, then we're in a cycle, but not one that comes back to the original chip.
			// So, just break out of it.
			if (chipsOnPath.Contains(subChipID))
			{
				return;
			}
			chipsOnPath.Add(subChipID);


			SimPinConnection[] connectionsOut = connectionsOutByChipID[subChipID];
			for (int outputPinIndex = 0; outputPinIndex < connectionsOut.Length; outputPinIndex++)
			{
				SimPinConnection connection = connectionsOut[outputPinIndex];
				if (!connection.TargetIsCyclePin)
				{
					// We've looped back around to the original chip, so mark input pin as cyclical.
					if (connection.Target.SubChipID == initialSubChipID)
					{
						connection.TargetIsCyclePin = true;
					}
					else
					{
						MarkChipInputCycles(initialSubChipID, connection.Target.SubChipID, connectionsOutByChipID, chipsOnPath);
					}
				}
			}
		}
	}
}
