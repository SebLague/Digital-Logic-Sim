using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DLS.Description
{
	public struct ProjectDescription
	{
		public string ProjectName;
		public string DLSVersion_LastSaved;
		public string DLSVersion_EarliestCompatible;
		public DateTime CreationTime;
		public DateTime LastSaveTime;

		// Prefs
		public int Prefs_MainPinNamesDisplayMode;
		public int Prefs_ChipPinNamesDisplayMode;
		public int Prefs_GridDisplayMode;
		public int Prefs_Snapping;
		public int Prefs_StraightWires;
		public bool Prefs_SimPaused;
		public int Prefs_SimTargetStepsPerSecond;
		public int Prefs_SimStepsPerClockTick;

		// List of all player-created chips (in order of creation -- oldest first)
		public string[] AllCustomChipNames;

		public List<StarredItem> StarredList;
		public List<ChipCollection> ChipCollections;

		// ---- Helper functions ----
		public bool IsStarred(string chipName, bool isCollection)
		{
			foreach (StarredItem item in StarredList)
			{
				if (item.IsCollection == isCollection && ChipDescription.NameMatch(chipName, item.Name)) return true;
			}

			return false;
		}
	}

	public struct StarredItem
	{
		public string Name;
		public bool IsCollection;

		// Cached displayed strings to avoid allocations. (Not serialized, just regenerated on load)
		[JsonIgnore] string bottomBar_collectionDisplayNameOpen;
		[JsonIgnore] string bottomBar_collectionDisplayNameClosed;


		public StarredItem(string name, bool isCollection) : this()
		{
			Name = name;
			IsCollection = isCollection;

			CacheDisplayStrings();
		}

		public void CacheDisplayStrings()
		{
			bottomBar_collectionDisplayNameOpen = "\u25b4<halfSpace>" + Name;
			bottomBar_collectionDisplayNameClosed = "\u25b8<halfSpace>" + Name;
		}

		public string GetDisplayStringForBottomBar(bool open)
		{
			if (IsCollection) return open ? bottomBar_collectionDisplayNameOpen : bottomBar_collectionDisplayNameClosed;
			return Name;
		}
	}

	public class ChipCollection
	{
		public readonly List<string> Chips;
		[JsonIgnore] string displayName_closed;
		[JsonIgnore] string displayName_empty;

		// Cached displayed strings to avoid allocations. (Not serialized, just regenerated on load)
		[JsonIgnore] string displayName_open;
		public bool IsToggledOpen;
		public string Name;

		public ChipCollection(string name, params string[] chips)
		{
			Name = name;
			Chips = new List<string>(chips);
			UpdateDisplayStrings();
		}

		public void UpdateDisplayStrings()
		{
			displayName_open = "\u25bc " + Name;
			displayName_closed = "\u25b6 " + Name;
			displayName_empty = "\u25cc " + Name;
		}

		public string GetDisplayString()
		{
			if (Chips.Count == 0) return displayName_empty;
			return IsToggledOpen ? displayName_open : displayName_closed;
		}
	}
}