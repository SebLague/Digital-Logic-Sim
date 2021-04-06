using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour {

	public event System.Action<Chip> customChipCreated;

	public ChipEditor chipEditorPrefab;
	public ChipPackage chipPackagePrefab;
	public Wire wirePrefab;
	public Chip[] builtinChips;

	ChipEditor activeChipEditor;
	int currentChipCreationIndex;
	static Manager instance;

	void Awake () {
		instance = this;
		activeChipEditor = FindObjectOfType<ChipEditor> ();
		FindObjectOfType<CreateMenu> ().onChipCreatePressed += SaveAndPackageChip;
	}

	void Start () {
		SaveSystem.Init ();
		SaveSystem.LoadAll (this);
	}

	public static ChipEditor ActiveChipEditor {
		get {
			return instance.activeChipEditor;
		}
	}

	public Chip LoadChip (ChipSaveData loadedChipData) {
		activeChipEditor.LoadFromSaveData (loadedChipData);
		currentChipCreationIndex = activeChipEditor.creationIndex;

		Chip loadedChip = PackageChip ();
		LoadNewEditor ();
		return loadedChip;
	}

	void SaveAndPackageChip () {

		ChipSaver.Save (activeChipEditor);
		PackageChip ();
		LoadNewEditor ();
	}

	Chip PackageChip () {
		ChipPackage package = Instantiate (chipPackagePrefab, parent : transform);
		package.PackageCustomChip (activeChipEditor);
		package.gameObject.SetActive (false);

		Chip customChip = package.GetComponent<Chip> ();
		customChipCreated?.Invoke (customChip);
		currentChipCreationIndex++;
		return customChip;
	}

	void LoadNewEditor () {
		if (activeChipEditor) {
			Destroy (activeChipEditor.gameObject);
		}
		activeChipEditor = Instantiate (chipEditorPrefab, Vector3.zero, Quaternion.identity);
		activeChipEditor.creationIndex = currentChipCreationIndex;
	}

	public void SpawnChip (Chip chip) {
		if (chip is CustomChip custom)
			custom.ApplyWireModes();

		activeChipEditor.chipInteraction.SpawnChip (chip);
	}

	public void LoadMainMenu () {
		UnityEngine.SceneManagement.SceneManager.LoadScene (0);
	}

}