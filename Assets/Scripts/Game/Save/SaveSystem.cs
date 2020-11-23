using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour {

	public bool loadSavedChips = true;
	public string saveProfileName = "Test";
	const string fileExtension = ".txt";

	void Start () {
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
			return Path.Combine (SaveDataDirectoryPath, saveProfileName);
		}
	}

	string CurrentSaveProfileWiringDirectoryPath {
		get {
			return Path.Combine (CurrentSaveProfileDirectoryPath, "Wiring");
		}
	}

	public static string[] GetSaveNames () {
		if (Directory.Exists (SaveDataDirectoryPath)) {
			return Directory.GetDirectories (SaveDataDirectoryPath);
		}
		return null;
	}

	public static string SaveDataDirectoryPath {
		get {
			const string saveFolderName = "SaveData";
			return Path.Combine (Application.persistentDataPath, saveFolderName);
		}
	}

}