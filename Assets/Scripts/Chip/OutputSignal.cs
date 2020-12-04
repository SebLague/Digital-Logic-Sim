using UnityEngine;

// Output signal of a chip.
public class OutputSignal : ChipSignal {

	protected override void Start () {
		base.Start ();
		SetDisplayState (0);
	}

	public override void ReceiveInputSignal (Pin inputPin) {
		currentState = inputPin.State;
		SetDisplayState (inputPin.State);
	}

	public override void UpdateSignalName (string newName) {
		base.UpdateSignalName (newName);
		inputPins[0].pinName = newName;
	}

}