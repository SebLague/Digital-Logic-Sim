using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomChip : Chip {


	public InputSignal[] inputSignals;
	public OutputSignal[] outputSignals;

	// Applies wire types from signals to pins
	public void ApplyWireModes()
	{
		foreach (var (pin, sig) in inputPins.Zip(inputSignals, (x, y) => (x, y)))
		{
			pin.wireType = sig.outputPins[0].wireType;
		}
		foreach (var (pin, sig) in outputPins.Zip(outputSignals, (x, y) => (x, y)))
		{
			pin.wireType = sig.inputPins[0].wireType;
		}
	}

	public override void ReceiveInputSignal (Pin pin) {
		base.ReceiveInputSignal (pin);
	}

	protected override void ProcessOutput () {
		// Send signals from input pins through the chip
		for (int i = 0; i < inputPins.Length; i++) {
			inputSignals[i].SendSignal (inputPins[i].State);
		}

		// Pass processed signals on to ouput pins
		for (int i = 0; i < outputPins.Length; i++) {
			int outputState = outputSignals[i].inputPins[0].State;
			outputPins[i].ReceiveSignal (outputState);
		}
	}

}