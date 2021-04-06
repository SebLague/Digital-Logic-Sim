using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SavedOutputPin {
	public string name;
	public Pin.WireType wireType;

	public SavedOutputPin (ChipSaveData chipSaveData, Pin pin) {
		name = pin.pinName;
		wireType = pin.wireType;
	}
}