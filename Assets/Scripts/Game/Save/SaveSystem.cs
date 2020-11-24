using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour {

	public static string activeProjectName = "Test";
	const string fileExtension = ".txt";

	public bool loadSavedChips = true;

	void Start () {
		Debug.Log (activeProjectName);
		// Create save directory (if doesn't exist already)
		Directory.CreateDirectory (CurrentSaveProfileDirectoryPath);
		Directory.CreateDirectory (CurrentSaveProfileWiringDirectoryPath);

		// Load any saved chips
		if (loadSavedChips) {
			var sw = System.Diagnostics.Stopwatch.StartNew ();
			string[] chipSavePaths = Directory.GetFiles (CurrentSaveProfileDirectoryPath, "*" + fileExtension);
			ChipLoader.LoadAllChips (chipSavePaths, FindObjectOfType<Player> ());
			Debug.Log ("Load time: " + sw.ElapsedMilliseconds);
		}
	}

	public static void SetActiveProject (string projectName) {
		// Create save directory (if doesn't exist already)
		Directory.CreateDirectory (SaveDataDirectoryPath);
		//var writer = new System.IO.StreamWriter()
	}

	public void SaveChip (GameObject chipHolder, string chipName) {
		ChipSaver.Save (chipHolder, chipName, GetPathToSaveFile (chipName), GetPathToWireSaveFile (chipName));
	}

	public string GetPathToSaveFile (string saveFileName) {
		return Path.Combine (CurrentSaveProfileDirectoryPath, saveFileName + fileExtension);
	}

	public string GetPathToWireSaveFile (string saveFileName) {
		return Path.Combine (CurrentSaveProfileWiringDirectoryPath, saveFileName + fileExtension);
	}

	string CurrentSaveProfileDirectoryPath {
		get {
			return Path.Combine (SaveDataDirectoryPath, activeProjectName);
		}
	}

	string CurrentSaveProfileWiringDirectoryPath {
		get {
			return Path.Combine (CurrentSaveProfileDirectoryPath, "Wiring");
		}
	}

	public static string[] GetSaveNames () {
		string[] savedProjectPaths = new string[0];
		if (Directory.Exists (SaveDataDirectoryPath)) {
			savedProjectPaths = Directory.GetDirectories (SaveDataDirectoryPath);
		}
		for (int i = 0; i < savedProjectPaths.Length; i++) {
			string[] pathSections = savedProjectPaths[i].Split ('/');
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