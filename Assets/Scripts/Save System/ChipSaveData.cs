using System.Collections.Generic;
using UnityEngine;

public class ChipSaveData {

	public string chipName;
	public Color chipColour;
	public Color chipNameColour;
	public int creationIndex;

	// All chips used as components in this new chip (including input and output signals)
	public Chip[] componentChips;
	// All wires in the chip (in case saving of wire layout is desired)
	public Wire[] wires;

	public ChipSaveData () {

	}

	public ChipSaveData (ChipEditor chipEditor) {
		List<Chip> componentChipList = new List<Chip> ();

		var sortedInputs = chipEditor.inputsEditor.signals;
		sortedInputs.Sort ((a, b) => b.transform.position.y.CompareTo (a.transform.position.y));
		var sortedOutputs = chipEditor.outputsEditor.signals;
		sortedOutputs.Sort ((a, b) => b.transform.position.y.CompareTo (a.transform.position.y));

		componentChipList.AddRange (sortedInputs);
		componentChipList.AddRange (sortedOutputs);

		componentChipList.AddRange (chipEditor.chipInteraction.allChips);
		componentChips = componentChipList.ToArray ();

		wires = chipEditor.pinAndWireInteraction.allWires.ToArray ();
		chipName = chipEditor.chipName;
		chipColour = chipEditor.chipColour;
		chipNameColour = chipEditor.chipNameColour;
		creationIndex = chipEditor.creationIndex;
	}

	public int ComponentChipIndex (Chip componentChip) {
		return System.Array.IndexOf (componentChips, componentChip);
	}

}