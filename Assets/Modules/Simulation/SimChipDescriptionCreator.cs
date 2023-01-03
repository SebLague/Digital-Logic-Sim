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

		public SimChipDescriptionCreator(IList<ChipDescription> chipDescriptions)
		{
			chipDescriptionLookUp = chipDescriptions.ToDictionary(description => description.Name, description => description);

			SimChipDescription[] simChipDescriptions = chipDescriptions.Select(desc => CreateSimChipDescription(desc)).ToArray();
			simChipDescriptionLookUp = simChipDescriptions.ToDictionary(description => description.Name, description => description);
		}

		public void UpdateChipsFromDescriptions(ChipDescription[] descriptions)
		{

			foreach (ChipDescription desc in descriptions)
			{
				if (chipDescriptionLookUp.ContainsKey(desc.Name))
				{
					simChipDescriptionLookUp[desc.Name] = CreateSimChipDescription(desc);
				}
				else
				{
					chipDescriptionLookUp.Add(desc.Name, desc);
					simChipDescriptionLookUp.Add(desc.Name, CreateSimChipDescription(desc));
				}
			}
		}

		public SimChipDescription GetDescription(string chipName)
		{
			return simChipDescriptionLookUp[chipName];
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