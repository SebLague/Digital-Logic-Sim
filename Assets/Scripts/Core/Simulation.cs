using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour {

	public static int simulationFrame { get; private set; }

	static Simulation instance;
	InputSignal[] inputSignals;
	ChipEditor chipEditor;
	public bool active = false;

	public float minStepTime = 0.075f;
	float lastStepTime;

	public void ToogleActive() {
		active = !active;
		if (!active) { StepSimulation(); }
    }

	void Awake () {
		simulationFrame = 0;
	}
	

	void Update () {
		if (Time.time - lastStepTime > minStepTime && active) {
			lastStepTime = Time.time;
			StepSimulation ();
		}
	}

	private void ClearOutputSignals() {
		List<ChipSignal> outputSignals = chipEditor.outputsEditor.signals;
		for (int i = 0; i < outputSignals.Count; i++) {
			outputSignals[i].SetDisplayState(0);
			outputSignals[i].currentState = 0;
		}
	}

	private void ProcessInputs() {
		List<ChipSignal> inputSignals = chipEditor.inputsEditor.signals;
		for (int i = 0; i < inputSignals.Count; i++) {
			((InputSignal)inputSignals[i]).SendSignal();
		}
	}

	void StepSimulation () {
		simulationFrame++;

		RefreshChipEditorReference();

		if (active) {
			ClearOutputSignals();
			InitChips();
			ProcessInputs();
		}
        

		var allWires = chipEditor.pinAndWireInteraction.allWires;
		for (int i = 0; i < allWires.Count; i++) {
			if (!active) {
				allWires[i].tellWireSimIsOff();
			} else {
				allWires[i].tellWireSimIsOn();
			}
		}

		if (!active) { ClearOutputSignals(); }
		
	}

    private void InitChips() {
        var allChips = chipEditor.chipInteraction.allChips;
        for (int i = 0; i < allChips.Count; i++)
        {
            allChips[i].InitSimulationFrame();
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