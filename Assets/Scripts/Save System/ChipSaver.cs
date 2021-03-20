using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using UnityEngine;
using System;

public static class ChipSaver {

	const bool usePrettyPrint = true;

	public static void Save(ChipEditor chipEditor)
	{
		ChipSaveData chipSaveData = new ChipSaveData(chipEditor);

		// Generate new chip save string
		var compositeChip = new SavedChip(chipSaveData);
		string saveString = JsonUtility.ToJson(compositeChip, usePrettyPrint);

		// Generate save string for wire layout
		var wiringSystem = new SavedWireLayout(chipSaveData);
		string wiringSaveString = JsonUtility.ToJson(wiringSystem, usePrettyPrint);

		// Write to file
		string savePath = SaveSystem.GetPathToSaveFile(chipEditor.chipName);
		using (StreamWriter writer = new StreamWriter(savePath))
		{
			writer.Write(saveString);
		}

		string wireLayoutSavePath = SaveSystem.GetPathToWireSaveFile(chipEditor.chipName);
		using (StreamWriter writer = new StreamWriter(wireLayoutSavePath))
		{
			writer.Write(wiringSaveString);
		}
	}

	public static void Update(ChipEditor chipEditor, Chip chip)
	{
		ChipSaveData chipSaveData = new ChipSaveData(chipEditor);

		// Generate new chip save string
		var compositeChip = new SavedChip(chipSaveData);
		string saveString = JsonUtility.ToJson(compositeChip, usePrettyPrint);

		// Generate save string for wire layout
		var wiringSystem = new SavedWireLayout(chipSaveData);
		string wiringSaveString = JsonUtility.ToJson(wiringSystem, usePrettyPrint);

		// Write to file
		string savePath = SaveSystem.GetPathToSaveFile(chipEditor.chipName);
		using (StreamWriter writer = new StreamWriter(savePath))
		{
			writer.Write(saveString);
		}

		string wireLayoutSavePath = SaveSystem.GetPathToWireSaveFile(chipEditor.chipName);
		using (StreamWriter writer = new StreamWriter(wireLayoutSavePath))
		{
			writer.Write(wiringSaveString);
		}

		// Update parent chips using this chip
		string currentChipName = chipEditor.chipName;
		SavedChip[] savedChips = SaveSystem.GetAllSavedChips();
		for (int i = 0; i < savedChips.Length; i++)
		{
			if (savedChips[i].componentNameList.Contains(currentChipName))
			{
				int currentChipIndex = Array.FindIndex(savedChips[i].savedComponentChips, c => c.chipName == currentChipName);
				SavedComponentChip updatedComponentChip = new SavedComponentChip(chipSaveData, chip);
				SavedComponentChip oldComponentChip = savedChips[i].savedComponentChips[currentChipIndex];

				// Update component chip I/O
				for (int j = 0; j < updatedComponentChip.inputPins.Length; j++) {
					for (int k = 0; k < oldComponentChip.inputPins.Length; k++) {
						if (updatedComponentChip.inputPins[j].name == oldComponentChip.inputPins[k].name) {
							updatedComponentChip.inputPins[j].parentChipIndex = oldComponentChip.inputPins[k].parentChipIndex;
							updatedComponentChip.inputPins[j].parentChipOutputIndex = oldComponentChip.inputPins[k].parentChipOutputIndex;
							updatedComponentChip.inputPins[j].isCylic = oldComponentChip.inputPins[k].isCylic;
						}
					}
				}

				// Write to file
				string parentSaveString = JsonUtility.ToJson(savedChips[i], usePrettyPrint);
				string parentSavePath = SaveSystem.GetPathToSaveFile(savedChips[i].name);
				using (StreamWriter writer = new StreamWriter(parentSavePath))
				{
					writer.Write(parentSaveString);
				}
			}
		}

	}

	public static void EditSavedChip(SavedChip savedChip, ChipSaveData chipSaveData)
    {
		
	}

	public static bool IsSafeToDelete(string chipName)
	{
		if (chipName == "AND" || chipName == "NOT"  || chipName == "OR"  || chipName == "XOR")
		{
			return false;
		}
		SavedChip[] savedChips = SaveSystem.GetAllSavedChips();
		for (int i = 0; i < savedChips.Length; i++)
		{
			if (savedChips[i].componentNameList.Contains(chipName))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsSignalSafeToDelete(string chipName, string signalName)
	{
		SavedChip[] savedChips = SaveSystem.GetAllSavedChips();
		for (int i = 0; i < savedChips.Length; i++)
		{
			if (savedChips[i].componentNameList.Contains(chipName))
			{
				SavedChip parentChip = savedChips[i];
				int currentChipIndex = Array.FindIndex(parentChip.savedComponentChips, scc => scc.chipName == chipName);
				SavedComponentChip currentChip = parentChip.savedComponentChips[currentChipIndex];
				int currentSignalIndex = Array.FindIndex(currentChip.outputPinNames, name => name == signalName);

				if (Array.Find(currentChip.inputPins, pin => pin.name == signalName && pin.parentChipIndex >= 0) != null) {
					return false;
				} else if (currentSignalIndex >= 0 && parentChip.savedComponentChips.Any(scc => scc.inputPins.Any(pin => pin.parentChipIndex == currentChipIndex && pin.parentChipOutputIndex == currentSignalIndex))) {
					return false;
				}
			}
		}
		return true;
	}

	public static void Delete(string chipName)
	{
		File.Delete(SaveSystem.GetPathToSaveFile(chipName));
		File.Delete(SaveSystem.GetPathToWireSaveFile(chipName));
	}

	public static void Rename(string oldChipName, string newChipName)
	{
		if (oldChipName == newChipName)
        {
			return;
        }
		SavedChip[] savedChips = SaveSystem.GetAllSavedChips();
		for (int i = 0; i < savedChips.Length; i++)
		{
			bool changed = false;
			if (savedChips[i].name == oldChipName)
			{
				savedChips[i].name = newChipName;
				changed = true;
			}
			for (int j = 0; j < savedChips[i].componentNameList.Length; j++)
			{
				string componentName = savedChips[i].componentNameList[j];
				if (componentName == oldChipName)
				{
					savedChips[i].componentNameList[j] = newChipName;
					changed = true;
				}
			}
			for (int j = 0; j < savedChips[i].savedComponentChips.Length; j++)
			{
				string componentChipName = savedChips[i].savedComponentChips[j].chipName;
				if (componentChipName == oldChipName)
				{
					savedChips[i].savedComponentChips[j].chipName = newChipName;
					changed = true;
				}
				
			}
			if (changed)
            {
				string saveString = JsonUtility.ToJson(savedChips[i], usePrettyPrint);
				// Write to file
				string savePath = SaveSystem.GetPathToSaveFile(savedChips[i].name);
				using (StreamWriter writer = new StreamWriter(savePath))
				{
					writer.Write(saveString);
				}
			}
		}
		// Rename wire layer file
		string oldWireSaveFile = SaveSystem.GetPathToWireSaveFile(oldChipName);
		string newWireSaveFile = SaveSystem.GetPathToWireSaveFile(newChipName);
        try
        {
			System.IO.File.Move(oldWireSaveFile, newWireSaveFile);
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogError(e);
		}
		// Delete old chip save file
		File.Delete(SaveSystem.GetPathToSaveFile(oldChipName));
	}
}