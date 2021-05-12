using System.Collections.Generic;
using UnityEngine;

public class ChipUtil : BuiltinChip{
    public static void setPins(string pinValue, Pin[] outputPins) {
		for(int i = 0; i < outputPins.Length; i++) {
			outputPins[i].ReceiveSignal(int.Parse(pinValue[i].ToString()));
		}
	}
}