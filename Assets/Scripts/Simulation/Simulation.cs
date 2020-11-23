using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour {

	public static int simulationFrame { get; private set; }

	public Transform inputSignalHolder;

	public int lastStepDurationMillis;
	static Simulation instance;
	Player player;
	InputSignal[] inputSignals;
	Constant[] constantSignals;

	void Awake () {
		simulationFrame = 0;
	}

	public static void SimulationModified (Player player) {
		Instance.player = player;
		Instance.inputSignals = FindObjectsOfType<InputSignal> ();
		Instance.constantSignals = player.chipHolder.GetComponentsInChildren<Constant> (includeInactive: true);
	}

	void Update () {

		GetInputSignals ();

		var simulationTimer = System.Diagnostics.Stopwatch.StartNew ();
		StepSimulation ();
		lastStepDurationMillis = (int) simulationTimer.ElapsedMilliseconds;

	}

	void StepSimulation () {
		simulationFrame++;
		// Tell all signal generators to send their signal out
		if (inputSignals != null) {
			for (int i = 0; i < inputSignals.Length; i++) {
				inputSignals[i].SendSignal ();
			}
		}

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