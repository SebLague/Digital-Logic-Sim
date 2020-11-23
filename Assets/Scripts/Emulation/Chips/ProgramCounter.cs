using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Inputs:
// Address (15 bit)
// Load (1 bit)

// Behaviour:
// If load == 1, then store and output given address
// Otherwise, output the stored address and increment it
public class ProgramCounter : EmulatedChip {

	Signal storedAddress;

	public override void ProcessOutput () {
		storedAddress.value++;
		outputPins[0].ReceiveSignal (storedAddress);
	}

}