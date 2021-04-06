using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipEditor : MonoBehaviour {
	public Transform chipImplementationHolder;
	public Transform wireHolder;
	public ChipInterfaceEditor inputsEditor;
	public ChipInterfaceEditor outputsEditor;
	public ChipInteraction chipInteraction;
	public PinAndWireInteraction pinAndWireInteraction;

	[HideInInspector]
	public string chipName;
	[HideInInspector]
	public Color chipColour;
	[HideInInspector]
	public Color chipNameColour;
	[HideInInspector]
	public int creationIndex;

	void Awake () {
		InteractionHandler[] allHandlers = { inputsEditor, outputsEditor, chipInteraction, pinAndWireInteraction };
		foreach (var handler in allHandlers) {
			handler.InitAllHandlers (allHandlers);
		}

		pinAndWireInteraction.Init (chipInteraction, inputsEditor, outputsEditor);
		pinAndWireInteraction.onConnectionChanged += OnChipNetworkModified;
		GetComponentInChildren<Canvas> ().worldCamera = Camera.main;
	}

	void LateUpdate () {
		inputsEditor.OrderedUpdate ();
		outputsEditor.OrderedUpdate ();
		pinAndWireInteraction.OrderedUpdate ();
		chipInteraction.OrderedUpdate ();
	}

	void OnChipNetworkModified () {
		CycleDetector.MarkAllCycles (this);
	}

	public void LoadFromSaveData (ChipSaveData saveData) {
		chipName = saveData.chipName;
		chipColour = saveData.chipColour;
		chipNameColour = saveData.chipNameColour;
		creationIndex = saveData.creationIndex;

		// Load component chips
		for (int i = 0; i < saveData.componentChips.Length; i++) {
			Chip componentChip = saveData.componentChips[i];
			if (componentChip is InputSignal inp) {
				inp.wireType = inp.outputPins[0].wireType;
				inputsEditor.LoadSignal (inp);
			} else if (componentChip is OutputSignal outp) {
				outp.wireType = outp.inputPins[0].wireType;
				outputsEditor.LoadSignal (outp);
			} else {
				chipInteraction.LoadChip (componentChip);
			}
		}

		// Load wires
		if (saveData.wires != null) {
			for (int i = 0; i < saveData.wires.Length; i++) {
				pinAndWireInteraction.LoadWire (saveData.wires[i]);
			}
		}
	}

}