using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using System.Linq;

namespace DLS.ChipCreation
{
	public static class ChipDescriptionHelper
	{

		// Returns the names of all chips that use the given chip as a direct subchip
		public static string[] GetParentChipNames(string chipName)
		{
			return GetParentChipDescriptions(chipName).Select(desc => desc.Name).ToArray();
		}

		// Returns the descriptions of all chips that use the given chip as a direct subchip
		public static ChipDescription[] GetParentChipDescriptions(string chipName)
		{
			List<ChipDescription> parents = new();

			if (!string.IsNullOrWhiteSpace(chipName))
			{
				foreach (ChipDescription chip in ChipDescriptionLoader.AllChips)
				{
					if (chip.SubChips.Any(subchip => subchip.Name == chipName))
					{
						parents.Add(chip);
					}
				}
			}
			return parents.ToArray();
		}

		// Returns an array of the names of all of the given chip's subchips, and all of their subchips, and so on
		public static IList<string> GetAllSubChipNames(string chipName, bool includeParentName = false)
		{
			List<string> allNames = new List<string>();
			if (includeParentName)
			{
				allNames.Add(chipName);
			}
			AddSubChipNamesRecursively(chipName);
			return allNames;


			void AddSubChipNamesRecursively(string parentName)
			{
				ChipDescription description = ChipDescriptionLoader.GetChipDescription(parentName);

				foreach (ChipInstanceData subchip in description.SubChips)
				{
					allNames.Add(subchip.Name);
					AddSubChipNamesRecursively(subchip.Name);
				}
			}
		}

		public static void RenameSubChip(ref ChipDescription description, string subchipNameOld, string subchipNameNew)
		{
			for (int i = 0; i < description.SubChips.Length; i++)
			{
				if (description.SubChips[i].Name == subchipNameOld)
				{
					description.SubChips[i].Name = subchipNameNew;
				}
			}
		}

		// When a chip is deleted, chips that use the deleted chip as a subchip must have it removed from their description,
		// as well as all connections to and from it.
		public static void RemoveSubChip(ref ChipDescription description, string subchipName)
		{
			HashSet<int> removedSubChipIDs = new HashSet<int>();
			List<ChipInstanceData> filteredSubchips = new();
			for (int i = 0; i < description.SubChips.Length; i++)
			{
				if (description.SubChips[i].Name == subchipName)
				{
					removedSubChipIDs.Add(description.SubChips[i].ID);
				}
				else
				{
					filteredSubchips.Add(description.SubChips[i]);
				}
			}
			description.SubChips = filteredSubchips.ToArray();
			description.Connections = description.Connections.Where(c => !DeleteConnection(c)).ToArray();

			bool DeleteConnection(ConnectionDescription connection)
			{
				return removedSubChipIDs.Contains(connection.Source.SubChipID) || removedSubChipIDs.Contains(connection.Target.SubChipID);
			}
		}

		// When a chip is saved after deleting one or more input/output pins, chips that use this modified chip must remove
		// all connections to those deleted pins
		public static void RemoveConnectionsToDeletedPins(ref ChipDescription description, int[] deletedPinIDs, string modifiedSubChipName)
		{
			HashSet<int> removedPinIDs = new HashSet<int>(deletedPinIDs);
			HashSet<int> subchipIDs = new(description.SubChips.Where(s => s.Name == modifiedSubChipName).Select(s => s.ID));
			description.Connections = description.Connections.Where(c => !DeleteConnection(c)).ToArray();

			bool DeleteConnection(ConnectionDescription connection)
			{
				if (connection.Source.BelongsToSubChip && subchipIDs.Contains(connection.Source.SubChipID) && removedPinIDs.Contains(connection.Source.PinID))
				{
					return true;
				}
				if (connection.Target.BelongsToSubChip && subchipIDs.Contains(connection.Target.SubChipID) && removedPinIDs.Contains(connection.Target.PinID))
				{
					return true;
				}
				return false;
			}
		}

		public static IEnumerable<int> GetAllPinIDs(ChipDescription chipDescription)
		{
			return chipDescription.InputPins.Concat(chipDescription.OutputPins).Select(pin => pin.ID);
		}
	}
}