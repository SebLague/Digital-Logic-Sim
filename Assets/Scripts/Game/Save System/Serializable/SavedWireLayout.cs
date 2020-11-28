using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SavedWireLayout {

	public SavedWire[] serializableWires;

	public SavedWireLayout (ChipSaveData chipSaveData) {
		Wire[] allWires = chipSaveData.wires;
		serializableWires = new SavedWire[allWires.Length];

		for (int i = 0; i < allWires.Length; i++) {
			serializableWires[i] = new SavedWire (chipSaveData, allWires[i]);
		}

	}
}