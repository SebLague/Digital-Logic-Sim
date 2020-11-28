using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour {

	public static int simulationFrame { get; private set; }

	public Transform inputSignalHolder;

	public int lastStepDurationMillis;
	static Simulation instance;
	InputSignal[] inputSignals;
	Constant[] constantSignals;
	ChipEditor chipEditor;

	void Awake () {
		simulationFrame = 0;
	}

	void Update () {

		//GetInputSignals ();

		var simulationTimer = System.Diagnostics.Stopwatch.StartNew ();
		StepSimulation ();
		lastStepDurationMillis = (int) simulationTimer.ElapsedMilliseconds;

	}

	void StepSimulation () {
		RefreshChipEditorReference ();

		List<ChipSignal> inputSignals = chipEditor.inputsEditor.signals;

		simulationFrame++;
		// Tell all signal generators to send their signal out

		for (int i = 0; i < inputSignals.Count; i++) {
			((InputSignal) inputSignals[i]).SendSignal ();
		}

		/*
		// Tell all constants to send their signal out
		if (constantSignals != null) {
			for (int i = 0; i < constantSignals.Length; i++) {
				constantSignals[i].SendSignal ();
			}
		}
	

		if (player != null) {
			foreach (var cyclicChip in player.allCyclicChips) {
				cyclicChip.InitSimulationFrame ();
			}
		}
			*/
	}

	void RefreshChipEditorReference () {
		if (chipEditor == null) {
			chipEditor = FindObjectOfType<ChipEditor> ();
		}
	}

	void GetInputSignals () {
		inputSignals = inputSignalHolder.GetComponentsInChildren<InputSignal> ();
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