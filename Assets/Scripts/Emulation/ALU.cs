using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ALU {

	Signal output = new Signal (0);

	public void Input (Signal inputA, Signal inputB, Signal control) {
		switch (control) {
			case 0b0000:
				output.SetValue (0);
				break;
			case 0b0001:
				output.SetValue (inputA + inputB);
				break;
		}
	}
}