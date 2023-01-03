using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using DLS.ChipData;

namespace DLS.ChipCreation
{
	public static class ProjectSettingsLoader
	{
		static string ProjectSettingsSavePath(string projectName) => Path.Combine(SavePaths.ProjectPath(projectName), "ProjectSettings.json");

		public static bool TryLoadProjectSettings(string projectName, out ProjectSettings projectSettings)
		{
			string path = ProjectSettingsSavePath(projectName);

			if (File.Exists(path))
			{
				using (StreamReader reader = new(path))
				{
					string saveString = reader.ReadToEnd();
					projectSettings = JsonConvert.DeserializeObject<ProjectSettings>(saveString);
					return true;
				}
			}
			projectSettings = GetDefaultProjectSettings(projectName); ;
			return false;
		}

		public static ProjectSettings LoadProjectSettings(string projectName)
		{
			TryLoadProjectSettings(projectName, out ProjectSettings settings);
			return settings;
		}

		public static ProjectSettings[] LoadAllProjectSettings()
		{
			List<ProjectSettings> allProjectSettings = new();

			string savePath = SavePaths.ProjectsPath;
			SavePaths.EnsureDirectoryExists(SavePaths.ProjectsPath);
			string[] projectPaths = Directory.GetDirectories(savePath);
			foreach (string projectPath in projectPaths)
			{
				string projectName = Path.GetFileName(projectPath);
				//Debug.Log()
				if (TryLoadProjectSettings(Path.GetFileNameWithoutExtension(projectName), out ProjectSettings projectSettings))
				{
					allProjectSettings.Add(projectSettings);
				}

			}
			return allProjectSettings.ToArray();
		}

		public static void SaveProjectSettings(ProjectSettings settings)
		{
			string path = ProjectSettingsSavePath(settings.ProjectName);
			string saveString = JsonConvert.SerializeObject(settings, Formatting.Indented);
			Directory.CreateDirectory(Path.GetDirectoryName(path));

			using (StreamWriter writer = new StreamWriter(path))
			{
				writer.Write(saveString);
			}
		}

		public static void CreateProject(string projectName)
		{
			ProjectSettings projectSettings = GetDefaultProjectSettings(projectName);
			SaveProjectSettings(projectSettings);
		}

		public static void DeleteProject(string projectName)
		{

			string path = SavePaths.ProjectPath(projectName);
			string deleteDirectoryPath = SavePaths.DeletedProjectsPath;
			Directory.CreateDirectory(deleteDirectoryPath);

			string moveToPath = FileHelper.EnsureUniqueDirectoryName(Path.Combine(deleteDirectoryPath, projectName));
			Directory.Move(path, moveToPath);

			Debug.Log(path);
			Debug.Log(moveToPath);
		}

		public static void CreateCopyOfProject(string originalName, string copyName)
		{
			string newPath = SavePaths.ProjectPath(copyName);
			Directory.CreateDirectory(newPath);


			FileHelper.CopyDirectory(SavePaths.ProjectPath(originalName), newPath, true);
			ProjectSettings newProjectSettings = LoadProjectSettings(copyName);
			newProjectSettings.CreationTime = System.DateTime.Now;
			newProjectSettings.UpdateProjectName(copyName, autosave: false);
			newProjectSettings.Save();
		}

		public static void RenameProject(string originalName, string newName)
		{
			string pathOld = SavePaths.ProjectPath(originalName);
			string pathNew = SavePaths.ProjectPath(newName);
			Directory.Move(pathOld, pathNew);

			ProjectSettings projectSettings = LoadProjectSettings(newName);
			projectSettings.UpdateProjectName(newName);
		}


		static ProjectSettings GetDefaultProjectSettings(string projectName)
		{
			ProjectSettings settings = new ProjectSettings(projectName);
			settings.SetStarredState(BuiltinChipNames.AndChip, true, autosave: false);
			settings.SetStarredState(BuiltinChipNames.NotChip, true, autosave: false);

			DisplayOptions displayOptions = new DisplayOptions()
			{
				MainChipPinNameDisplayMode = DisplayOptions.PinNameDisplayMode.Hover,
				SubChipPinNameDisplayMode = DisplayOptions.PinNameDisplayMode.Hover,
				ShowCursorGuide = DisplayOptions.ToggleState.Off
			};
			settings.CreationTime = System.DateTime.Now;
			settings.UpdateDisplayOptions(displayOptions, autosave: false);
			settings.BuildVersion = Application.version;
			return settings;
		}

	}
}
