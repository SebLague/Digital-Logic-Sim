using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buffer : CustomChip {

	protected override void ProcessOutput () {

		for (int i = 0; i < outputPins.Length; i++) {
			outputPins[i].ReceiveSignal (inputPins[i].State & inputPins[4].State);
		}
	}
}