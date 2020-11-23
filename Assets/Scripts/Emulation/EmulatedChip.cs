using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EmulatedChip : MonoBehaviour {

	public EmulatedPin[] inputPins;
	public EmulatedPin[] outputPins;

	int lastEmulatedFrame;
	int numInputSignalsReceived;

	public virtual void ReceiveInputSignal (EmulatedPin pin) {
		// Reset if on new step of simulation
		if (lastEmulatedFrame != Emulator.emulationFrame) {
			lastEmulatedFrame = Emulator.emulationFrame;
			numInputSignalsReceived = 0;
		}

		numInputSignalsReceived++;

		//if (numInputSignalsReceived == 1) {
		//ProcessCycleAndUnconnectedInputs ();
		//}

		if (numInputSignalsReceived == inputPins.Length) {
			ProcessOutput ();
		}
	}

	public virtual void ProcessOutput () {

	}

}