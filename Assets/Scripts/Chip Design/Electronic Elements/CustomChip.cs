using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomChip : Chip {

	public bool customFullName;
	[Multiline]
	public string fullName;

	public InputSignal[] signalGenerators;
	public OutputSignal[] outputSignals;
	public Constant[] constants;
	public bool diagnose;

	public override void ReceiveInputSignal (Pin pin) {
		base.ReceiveInputSignal (pin);
		if (diagnose) {
			Debug.Log ("Input received on pin: " + pin.pinName + " index: " + pin.index + " on chip: " + chipName + " state: " + pin.State);
		}
	}

	protected override void ProcessOutput () {
		// Send signals from input pins through the chip
		for (int i = 0; i < inputPins.Length; i++) {
			signalGenerators[i].SendSignal (inputPins[i].State);
		}
		if (diagnose) {
			Debug.Log ("Processing output");
		}

		// Pass processed signals on to ouput pins
		for (int i = 0; i < outputPins.Length; i++) {
			int outputState = outputSignals[i].inputPins[0].State;
			outputPins[i].ReceiveSignal (outputState);
		}

		// Send constant signals
		for (int i = 0; i < constants.Length; i++) {
			constants[i].SendSignal ();
		}

	}

}