using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmulatedRAM : CustomChip {

	//public string[] assembly;

	public string[] storedValuesBinary;
	public int[] storedValues;

	const int dataMSBPin = 0;
	const int addressMSBPin = 4;
	const int loadEnablePin = 8;
	const int outputEnablePin = 9;

	public void LoadInstructions (int[] instructions) {
		for (int i = 0; i < instructions.Length; i++) {
			storedValues[i] = instructions[i];
		}
		UpdateBinaryDisplay ();
	}

	void UpdateBinaryDisplay () {
		for (int i = 0; i < 16; i++) {
			storedValuesBinary[i] = Assembler.BinaryStringFromByte (storedValues[i]);
		}
	}

	public override void ReceiveInputSignal (Pin pin) {
		base.ReceiveInputSignal (pin);
		//Debug.Log (pin.pinName);
		if (pin.parentPin) {
			//Debug.Log ("Input received from: " + pin.parentPin.pinName + " on pin: " + pin.pinName + "  " + pin.State);
		} else {
			//Debug.Log ("Const input on: " + pin.pinName + "  " + pin.State);
		}
	}

	protected override void ProcessOutput () {
		bool outputEnable = inputPins[outputEnablePin].State == 1;
		bool loadEnable = inputPins[loadEnablePin].State == 1;

		int address = 0;
		for (int i = 0; i < 4; i++) {
			int addressPin = addressMSBPin + i;
			int pinState = inputPins[addressPin].currentState;
			address |= pinState << (3 - i);
		}

		if (loadEnable) {
			int valueToLoad = 0;
			//storedValuesBinary[address] = "0000 ";
			for (int i = 0; i < 4; i++) {
				int dataPin = dataMSBPin + i;
				int pinState = inputPins[dataPin].currentState;
				valueToLoad |= pinState << (3 - i);
				//storedValuesBinary[address] += pinState;
			}
			if (storedValues[address] != valueToLoad) {
				storedValues[address] = valueToLoad;
				storedValuesBinary[address] = Assembler.BinaryStringFromByte (valueToLoad);
			}

		}

		if (outputEnable) {
			int valueAtAddress = storedValues[address];
			//Debug.Log (address + "  " + valueAtAddress);
			for (int i = 0; i < 8; i++) {
				int bit = valueAtAddress.GetBit (7 - i);
				outputPins[i].ReceiveSignal (bit);
			}
		} else {
			//Debug.Log("Ram output not enabled");
			for (int i = 0; i < 8; i++) {
				outputPins[i].ReceiveSignal (-1);
			}
		}
	}

}