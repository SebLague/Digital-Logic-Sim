using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ChipLoader {

	public static void LoadAllChips (string[] chipPaths, Player player) {
		var savedChips = new List<SerializableCompositeChip> ();

		for (int i = 0; i < chipPaths.Length; i++) {
			using (StreamReader reader = new StreamReader (chipPaths[i])) {
				string chipSaveString = reader.ReadToEnd ();
				savedChips.Add (JsonUtility.FromJson<SerializableCompositeChip> (chipSaveString));
			}
		}
		savedChips.Sort ((a, b) => a.loadOrder.CompareTo (b.loadOrder));

		var sw = System.Diagnostics.Stopwatch.StartNew ();
		sw.Stop ();
		// Maintain hashset of loaded chips, initially containing the builtin chips
		// Each chip will only be loaded when all the chips it's comprised of have already been loaded
		Dictionary<string, Chip> loadedChips = new Dictionary<string, Chip> ();
		for (int i = 0; i < player.builtinChips.Length; i++) {
			loadedChips.Add (player.builtinChips[i].chipName, player.builtinChips[i]);
		}

		PinAndWireInteraction wireInteraction = GameObject.FindObjectOfType<PinAndWireInteraction> ();

		for (int i = 0; i < savedChips.Count; i++) {

			var chipToTryLoad = savedChips[i];

			GameObject loadedChip = LoadChip (chipToTryLoad, loadedChips, wireInteraction.wirePrefab);
			Chip manufacturedChip = player.ManufactureChip (loadedChip, chipToTryLoad.chipName, false);
			loadedChips.Add (manufacturedChip.chipName, manufacturedChip);
		}
		player.ChipLoadingComplete ();

	}

	// Instantiates all components that make up the given clip, and connects them up with wires
	// The components are parented under a single "holder" object, which is returned from the function
	static GameObject LoadChip (SerializableCompositeChip chipToLoad, Dictionary<string, Chip> previouslyLoadedChips, Wire wirePrefab) {
		Chip[] loadedComponents = new Chip[chipToLoad.components.Length];
		Transform chipHolder = new GameObject ("Chip Holder").transform;
		//Debug.Log(chipToLoad.chipName + "  " + chipToLoad.loadOrder);
		// Spawn components (the chips used to create this chip)
		// These will have been loaded already, and stored in the previouslyLoadedChips dictionary
		for (int i = 0; i < loadedComponents.Length; i++) {
			SerializableChip componentToLoad = chipToLoad.components[i];
			string componentName = componentToLoad.chipName;
			Vector2 pos = new Vector2 ((float) componentToLoad.posX, (float) componentToLoad.posY);
			if (!previouslyLoadedChips.ContainsKey (componentName)) {
				Debug.Log ("Failed to load sub component: " + componentName + " While loading " + chipToLoad.chipName);
			}
			loadedComponents[i] = GameObject.Instantiate (previouslyLoadedChips[componentName], pos, Quaternion.identity, chipHolder);
			loadedComponents[i].order = componentToLoad.order;
			loadedComponents[i].chipSaveIndex = i;

			// Load input pin names
			for (int inputIndex = 0; inputIndex < componentToLoad.inputPins.Length; inputIndex++) {
				loadedComponents[i].inputPins[inputIndex].pinName = componentToLoad.inputPins[inputIndex].name;
			}

			// Load output pin names
			for (int ouputIndex = 0; ouputIndex < componentToLoad.outputPinNames.Length; ouputIndex++) {
				loadedComponents[i].outputPins[ouputIndex].pinName = componentToLoad.outputPinNames[ouputIndex];
			}
		}

		// Connect pins with wires
		for (int chipIndex = 0; chipIndex < loadedComponents.Length; chipIndex++) {
			Chip loadedComponentChip = loadedComponents[chipIndex];
			for (int inputPinIndex = 0; inputPinIndex < loadedComponentChip.inputPins.Length; inputPinIndex++) {
				SerializablePin savedPin = chipToLoad.components[chipIndex].inputPins[inputPinIndex];
				Pin pin = loadedComponentChip.inputPins[inputPinIndex];

				// If this pin should receive input from somewhere, then wire it up to that pin
				if (savedPin.parentChipIndex != -1) {
					Pin connectedPin = loadedComponents[savedPin.parentChipIndex].outputPins[savedPin.parentChipOutputIndex];
					pin.cyclic = savedPin.isCylic;
					Pin.TryConnect (connectedPin, pin);
					//if (Pin.TryConnect (connectedPin, pin)) {
					//Wire loadedWire = GameObject.Instantiate (wirePrefab, parent : chipHolder);
					//loadedWire.Connect (connectedPin, loadedComponentChip.inputPins[inputPinIndex]);
					//}

				}
			}
		}

		return chipHolder.gameObject;
	}

	public static SerializableWiringSystem LoadWiringFile (string path) {
		using (StreamReader reader = new StreamReader (path)) {
			string wiringSaveString = reader.ReadToEnd ();
			return JsonUtility.FromJson<SerializableWiringSystem> (wiringSaveString);
		}
	}

}