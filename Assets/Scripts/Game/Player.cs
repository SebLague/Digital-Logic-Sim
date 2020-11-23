using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public ChipTemplate chipTemplatePrefab;
	public Chip[] builtinChips;

	public event System.Action<Chip> onChipManufactured;

	public GameObject chipHolder { get; private set; }
	GameObject customChipTemplatesHolder;

	public int numCustomChips;

	public List<Chip> allCyclicChips { get; private set; }

	static Player instance;
	public bool smallMode;

	public static bool SmallMode {
		get {
			return Instance.smallMode;
		}
	}

	static Player Instance {
		get {
			if (instance == null) {
				instance = FindObjectOfType<Player> ();
			}
			return instance;
		}
	}

	void Awake () {

		CreateChipHolder ();

		customChipTemplatesHolder = new GameObject ("Custom Chip Templates");
		customChipTemplatesHolder.transform.parent = transform;
	}

	public void ChipLoadingComplete () {
		FindObjectOfType<SavedChipEditor> ().CaptureLoadedChip (chipHolder);
		SimulationModified ();
	}

	// 
	public Chip ManufactureChip (GameObject customChipHolder, string chipName, bool saveToFile) {
		if (chipHolder != customChipHolder) {
			Destroy (chipHolder);
			chipHolder = customChipHolder;
		}
		return ManufactureChip (chipName, saveToFile);
	}

	public Chip ManufactureChip (string chipName, bool saveToFile) {
		if (saveToFile) {
			// Ensure all cycles are marked
			CycleDetector.MarkAllCycles (chipHolder);
			// Save the chip to file
			FindObjectOfType<SaveSystem> ().SaveChip (chipHolder, chipName);
		} else {
			var savedChipEditor = FindObjectOfType<SavedChipEditor> ();
			if (savedChipEditor.loadInEditMode && savedChipEditor.chipToEditName == chipName) {
				FindObjectOfType<SavedChipEditor> ().Load (chipName, chipHolder, this);
			}
		}

		ChipTemplate template = Instantiate (chipTemplatePrefab);
		Chip manufacturedChip = template.Create (chipHolder, chipName, !saveToFile);

		// Keep this chip in reserve as a template to spawn all future chips of this kind
		manufacturedChip.gameObject.SetActive (false);
		manufacturedChip.transform.parent = customChipTemplatesHolder.transform;

		// Manufacturer swallows the chip holder, so create a new one for next chip
		CreateChipHolder ();

		// Notify anyone who cares that a new chip has been manufactured
		if (onChipManufactured != null) {
			onChipManufactured.Invoke (manufacturedChip);
		}

		numCustomChips++;

		// Return the manufactured chip
		return manufacturedChip;
	}

	public void SimulationModified () {
		allCyclicChips = CycleDetector.MarkAllCycles (chipHolder);
		Simulation.SimulationModified (this);
	}

	// Create the empty object to which all elements of the work-in-progress chip design will be parented
	void CreateChipHolder () {
		chipHolder = new GameObject ("Chip Holder");
	}

}