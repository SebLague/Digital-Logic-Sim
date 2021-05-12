using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour {

	public static int simulationFrame { get; private set; }

	static Simulation instance;
	InputSignal[] inputSignals;
	ChipEditor chipEditor;
	public static bool active = false;

	public float minStepTime = 0.075f;
	float lastStepTime;

	public void ToogleActive() {
		// Method called by the "Run/Stop" button that toogles simulation active/inactive
		active = !active;

		simulationFrame++;
		if (active) {
			ResumeSimulation();
		} else {
			StopSimulation();
		}
    }

	void Awake () {
		simulationFrame = 0;
	}
	

	void Update () {

		// If simulation is off StepSimulation is not executed. 
		if (Time.time - lastStepTime > minStepTime && active) {
			lastStepTime = Time.time;
			simulationFrame++;
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

	void StopSimulation() {
		RefreshChipEditorReference();

		var allWires = chipEditor.pinAndWireInteraction.allWires;
		for (int i = 0; i < allWires.Count; i++) {
			// Tell all wires the simulation is inactive makes them all inactive (gray colored)
			allWires[i].tellWireSimIsOff();
		}

		// If sim is not active all output signals are set with a temporal value of 0
		// (group signed/unsigned displayed value) and get gray colored (turned off)
		ClearOutputSignals();
	}

	void ResumeSimulation() {
		StepSimulation();

		var allWires = chipEditor.pinAndWireInteraction.allWires;
		for (int i = 0; i < allWires.Count; i++)
		{
			// Tell all wires the simulation is active makes them all active (dynamic colored based on the circuits logic)
			allWires[i].tellWireSimIsOn();
		}
	}

	void StepSimulation () {
		RefreshChipEditorReference();
		ClearOutputSignals();
		InitChips();
		ProcessInputs();		
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