using UnityEngine;

public class AndGate : BuiltinChip {

	protected override void Awake () {
		base.Awake ();
		AutoNameAllPins ();
	}

	protected override void ProcessOutput () {
		int outputSignal = inputPins[0].State & inputPins[1].State;
		//bool signal = (inputPins[0].State == 1 && inputPins[1].State == 1);
		//int outputSignal = (signal) ? 1 : 0;
		outputPins[0].ReceiveSignal (outputSignal);
	}

}