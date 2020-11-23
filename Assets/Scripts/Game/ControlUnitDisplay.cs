using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlUnitDisplay : MonoBehaviour {

	public string activeInstruction;
	public int activeInstructionCode;
	[TextArea ()]
	public string message;
	public int controlUnitStepCounter;
	EmulatedRAM emulatedRAM;
	public int[] valuesInRAM;

	void Update () {
		if (emulatedRAM) {
			valuesInRAM = emulatedRAM.storedValues;
		} else {
			emulatedRAM = FindObjectOfType<EmulatedRAM> ();
		}
	}

	public void Message (string message) {
		this.message = message;
	}

	public void ActiveInstruction (string instructionName) {
		activeInstruction = instructionName;
	}

	public void ActiveInstruction (int instructionCode) {
		activeInstructionCode = instructionCode;
	}
}