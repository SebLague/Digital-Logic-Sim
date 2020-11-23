using System.IO;
using UnityEngine;

public static class ChipSaver {

	const bool usePrettyPrint = true;

	public static void Save (GameObject chipHolder, string chipName, string savePath, string wireSavePath) {
		var allChips = GameObject.FindObjectsOfType<Chip> ();
		var compositeChip = new SerializableCompositeChip (chipName, allChips);
		string saveString = JsonUtility.ToJson (compositeChip, usePrettyPrint);
		

		using (StreamWriter writer = new StreamWriter (savePath)) {
			writer.Write (saveString);
		}
		
		var wiringSystem = new SerializableWiringSystem (allChips);
		string wiringSaveString = JsonUtility.ToJson (wiringSystem, usePrettyPrint);
		using (StreamWriter writer = new StreamWriter (wireSavePath)) {
			writer.Write (wiringSaveString);
		}
	}

}