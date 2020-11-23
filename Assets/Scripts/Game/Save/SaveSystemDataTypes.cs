using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
// Composite chip is a custom chip made up from other chips ("components")
public class SerializableCompositeChip {
	public string chipName;
	// List of unique chip names used as components in this chip
	public string[] componentNameList;
	// Data about all the chips used as components in this chip (positions, connections, etc)
	public SerializableChip[] components;
	public int loadOrder;

	public SerializableCompositeChip (string chipName, Chip[] allChips) {
		loadOrder = GameObject.FindObjectOfType<Player> ().numCustomChips;
		this.chipName = chipName;

		// Create list of (unique) names of all chips used to make this chip
		List<string> usedChipsList = new List<string> ();
		HashSet<string> usedChipsHash = new HashSet<string> ();
		for (int i = 0; i < allChips.Length; i++) {
			string usedChipName = allChips[i].chipName;
			if (!usedChipsHash.Contains (usedChipName)) {
				usedChipsHash.Add (usedChipName);
				usedChipsList.Add (usedChipName);
			}
		}
		componentNameList = usedChipsList.ToArray ();

		// Store index of chip in array inside the chip, to avoid having to search through array to find index of chip when needed
		// (speeds up save system)
		for (int i = 0; i < allChips.Length; i++) {
			allChips[i].chipSaveIndex = i;
		}

		// Create serializable chips
		components = new SerializableChip[allChips.Length];
		for (int i = 0; i < allChips.Length; i++) {
			var chip = allChips[i];
			components[i] = new SerializableChip (chip, allChips);
		}
	}
}

[System.Serializable]
public class SerializableChip {
	public string chipName;
	public double posX;
	public double posY;
	public int order;

	public SerializablePin[] inputPins;
	public string[] outputPinNames;

	public SerializableChip (Chip chip, Chip[] allChips) {
		chipName = chip.chipName;
		order = chip.order;

		// Store position in doubles and limit precision to reduce space in save file
		const double precision = 10000;
		posX = ((int) (chip.transform.position.x * precision)) / precision;
		posY = ((int) (chip.transform.position.y * precision)) / precision;

		inputPins = new SerializablePin[chip.inputPins.Length];

		for (int i = 0; i < inputPins.Length; i++) {
			inputPins[i] = new SerializablePin (chip.inputPins[i], allChips);
		}

		outputPinNames = new string[chip.outputPins.Length];
		for (int i = 0; i < chip.outputPins.Length; i++) {
			outputPinNames[i] = chip.outputPins[i].pinName;
		}
	}

}

[System.Serializable]
public class SerializablePin {
	public string name;
	// A pin receives input from one of the output pins of some chip (called the parent chip)
	public int parentChipIndex;
	public int parentChipOutputIndex;
	public bool isCylic;

	public SerializablePin (Pin pin, Chip[] allChips) {
		name = pin.pinName;
		isCylic = pin.cyclic;
		if (pin.parentPin) {
			parentChipIndex = pin.parentPin.chip.chipSaveIndex;
			parentChipOutputIndex = pin.parentPin.index;
		} else {
			parentChipIndex = -1;
			parentChipOutputIndex = -1;
		}
	}
}

[System.Serializable]
public class SerializableWiringSystem {

	public SerializableWire[] wires;

	public SerializableWiringSystem (Chip[] allChips) {
		Player player = GameObject.FindObjectOfType<Player> ();
		PinAndWireInteraction wireInteraction = GameObject.FindObjectOfType<PinAndWireInteraction> ();

		var wireList = new List<SerializableWire> ();
		for (int i = 0; i < allChips.Length; i++) {
			foreach (Pin childPin in allChips[i].inputPins) {
				Wire wire = wireInteraction.GetWire (childPin);
				if (wire) {
					wireList.Add (new SerializableWire (wire));
				}
			}
		}
		wires = wireList.ToArray ();
	}
}

[System.Serializable]
public class SerializableWire {
	public int parentChipIndex;
	public int parentChipOutputIndex;
	public int childChipIndex;
	public int childChipInputIndex;
	public Vector2[] anchorPoints;

	public SerializableWire (Wire wire) {
		Pin parentPin = wire.startPin;
		Pin childPin = wire.endPin;

		parentChipIndex = parentPin.chip.chipSaveIndex;
		parentChipOutputIndex = parentPin.index;

		childChipIndex = childPin.chip.chipSaveIndex;
		childChipInputIndex = childPin.index;

		anchorPoints = wire.anchorPoints.ToArray ();
	}
}