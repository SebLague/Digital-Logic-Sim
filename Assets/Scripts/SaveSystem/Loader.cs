using System;
using System.Collections.Generic;
using System.IO;
using DLS.Description;
using DLS.Description.Types;
using DLS.Game;
using UnityEngine.WSA;

namespace DLS.SaveSystem
{
	public static class Loader
	{
		public static AppSettings LoadAppSettings()
		{
			if (File.Exists(SavePaths.AppSettingsPath))
			{
				string settingsString = File.ReadAllText(SavePaths.AppSettingsPath);
				return Serializer.DeserializeAppSettings(settingsString);
			}

			return AppSettings.Default();
		}

		public static Project LoadProject(string projectName)
		{
			ProjectDescription projectDescription = LoadProjectDescription(projectName);
			ChipLibrary chipLibrary = LoadChipLibrary(projectDescription);
			return new Project(projectDescription, chipLibrary);
		}

		public static bool ProjectExists(string projectName)
		{
			string path = SavePaths.GetProjectDescriptionPath(projectName);
			return File.Exists(path);
		}

		public static ProjectDescription LoadProjectDescription(string projectName)
		{
			string path = SavePaths.GetProjectDescriptionPath(projectName);
			if (!File.Exists(path)) throw new Exception("No project description found at " + path);

			ProjectDescription desc = Serializer.DeserializeProjectDescription(File.ReadAllText(path));
			desc.ProjectName = projectName; // Enforce name = directory name (in case player modifies manually -- operations like deleting projects rely on this)

			for (int i = 0; i < desc.StarredList.Count; i++)
			{
				StarredItem starred = desc.StarredList[i];
				starred.CacheDisplayStrings();
				desc.StarredList[i] = starred;
			}

			foreach (ChipCollection collection in desc.ChipCollections)
			{
				collection.UpdateDisplayStrings();
			}

			return desc;
		}

		// Get list of saved project descriptions (ordered by last save time)
		public static ProjectDescription[] LoadAllProjectDescriptions()
		{
			List<ProjectDescription> projectDescriptions = new();

			foreach (string dir in Directory.EnumerateDirectories(SavePaths.ProjectsPath))
			{
				try
				{
					string projectName = Path.GetFileName(dir);
					projectDescriptions.Add(LoadProjectDescription(projectName));
				}
				catch (Exception)
				{
					// Ignore invalid project directory
				}
			}

			projectDescriptions.Sort((a, b) => b.LastSaveTime.CompareTo(a.LastSaveTime));
			return projectDescriptions.ToArray();
		}
		
		public static ModDescription LoadModDescription(string modName)
		{
			string path = SavePaths.ModDirectory+$"\\{modName}\\manifest.json";
			if (!File.Exists(path)) throw new Exception("No mod description found at " + path);

			ModDescription desc = Serializer.DeserializeModDescription(File.ReadAllText(path));
			desc.ModName = modName; // enforce name = directory name (in case player modifies manually -- operations like deleting mods rely on this)

			return desc;
		}
		public static ModDescription[] LoadAllModDescriptions()
		{
			List<ModDescription> modDescriptions = new();

			if (!Directory.Exists(SavePaths.ModDirectory))
			{
				Directory.CreateDirectory(SavePaths.ModDirectory);
				return modDescriptions.ToArray();
			}

			foreach (string dir in Directory.EnumerateDirectories(SavePaths.ModDirectory))
			{
				try
				{
					string modName = Path.GetFileName(dir);
					modDescriptions.Add(LoadModDescription(modName));
				}
				catch (Exception)
				{
					// Ignore invalid mods
				}
			}

			modDescriptions.Sort((a, b) => string.Compare(a.Author, b.Author, StringComparison.OrdinalIgnoreCase));
			return modDescriptions.ToArray();
		}

		static ChipLibrary LoadChipLibrary(ProjectDescription projectDescription)
		{
			string chipDirectoryPath = SavePaths.GetChipsPath(projectDescription.ProjectName);
			ChipDescription[] loadedChips = new ChipDescription[projectDescription.AllCustomChipNames.Length];

			if (!Directory.Exists(chipDirectoryPath) && loadedChips.Length > 0) throw new DirectoryNotFoundException(chipDirectoryPath);

			for (int i = 0; i < loadedChips.Length; i++)
			{
				string chipPath = Path.Combine(chipDirectoryPath, projectDescription.AllCustomChipNames[i] + ".json");
				string chipSaveString = File.ReadAllText(chipPath);
				loadedChips[i] = Serializer.DeserializeChipDescription(chipSaveString);
			}

			return new ChipLibrary(loadedChips);
		}
	}
}