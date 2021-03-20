using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour {

	public event System.Action<Chip> customChipCreated;
	public event System.Action<Chip> customChipUpdated;

	public ChipEditor chipEditorPrefab;
	public ChipPackage chipPackagePrefab;
	public Wire wirePrefab;
	public Chip[] builtinChips;
	public List<Chip> spawnableChips;
	public UIManager UIManager;

	ChipEditor activeChipEditor;
	int currentChipCreationIndex;
	static Manager instance;

	void Awake () {
		instance = this;
		activeChipEditor = FindObjectOfType<ChipEditor> ();
		FindObjectOfType<CreateMenu> ().onChipCreatePressed += SaveAndPackageChip;
		FindObjectOfType<UpdateButton> ().onChipUpdatePressed += UpdateChip;
	}

	void Start () {
		spawnableChips = new List<Chip>();
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

	public void ViewChip (Chip chip) {
		LoadNewEditor ();
		UIManager.ChangeState(UIManagerState.Update);

		ChipSaveData chipSaveData = ChipLoader.GetChipSaveData(chip, builtinChips, spawnableChips, wirePrefab, activeChipEditor);
		activeChipEditor.LoadFromSaveData(chipSaveData);
	}

	void SaveAndPackageChip () {
		ChipSaver.Save (activeChipEditor);
		PackageChip ();
		LoadNewEditor ();
	}

	void UpdateChip () {
		Chip updatedChip = TryPackageAndReplaceChip(activeChipEditor.chipName);
		ChipSaver.Update (activeChipEditor, updatedChip);
		LoadNewEditor ();
	}

	Chip PackageChip () {
		ChipPackage package = Instantiate (chipPackagePrefab, parent : transform);
		package.PackageCustomChip (activeChipEditor);
		package.gameObject.SetActive (false);

		Chip customChip = package.GetComponent<Chip> ();
		customChip.canBeEdited = true;
		customChipCreated?.Invoke (customChip);
		currentChipCreationIndex++;
		spawnableChips.Add(customChip);
		return customChip;
	}

	Chip TryPackageAndReplaceChip(string original) {
		ChipPackage oldPackage = Array.Find(GetComponentsInChildren<ChipPackage>(true), cp => cp.name == original);
		if (oldPackage != null) {
			Destroy(oldPackage.gameObject);
		}

		ChipPackage package = Instantiate (chipPackagePrefab, parent : transform);
		package.PackageCustomChip (activeChipEditor);
		package.gameObject.SetActive (false);

		Chip customChip = package.GetComponent<Chip> ();
		customChip.canBeEdited = true;
		int index = spawnableChips.FindIndex(c => c.chipName == original);
		if (index >= 0) {
			spawnableChips[index] = customChip;
			customChipUpdated?.Invoke(customChip);
		}

		return customChip;
	}

	void LoadNewEditor () {
		if (activeChipEditor) {
			Destroy (activeChipEditor.gameObject);
			UIManager.ChangeState(UIManagerState.Create);
		}
		activeChipEditor = Instantiate (chipEditorPrefab, Vector3.zero, Quaternion.identity);
		activeChipEditor.creationIndex = currentChipCreationIndex;
	}

	public void SpawnChip (Chip chip) {
		activeChipEditor.chipInteraction.SpawnChip (chip);
	}

	public void LoadMainMenu () {
		UnityEngine.SceneManagement.SceneManager.LoadScene (0);
	}

}