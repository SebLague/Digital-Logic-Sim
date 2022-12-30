using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace DLS.ChipCreation
{
	public class ProjectSettings
	{



		public ProjectSettings(string projectName)
		{
			ProjectName = projectName;
		}

		[JsonProperty] public string ProjectName;
		[JsonProperty] public string BuildVersion;
		[JsonProperty] public System.DateTime CreationTime;
		[JsonProperty] public DisplayOptions DisplayOptions { get; private set; }
		// List of all created chips (in order of creation -- older first)
		[JsonProperty] List<string> AllCreatedChips;
		// List of starred chips (sorted by time starred -- oldest first)
		[JsonProperty] List<string> StarredChips;



		public void AddNewChip(string chipName, bool starByDefault = true)
		{
			AllCreatedChips ??= new List<string>();
			AllCreatedChips.Add(chipName);
			if (starByDefault)
			{
				SetStarredState(chipName, true, autosave: false);
			}
			Save();
		}

		public void RemoveChip(string chipName)
		{
			StarredChips.Remove(chipName);
			AllCreatedChips.Remove(chipName);
			Save();
		}

		public void UpdateProjectName(string newName)
		{
			ProjectName = newName;
			Save();
		}

		public void UpdateDisplayOptions(DisplayOptions displayOptions, bool autosave = true)
		{
			DisplayOptions = displayOptions;
			if (autosave)
			{
				Save();
			}
		}


		public void UpdateChipName(string nameOld, string nameNew)
		{
			int index = AllCreatedChips.IndexOf(nameOld);
			AllCreatedChips[index] = nameNew;

			int starredIndex = StarredChips.IndexOf(nameOld);
			if (starredIndex >= 0)
			{
				StarredChips[starredIndex] = nameNew;
			}

			Save();
		}

		public void SetStarredState(string chipName, bool star, bool autosave = true)
		{
			StarredChips ??= new List<string>();

			if (star && !IsStarred(chipName))
			{
				StarredChips.Add(chipName);
			}
			else if (!star && IsStarred(chipName))
			{
				StarredChips.Remove(chipName);
			}

			if (autosave)
			{
				Save();
			}
		}

		public ReadOnlyCollection<string> GetStarredChipNames() => new ReadOnlyCollection<string>(StarredChips);
		public ReadOnlyCollection<string> GetAllCreatedChipNames() => new ReadOnlyCollection<string>(AllCreatedChips ?? new List<string>());

		public bool IsStarred(string chipName)
		{
			if (StarredChips is null)
			{
				return false;
			}
			return StarredChips.Contains(chipName);
		}

		void Save()
		{
			ProjectSettingsLoader.SaveProjectSettings(this);
		}

	}
}