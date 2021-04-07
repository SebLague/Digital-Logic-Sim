using UnityEngine;

public class XorGate : BuiltinChip {

	protected override void Awake () {
		base.Awake ();
	}

	protected override void ProcessOutput () {
		int outputSignal = inputPins[0].State ^ inputPins[1].State;
		outputPins[0].ReceiveSignal (outputSignal);
	}

}