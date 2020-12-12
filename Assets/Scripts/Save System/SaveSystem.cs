using System.IO;
using UnityEngine;

public static class SaveSystem {

	static string activeProjectName = "Untitled";
	const string fileExtension = ".txt";

	public static void SetActiveProject (string projectName) {
		activeProjectName = projectName;
	}

	public static void Init () {
		// Create save directory (if doesn't exist already)
		Directory.CreateDirectory (CurrentSaveProfileDirectoryPath);
		Directory.CreateDirectory (CurrentSaveProfileWireLayoutDirectoryPath);
	}

	public static string[] GetChipSavePaths()
    {
		return Directory.GetFiles(CurrentSaveProfileDirectoryPath, "*" + fileExtension);
	}

	public static void LoadAll (Manager manager) {
		// Load any saved chips
		var sw = System.Diagnostics.Stopwatch.StartNew();
		ChipLoader.LoadAllChips (GetChipSavePaths(), manager);
		Debug.Log ("Load time: " + sw.ElapsedMilliseconds);
	}

	public static SavedChip[] GetAllSavedChips()
	{
		// Load any saved chips
		return ChipLoader.GetAllSavedChips(GetChipSavePaths());
	}

	public static string GetPathToSaveFile (string saveFileName) {
		return Path.Combine (CurrentSaveProfileDirectoryPath, saveFileName + fileExtension);
	}

	public static string GetPathToWireSaveFile (string saveFileName) {
		return Path.Combine (CurrentSaveProfileWireLayoutDirectoryPath, saveFileName + fileExtension);
	}

	static string CurrentSaveProfileDirectoryPath {
		get {
			return Path.Combine (SaveDataDirectoryPath, activeProjectName);
		}
	}

	static string CurrentSaveProfileWireLayoutDirectoryPath {
		get {
			return Path.Combine (CurrentSaveProfileDirectoryPath, "WireLayout");
		}
	}

	public static string[] GetSaveNames () {
		string[] savedProjectPaths = new string[0];
		if (Directory.Exists (SaveDataDirectoryPath)) {
			savedProjectPaths = Directory.GetDirectories (SaveDataDirectoryPath);
		}
		for (int i = 0; i < savedProjectPaths.Length; i++) {
			string[] pathSections = savedProjectPaths[i].Split (Path.DirectorySeparatorChar);
			savedProjectPaths[i] = pathSections[pathSections.Length - 1];
		}
		return savedProjectPaths;
	}

	public static string SaveDataDirectoryPath {
		get {
			const string saveFolderName = "SaveData";
			return Path.Combine (Application.persistentDataPath, saveFolderName);
		}
	}

}