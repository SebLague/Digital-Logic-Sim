public class NotGate : BuiltinChip {

	protected override void Awake () {
		base.Awake ();
	}

	protected override void ProcessOutput () {
		int outputSignal = 1 - inputPins[0].State;
		outputPins[0].ReceiveSignal (outputSignal);
	}
}