using System.Collections.Generic;
using System.Linq;
using DLS.Description;

namespace DLS.Game
{
	public class ChipLibrary
	{
		public readonly List<ChipDescription> allChips = new();

		readonly HashSet<string> builtinChipNames = new(ChipDescription.NameComparer);
		readonly Dictionary<string, ChipDescription> descriptionFromNameLookup = new(ChipDescription.NameComparer);

		readonly List<ChipDescription> hiddenChips = new();

		// Names of chips (built-in or custom) whose logic is purely combinational (static)
		readonly HashSet<string> staticChips = new(ChipDescription.NameComparer);

		// Built-in chip types that are pure combinational (no internal state)
		static readonly HashSet<ChipType> pureCombinationalBuiltins = new()
		{
			ChipType.Nand,
			ChipType.Split_4To1Bit,
			ChipType.Merge_1To4Bit,
			ChipType.Merge_1To8Bit,
			ChipType.Merge_4To8Bit,
			ChipType.Split_8To4Bit,
			ChipType.Split_8To1Bit,
			ChipType.TriStateBuffer,
		};

		public ChipLibrary(ChipDescription[] customChips, ChipDescription[] builtinChips)
		{
			// Add built-in chips to list of all chips
			foreach (ChipDescription chip in builtinChips)
			{
				// Bus terminus chip should not be shown to the user (it is created automatically upon placement of a bus start point)
				bool hidden = ChipTypeHelper.IsBusTerminusType(chip.ChipType) || chip.ChipType == ChipType.dev_Ram_8Bit;

				AddChipToLibrary(chip, hidden);
				builtinChipNames.Add(chip.Name);
			}

			// Add custom chips to list of all chips
			foreach (ChipDescription chip in customChips)
			{
				AddChipToLibrary(chip);
			}

			RebuildChipDescriptionLookup();
			RecomputeStaticChips();
		}

		void RebuildChipDescriptionLookup()
		{
			descriptionFromNameLookup.Clear();
			foreach (ChipDescription desc in allChips)
			{
				descriptionFromNameLookup.Add(desc.Name, desc);
			}

			foreach (ChipDescription desc in hiddenChips)
			{
				descriptionFromNameLookup.Add(desc.Name, desc);
			}
		}


		public bool IsBuiltinChip(string name) => builtinChipNames.Contains(name);

		public bool HasChip(string name) => TryGetChipDescription(name, out _);

		public ChipDescription GetChipDescription(string name) => descriptionFromNameLookup[name];

		public bool TryGetChipDescription(string name, out ChipDescription description) =>
			descriptionFromNameLookup.TryGetValue(name, out description);

		public void RemoveChip(string chipName)
		{
			allChips.RemoveAll(c => c.NameMatch(chipName));
			RebuildChipDescriptionLookup();
		}

		public void NotifyChipSaved(ChipDescription description)
		{
			// Replace chip description if already exists
			bool foundChip = false;

			for (int i = 0; i < allChips.Count; i++)
			{
				if (allChips[i].NameMatch(description.Name))
				{
					allChips[i] = description;
					foundChip = true;
					break;
				}
			}

			// Otherwise add as new description
			if (!foundChip) AddChipToLibrary(description);

			RebuildChipDescriptionLookup();
			RecomputeStaticChips();
		}

		public void NotifyChipRenamed(ChipDescription description, string nameOld)
		{
			// Replace chip description
			for (int i = 0; i < allChips.Count; i++)
			{
				if (allChips[i].NameMatch(nameOld))
				{
					allChips[i] = description;
					break;
				}
			}

			RebuildChipDescriptionLookup();
			RecomputeStaticChips();
		}

		public string[] GetAllCustomChipNames()
		{
			List<string> customChipNames = new();

			foreach (ChipDescription chip in allChips)
			{
				if (!IsBuiltinChip(chip.Name))
				{
					customChipNames.Add(chip.Name);
				}
			}

			return customChipNames.ToArray();
		}

		// Returns the descriptions of all chips that use the given chip as a direct subchip
		public ChipDescription[] GetDirectParentChips(string chipName)
		{
			List<ChipDescription> parents = new();

			foreach (ChipDescription other in allChips)
			{
				if (other.SubChips == null) continue;
				if (other.SubChips.Any(subchip => ChipDescription.NameMatch(subchip.Name, chipName)))
				{
					parents.Add(other);
				}
			}

			return parents.ToArray();
		}

		void AddChipToLibrary(ChipDescription description, bool hidden = false)
		{
			if (hidden) hiddenChips.Add(description);
			else allChips.Add(description);
		}

		// Recompute which chips are purely combinational (static) based solely on user override for custom chips and default pure builtins
		void RecomputeStaticChips()
		{
			staticChips.Clear();
			foreach (var chip in allChips)
			{
				// Custom chips honor user setting only
				if (chip.ForceStaticCombinational.HasValue)
				{
					if (chip.ForceStaticCombinational.Value)
						staticChips.Add(chip.Name);
				}
				// Pure combinational builtins are cached by default
				else if (IsBuiltinChip(chip.Name) && pureCombinationalBuiltins.Contains(chip.ChipType))
				{
					staticChips.Add(chip.Name);
				}
			}
		}

		// Query whether a chip (by name) is static/combinational
		public bool IsChipStatic(string chipName)
		{
			return staticChips.Contains(chipName);
		}
	}
}