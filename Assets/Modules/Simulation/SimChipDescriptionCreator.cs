using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using System.Linq;

namespace DLS.Simulation
{
	public class SimChipDescriptionCreator
	{

		Dictionary<string, SimChipDescription> simChipDescriptionLookUp;
		Dictionary<string, ChipDescription> chipDescriptionLookUp;
		ChipDescription[] chipDescriptions;

		public SimChipDescriptionCreator(IList<ChipDescription> chipDescriptions)
		{
			this.chipDescriptions = chipDescriptions.ToArray();
			chipDescriptionLookUp = chipDescriptions.ToDictionary(description => description.Name, description => description);

			SimChipDescription[] simChipDescriptions = CreateAllSimChipDescriptions();
			simChipDescriptionLookUp = simChipDescriptions.ToDictionary(description => description.Name, description => description);
		}

		public SimChipDescription GetDescription(string chipName)
		{
			return simChipDescriptionLookUp[chipName];
		}

		// Create sim chip description for all chips
		SimChipDescription[] CreateAllSimChipDescriptions()
		{
			SimChipDescription[] simChipDescriptions = new SimChipDescription[chipDescriptions.Length];

			for (int i = 0; i < simChipDescriptions.Length; i++)
			{
				simChipDescriptions[i] = CreateSimChipDescription(chipDescriptions[i]);
			}

			return simChipDescriptions;
		}

		// Create sim chip description for specified chip
		public SimChipDescription CreateSimChipDescription(ChipDescription description)
		{
			SimChipDescription simDescription = new SimChipDescription();

			simDescription.Name = description.Name;
			simDescription.IsBuiltin = BuiltinChipNames.IsBuiltinName(description.Name);
			simDescription.SubChipNames = description.SubChips.Select(s => s.Name).ToList();
			simDescription.SubChipIDs = description.SubChips.Select(s => s.ID).ToList();
			simDescription.InputPinIDs = description.InputPins.Select(p => p.ID).ToArray();
			simDescription.OutputPinIDs = description.OutputPins.Select(p => p.ID).ToArray();

			simDescription.AllConnections = new List<SimPinConnection>();
			foreach (var connection in description.Connections)
			{
				if (ShouldSimulateConnection(connection, description))
				{
					SimPinConnection c = new SimPinConnection() { Source = connection.Source, Target = connection.Target };
					simDescription.AllConnections.Add(c);
				}
			}

			CycleDetector.MarkCycles(simDescription);

			return simDescription;
		}

		// Some connections are just 'convenience' wires which don't actually contribute to the simulation.
		bool ShouldSimulateConnection(ConnectionDescription connection, ChipDescription description)
		{
			if (connection.Source.SubChipID == connection.Target.SubChipID && connection.Source.BelongsToSubChip)
			{
				string subChipName = description.SubChips.First(s => s.ID == connection.Source.SubChipID).Name;
				if (BuiltinChipNames.Compare(subChipName, BuiltinChipNames.BusName))
				{
					return false;
				}
			}

			return true;
		}
	}
}