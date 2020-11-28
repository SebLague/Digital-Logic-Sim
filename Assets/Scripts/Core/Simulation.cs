using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour {

	public static int simulationFrame { get; private set; }

	static Simulation instance;
	InputSignal[] inputSignals;
	ChipEditor chipEditor;

	void Awake () {
		simulationFrame = 0;
	}

	void Update () {
		StepSimulation ();
	}

	void StepSimulation () {
		RefreshChipEditorReference ();

		List<ChipSignal> inputSignals = chipEditor.inputsEditor.signals;

		simulationFrame++;
		// Tell all signal generators to send their signal out
		for (int i = 0; i < inputSignals.Count; i++) {
			((InputSignal) inputSignals[i]).SendSignal ();
		}

	}

	void RefreshChipEditorReference () {
		if (chipEditor == null) {
			chipEditor = FindObjectOfType<ChipEditor> ();
		}
	}

	static Simulation Instance {
		get {
			if (!instance) {
				instance = FindObjectOfType<Simulation> ();
			}
			return instance;
		}
	}
}