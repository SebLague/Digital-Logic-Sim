using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SavedWire {
	public int parentChipIndex;
	public int parentChipOutputIndex;
	public int childChipIndex;
	public int childChipInputIndex;
	public Vector2[] anchorPoints;

	public SavedWire (ChipSaveData chipSaveData, Wire wire) {
		Pin parentPin = wire.startPin;
		Pin childPin = wire.endPin;

		parentChipIndex = chipSaveData.ComponentChipIndex (parentPin.chip);
		parentChipOutputIndex = parentPin.index;

		childChipIndex = chipSaveData.ComponentChipIndex (childPin.chip);
		childChipInputIndex = childPin.index;

		anchorPoints = wire.anchorPoints.ToArray ();
	}
}