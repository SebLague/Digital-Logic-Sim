using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ChipLoader
{

	public static void LoadAllChips(string[] chipPaths, Manager manager)
	{
		var savedChips = new List<SavedChip>();

		// Read saved chips from file
		for (int i = 0; i < chipPaths.Length; i++)
		{
			using (StreamReader reader = new StreamReader(chipPaths[i]))
			{
				string chipSaveString = reader.ReadToEnd();
				savedChips.Add(JsonUtility.FromJson<SavedChip>(chipSaveString));
			}
		}

		// Maintain dictionary of loaded chips (initially just the built-in chips)
		Dictionary<string, Chip> loadedChips = new Dictionary<string, Chip>();
		string[] builtinChipNames = new string[manager.builtinChips.Length];
		for (int i = 0; i < manager.builtinChips.Length; i++)
		{
			Chip builtinChip = manager.builtinChips[i];
			loadedChips.Add(builtinChip.chipName, builtinChip);
			builtinChipNames[i] = builtinChip.chipName;
		}

		//SortChipsByOrderOfCreation(ref savedChips, builtinChipNames);
		foreach (var c in savedChips)
		{
			//Debug.Log(c.name);
		}

		int numFailedLoads = 0;

		for (int i = 0; i < savedChips.Count; i++)
		{
			SavedChip chipToTryLoad = savedChips[i];
			if (CanLoad(savedChips[i], loadedChips))
			{
				ChipSaveData loadedChipData = LoadChip(chipToTryLoad, loadedChips, manager.wirePrefab);
				Chip loadedChip = manager.LoadChip(loadedChipData);
				loadedChips.Add(loadedChip.chipName, loadedChip);
			}
			else
			{
				// Hackkyy
				if (numFailedLoads > savedChips.Count * 10)
				{
					Debug.LogError("Failed to resolve chip load order.");
					for (int j = i; j < savedChips.Count; j++)
					{
						CanLoad(savedChips[j], loadedChips, true);
					}
					return;
				}
				savedChips.RemoveAt(i);
				savedChips.Add(chipToTryLoad);
				i--;
				numFailedLoads++;

			}
		}
	}

	static bool CanLoad(SavedChip chipToLoad, Dictionary<string, Chip> previouslyLoadedChips, bool debug = false)
	{
		for (int i = 0; i < chipToLoad.savedComponentChips.Length; i++)
		{
			SavedComponentChip componentToLoad = chipToLoad.savedComponentChips[i];
			if (!previouslyLoadedChips.ContainsKey(componentToLoad.chipName))
			{
				if (debug)
				{
					Debug.Log("Failed: " + chipToLoad.name + " as component not yet loaded: " + componentToLoad.chipName);
				}
				return false;

			}
		}
		return true;
	}

	// Instantiates all components that make up the given clip, and connects them up with wires
	// The components are parented under a single "holder" object, which is returned from the function
	static ChipSaveData LoadChip(SavedChip chipToLoad, Dictionary<string, Chip> previouslyLoadedChips, Wire wirePrefab)
	{
		Debug.Log("Load " + chipToLoad.name);
		ChipSaveData loadedChipData = new ChipSaveData();
		int numComponents = chipToLoad.savedComponentChips.Length;
		loadedChipData.componentChips = new Chip[numComponents];
		loadedChipData.chipName = chipToLoad.name;
		loadedChipData.chipColour = chipToLoad.colour;
		loadedChipData.chipNameColour = chipToLoad.nameColour;
		loadedChipData.creationIndex = chipToLoad.creationIndex;

		// Spawn component chips (the chips used to create this chip)
		// These will have been loaded already, and stored in the previouslyLoadedChips dictionary
		for (int i = 0; i < numComponents; i++)
		{
			SavedComponentChip componentToLoad = chipToLoad.savedComponentChips[i];
			string componentName = componentToLoad.chipName;
			Vector2 pos = new Vector2((float)componentToLoad.posX, (float)componentToLoad.posY);

			if (!previouslyLoadedChips.ContainsKey(componentName))
			{
				Debug.LogError("Failed to load sub component: " + componentName + " While loading " + chipToLoad.name);
			}

			Chip loadedComponentChip = GameObject.Instantiate(previouslyLoadedChips[componentName], pos, Quaternion.identity);
			loadedChipData.componentChips[i] = loadedComponentChip;

			// Load input pin names
			for (int inputIndex = 0; inputIndex < componentToLoad.inputPins.Length; inputIndex++)
			{
				loadedChipData.componentChips[i].inputPins[inputIndex].pinName = componentToLoad.inputPins[inputIndex].name;
			}

			// Load output pin names
			for (int ouputIndex = 0; ouputIndex < componentToLoad.outputPinNames.Length; ouputIndex++)
			{
				loadedChipData.componentChips[i].outputPins[ouputIndex].pinName = componentToLoad.outputPinNames[ouputIndex];
			}
		}

		// Connect pins with wires
		for (int chipIndex = 0; chipIndex < chipToLoad.savedComponentChips.Length; chipIndex++)
		{
			Chip loadedComponentChip = loadedChipData.componentChips[chipIndex];
			for (int inputPinIndex = 0; inputPinIndex < loadedComponentChip.inputPins.Length; inputPinIndex++)
			{
				SavedInputPin savedPin = new SavedInputPin();
				if (inputPinIndex >= chipToLoad.savedComponentChips[chipIndex].inputPins.Length)
				{

					Debug.Log(loadedComponentChip.chipName + "  " + inputPinIndex + "  Input pins: " + loadedComponentChip.inputPins.Length + "   SAved pins: " + chipToLoad.savedComponentChips[chipIndex].inputPins.Length);
					savedPin.name = "IN " + (inputPinIndex + 1);
					savedPin.isCylic = false;
					savedPin.parentChipIndex = -1;
					savedPin.parentChipOutputIndex = -1;
				}
				else
				{
					savedPin = chipToLoad.savedComponentChips[chipIndex].inputPins[inputPinIndex];
				}
				Pin pin = loadedComponentChip.inputPins[inputPinIndex];

				// If this pin should receive input from somewhere, then wire it up to that pin
				if (savedPin.parentChipIndex != -1)
				{
					Pin connectedPin = loadedChipData.componentChips[savedPin.parentChipIndex].outputPins[savedPin.parentChipOutputIndex];
					pin.cyclic = savedPin.isCylic;
					Pin.TryConnect(connectedPin, pin);
					//if (Pin.TryConnect (connectedPin, pin)) {
					//Wire loadedWire = GameObject.Instantiate (wirePrefab, parent : chipHolder);
					//loadedWire.Connect (connectedPin, loadedComponentChip.inputPins[inputPinIndex]);
					//}
				}
			}
		}



		return loadedChipData;
	}

	public static SavedWireLayout LoadWiringFile(string path)
	{
		using (StreamReader reader = new StreamReader(path))
		{
			string wiringSaveString = reader.ReadToEnd();
			return JsonUtility.FromJson<SavedWireLayout>(wiringSaveString);
		}
	}

	/*
		static void SortChipsByOrderOfCreation(ref SavedChip[] chips, string[] builtinChipNames)
		{
			HashSet<string> builtinChipNameHash = new HashSet<string>(builtinChipNames);
			var sortedChips = new List<SavedChip>(chips);
			//sortedChips.Sort((a, b) => CompareChips(a, b, builtinChipNameHash));
			//sortedChips.Sort((a, b) => a.creationIndex.CompareTo(b.creationIndex));
			chips = sortedChips.ToArray();

			foreach (var c in chips)
			{
				Debug.Log(c.name);
				if (c.name == "BUS")
				{
					foreach (var n in c.componentNameList)
					{
						//Debug.Log(n);
						//c.savedComponentChips[0].
					}
				}
			}

		}
	*/

}