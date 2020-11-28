using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SavedComponentChip {
	public string chipName;
	public double posX;
	public double posY;

	public SavedInputPin[] inputPins;
	public string[] outputPinNames;

	public SavedComponentChip (ChipSaveData chipSaveData, Chip chip) {
		chipName = chip.chipName;

		// Store position in doubles and limit precision to reduce space in save file
		const double precision = 10000;
		posX = ((int) (chip.transform.position.x * precision)) / precision;
		posY = ((int) (chip.transform.position.y * precision)) / precision;

		// Input pins
		inputPins = new SavedInputPin[chip.inputPins.Length];
		for (int i = 0; i < inputPins.Length; i++) {
			inputPins[i] = new SavedInputPin (chipSaveData, chip.inputPins[i]);
		}

		// Output pins (only need to store names, connections are stored on input pins only)
		outputPinNames = new string[chip.outputPins.Length];
		for (int i = 0; i < chip.outputPins.Length; i++) {
			outputPinNames[i] = chip.outputPins[i].pinName;
		}
	}

}