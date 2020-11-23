using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmulatedPin : MonoBehaviour {
	public enum PinType { ChipInput, ChipOutput }
	public PinType pinType;

	public EmulatedChip connectedChip;

	public EmulatedPin parentPin;
	public List<EmulatedPin> childPins = new List<EmulatedPin> ();

	public Signal currentSignal;

	// Receive signal: 0 == LOW, 1 = HIGH
	// Sets the current state to the signal
	// Passes the signal on to any connected pins / electronic component
	public void ReceiveSignal (Signal signal) {
		currentSignal = signal;

		if (pinType == PinType.ChipInput) {
			connectedChip.ReceiveInputSignal (this);
		}

		foreach (EmulatedPin connectedPin in childPins) {
			connectedPin.ReceiveSignal (signal);
		}
	}
}