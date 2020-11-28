using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ChipSaver {

	const bool usePrettyPrint = true;

	public static void Save (ChipEditor chipEditor) {
		ChipSaveData chipSaveData = new ChipSaveData (chipEditor);

		// Generate new chip save string
		var compositeChip = new SavedChip (chipSaveData);
		string saveString = JsonUtility.ToJson (compositeChip, usePrettyPrint);

		// Generate save string for wire layout
		var wiringSystem = new SavedWireLayout (chipSaveData);
		string wiringSaveString = JsonUtility.ToJson (wiringSystem, usePrettyPrint);

		// Write to file
		string savePath = SaveSystem.GetPathToSaveFile (chipEditor.chipName);
		using (StreamWriter writer = new StreamWriter (savePath)) {
			writer.Write (saveString);
		}

		string wireLayoutSavePath = SaveSystem.GetPathToWireSaveFile (chipEditor.chipName);
		using (StreamWriter writer = new StreamWriter (wireLayoutSavePath)) {
			writer.Write (wiringSaveString);
		}
	}

}