public class NotGate : BuiltinChip {

	protected override void Awake () {
		base.Awake ();
		AutoNameAllPins ();
	}

	protected override void ProcessOutput () {
		int outputSignal = 1 - inputPins[0].State;
		if (inputPins[0].State == -1) {
			outputSignal = 1;
		}
		outputPins[0].ReceiveSignal (outputSignal);
	}
}