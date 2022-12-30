using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using DLS.ChipData;
using System.Collections.Generic;

namespace DLS.ChipCreation
{
	// Handles writing / reading chip save data to disk
	public static class ChipSaver
	{


		// Save chip descriptions to disk
		public static void SaveChips(ChipDescription[] chipDescriptions, string projectName)
		{
			foreach (ChipDescription d in chipDescriptions)
			{
				SaveChip(d, projectName);
			}
		}

		// Save chip description to disk
		public static void SaveChip(ChipDescription chipDescription, string projectName)
		{
			string serializedChip = SerializeChipDescription(chipDescription, true);
			WriteToFile(serializedChip, chipDescription.Name, projectName);
		}

		public static string SerializeChipDescription(ChipDescription description, bool prettyPrint)
		{
			var formatting = prettyPrint ? Formatting.Indented : Formatting.None;
			return JsonConvert.SerializeObject(description, formatting);
		}

		// Delete chip save file, with option to keep backup in a DeletedChips folder.
		public static void DeleteChip(string chipName, string projectName, bool backupInDeletedFolder = true)
		{
			string filePath = GetChipFilePath(projectName, chipName);
			if (backupInDeletedFolder)
			{
				string deletedChipDirectoryPath = SavePaths.DeletedChipsPath(projectName);
				string deletedFilePath = FileHelper.EnsureUniqueFileName(Path.Combine(deletedChipDirectoryPath, chipName + ".json"));
				SavePaths.EnsureDirectoryExists(Path.GetDirectoryName(deletedFilePath));
				File.Move(filePath, deletedFilePath);
			}
			else
			{
				File.Delete(filePath);
			}
		}

		// Loads all saved chip files for the given project name
		public static ChipDescription[] LoadAllSavedChips(string projectName)
		{
			string path = SavePaths.ChipsPath(projectName);
			List<ChipDescription> loadedChips = new();
			if (Directory.Exists(path))
			{
				string[] filePaths = Directory.GetFiles(path, "*.json");

				foreach (string filePath in filePaths)
				{
					loadedChips.Add(LoadSavedChip(filePath));
				}
			}
			return loadedChips.ToArray();
		}

		// Load a single saved chip file
		public static ChipDescription LoadSavedChip(string projectName, string chipName)
		{
			string path = Path.Combine(SavePaths.ChipsPath(projectName), chipName + ".json");
			return LoadSavedChip(path);
		}

		// Load a single saved chip file
		static ChipDescription LoadSavedChip(string path)
		{
			using (StreamReader reader = new(path))
			{
				string saveString = reader.ReadToEnd();
				ChipDescription chipDescription = JsonConvert.DeserializeObject<ChipDescription>(saveString);
				return chipDescription;
			}
		}

		static void WriteToFile(string serializedChip, string chipName, string projectName)
		{
			string saveFilePath = GetChipFilePath(projectName, chipName);
			Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath));

			using (var writer = new StreamWriter(saveFilePath))
			{
				writer.Write(serializedChip);
				Debug.Log("Saved to: " + saveFilePath);
			}
		}

		static string GetChipFilePath(string projectName, string chipName)
		{
			string saveDirectoryPath = SavePaths.ChipsPath(projectName);
			return Path.Combine(saveDirectoryPath, chipName + ".json");
		}

	}
}