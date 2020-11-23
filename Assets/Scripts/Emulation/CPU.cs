using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPU {

	RegisterOld addressRegister;
	RegisterOld dataRegister;

	Signal outputData;
	

	public CPU () {
		addressRegister = new RegisterOld ();
		dataRegister = new RegisterOld ();
	}

	// data: 16 bit signal coming from data memory
	// instruction: 16 bit signal coming from instruction memory
	// reset: 1 bit signal coming from input device
	public void Input (Signal data, Signal instruction, Signal reset) {

	}

	//public (Signal data, Signal address, Signal writeToMemory, Signal programCounter) Ouput () {
		
	//}
}

public class RegisterOld {
	Signal contents;

	public void Input (Signal inputValue, Signal writeEnable, Signal readEnable) {
		if (writeEnable == 1) {
			contents = inputValue;
		}
	}
}
