using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SavedInputPin
{
	public string name;
	// An input pin receives its input from one of the output pins of some chip (called the parent chip)
	// The chipIndex is the chip's index in the array of chips being written to file
	public int parentChipIndex;
	public int parentChipOutputIndex;
	public bool isCylic;

	public SavedInputPin(ChipSaveData chipSaveData, Pin pin)
	{
		name = pin.pinName;
		isCylic = pin.cyclic;
		if (pin.parentPin)
		{
			parentChipIndex = chipSaveData.ComponentChipIndex(pin.parentPin.chip);
			parentChipOutputIndex = pin.parentPin.index;
		}
		else
		{
			parentChipIndex = -1;
			parentChipOutputIndex = -1;
		}
	}

	public SavedInputPin()
	{

	}
}