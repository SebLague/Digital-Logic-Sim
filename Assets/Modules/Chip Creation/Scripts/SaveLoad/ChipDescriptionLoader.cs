using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using System.Collections.ObjectModel;
using System.Linq;

namespace DLS.ChipCreation
{
	public static class ChipDescriptionLoader
	{
		public static ReadOnlyCollection<ChipDescription> AllChips => new(builtinChips.Concat(customChips).ToList());
		public static ReadOnlyCollection<ChipDescription> BuiltinChips => new(builtinChips);
		public static ReadOnlyCollection<ChipDescription> CustomChips => new(customChips);
		// Lookup chip description by name (case-insensitive)
		static Dictionary<string, ChipDescription> chipLookup;
		static List<ChipDescription> builtinChips;
		static List<ChipDescription> customChips;

		public static void LoadChips(string projectName)
		{
			ChipDescription[] loadedBuiltinChips = BuiltinChipDescriptionCreator.CreateBuiltinChipDescriptions();
			ChipDescription[] loadedCustomChips = ChipSaver.LoadAllSavedChips(projectName);

			builtinChips = new List<ChipDescription>();
			customChips = new List<ChipDescription>();
			chipLookup = new Dictionary<string, ChipDescription>(System.StringComparer.OrdinalIgnoreCase);

			AddChips(loadedBuiltinChips);
			AddChips(loadedCustomChips);
		}

		// Get chip description by name (case-insensitive)
		public static ChipDescription GetChipDescription(string chipName)
		{
			return chipLookup[chipName];
		}

		public static bool TryGetChipDescription(string chipName, out ChipDescription description)
		{
			return chipLookup.TryGetValue(chipName, out description);
		}


		public static bool HasLoaded(string chipName)
		{
			return chipLookup.ContainsKey(chipName);
		}


		// Delete this chip
		public static void RemoveChip(string chipName)
		{
			ChipDescription description = GetChipDescription(chipName);
			customChips.Remove(description);
			chipLookup.Remove(chipName);
		}

		public static void AddChip(ChipDescription description)
		{
			if (BuiltinChipNames.IsBuiltinName(description.Name))
			{
				builtinChips.Add(description);
			}
			else
			{
				customChips.Add(description);
			}
			chipLookup.Add(description.Name, description);
		}

		public static void UpdateChipDescription(ChipDescription updatedDescription, string nameOld)
		{
			ChipDescription descriptionOld = GetChipDescription(nameOld);
			customChips.Remove(descriptionOld);
			customChips.Add(updatedDescription);
			chipLookup.Remove(nameOld);
			chipLookup.Add(updatedDescription.Name, updatedDescription);
		}

		public static void UpdateChipDescriptions(ChipDescription[] updatedDescriptions)
		{
			foreach (var desc in updatedDescriptions)
			{
				UpdateChipDescription(desc);
			}
		}

		// Update a description that has been modified
		public static void UpdateChipDescription(ChipDescription updatedDescription)
		{
			ChipDescription descriptionOld = GetChipDescription(updatedDescription.Name);
			customChips.Remove(descriptionOld);
			customChips.Add(updatedDescription);
			chipLookup[updatedDescription.Name] = updatedDescription;
		}

		static void AddChips(IList<ChipDescription> descriptions)
		{
			foreach (var d in descriptions)
			{
				AddChip(d);
			}
		}


		[RuntimeInitializeOnLoadMethod]
		static void Initialize()
		{
		}
	}
}