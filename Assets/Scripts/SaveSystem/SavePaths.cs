using System.IO;
using UnityEngine;

namespace DLS.SaveSystem
{
	public static class SavePaths
	{
		const bool UseBuildPathInEditor = false;

		public const string ProjectFileName = "ProjectDescription.json";
		public static readonly string dataPath_Build = Application.persistentDataPath;
		static readonly string dataPath_Editor = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TestData");
		public static readonly string AllData = Application.isEditor && !UseBuildPathInEditor ? dataPath_Editor : dataPath_Build;

		// Path to save folder for all projects
		public static readonly string ProjectsPath = Path.Combine(AllData, "Projects");
		public static readonly string DeletedProjectsPath = Path.Combine(AllData, "Deleted Projects");
		public static readonly string AppSettingsPath = Path.Combine(AllData, "AppSettings.json");

		public static void EnsureDirectoryExists(string directoryPath) => Directory.CreateDirectory(directoryPath);

		// ---- Path to save folder for a specific project ----
		public static string GetProjectPath(string projectName) => Path.Combine(ProjectsPath, projectName);
		public static string GetDeletedProjectPath(string projectName) => Path.Combine(DeletedProjectsPath, projectName);
		public static string GetChipsPath(string projectName) => Path.Combine(GetProjectPath(projectName), "Chips");
		public static string GetDeletedChipsPath(string projectName) => Path.Combine(GetProjectPath(projectName), "Deleted Chips");
		public static string GetProjectDescriptionPath(string projectName) => Path.Combine(GetProjectPath(projectName), ProjectFileName);
	}
}