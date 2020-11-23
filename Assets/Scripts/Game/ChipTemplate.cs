using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ChipTemplate : MonoBehaviour {

	public bool builtinChip;
	public TMPro.TextMeshPro nameText;
	public Transform container;
	public Pin chipPinPrefab;
	public Palette pallete;
	public Color col;
	public bool customizeInEditor;
	public bool horizontal;
	public bool horizontal2;
	public bool useCol;
	public bool useCustomLastInputPos;
	public Vector3 customLastInputPos;
	public Vector2 customPadding;

	const string pinHolderName = "Pin Holder";

	void Awake () {
		if (builtinChip && Application.isPlaying) {
			SetSizeAndSpacing (GetComponent<Chip> ());
			SetCol (GetComponent<Chip> ().chipName);

		}
	}

	void Update () {
		if (!Application.isPlaying && customizeInEditor) {
			if (transform.Find (pinHolderName)) {
				GameObject.DestroyImmediate (transform.Find (pinHolderName).gameObject);
			}
			var customChip = GetComponent<CustomChip> ();
			nameText.text = (customChip.customFullName) ? customChip.fullName : customChip.chipName;
			SpawnPins (customChip);
			SetSizeAndSpacing (customChip);
		}
	}

	void SetCol (string chipName) {
		if (useCol) {
			container.GetComponent<MeshRenderer> ().material.color = col;
		} else {
			for (int i = 0; i < pallete.chipCols.Length; i++) {
				//Debug.Log ("|" + pallete.chipCols[i].name + "|  |" + chipName + "|  " + string.Equals (pallete.chipCols[i].name, chipName));
				if (pallete.chipCols[i].name == chipName) {

					//Debug.Log (pallete.chipCols[i].col);
					container.GetComponent<MeshRenderer> ().material.color = pallete.chipCols[i].col;
					break;
				}
			}
		}
	}

	public Chip Create (GameObject chipHolder, string chipName, bool fromDisk = true) {
		gameObject.name = chipName;
		nameText.text = chipName;
		if (chipName == "REGISTER") {
			//nameText.text = "REG";
		}

		SetCol (chipName);

		// Add and set up the custom chip component
		CustomChip chip = gameObject.AddComponent<CustomChip> ();
		chip.chipName = chipName;
		var signalGenerators = new List<InputSignal> ();
		var outputSignals = new List<OutputSignal> ();

		if (fromDisk) {
			signalGenerators.AddRange (chipHolder.GetComponentsInChildren<InputSignal> ());
			outputSignals.AddRange (chipHolder.GetComponentsInChildren<OutputSignal> ());

		} else {
			var signalGens = FindObjectsOfType<InputSignal> ();
			var outputSigs = FindObjectsOfType<OutputSignal> ();
			foreach (var va in signalGens) {
				var copy = Instantiate (va, va.transform.position, Quaternion.identity, chipHolder.transform);
				signalGenerators.Add (copy);
				foreach (var c in va.outputPins[0].childPins) {
					c.parentPin = copy.outputPins[0];
				}
			}
			foreach (var vb in outputSigs) {
				var copy = Instantiate (vb, vb.transform.position, Quaternion.identity, chipHolder.transform);
				outputSignals.Add (copy);
				//if (copy.inputPins[0].parentPin) {
				var originalIndex = copy.inputPins[0].parentPin.childPins.IndexOf (vb.inputPins[0]);
				copy.inputPins[0].parentPin.childPins[originalIndex] = copy.inputPins[0];
				//}
			}
			//signalGenerators.AddRange (FindObjectsOfType<SignalGenerator> ());
			//outputSignals.AddRange (FindObjectsOfType<OutputSignal> ());
		}

		//var signalGenerators = new List<SignalGenerator> (FindObjectsOfType<SignalGenerator> ());
		//var outputSignals = new List<OutputSignal> (FindObjectsOfType<OutputSignal> ());
		signalGenerators.Sort ((a, b) => a.order.CompareTo (b.order));
		outputSignals.Sort ((a, b) => a.order.CompareTo (b.order));

		chip.signalGenerators = signalGenerators.ToArray ();
		chip.outputSignals = outputSignals.ToArray ();
		chip.constants = chipHolder.GetComponentsInChildren<Constant> ();

		SpawnPins (chip);
		SetSizeAndSpacing (chip);

		// Parent chip holder to the template, and hide
		chipHolder.gameObject.name = "Implementation";
		chipHolder.transform.parent = transform;
		chipHolder.transform.localPosition = Vector3.zero;
		chipHolder.SetActive (false);

		return chip;
	}

	public void SpawnPins (CustomChip chip) {
		transform.eulerAngles = Vector3.zero;
		Transform pinHolder = new GameObject (pinHolderName).transform;
		pinHolder.parent = transform;
		pinHolder.localPosition = Vector3.zero;
		pinHolder.localEulerAngles = Vector3.zero;

		chip.inputPins = new Pin[chip.signalGenerators.Length];
		chip.outputPins = new Pin[chip.outputSignals.Length];

		for (int i = 0; i < chip.inputPins.Length; i++) {
			Pin inputPin = Instantiate (chipPinPrefab, pinHolder.position, Quaternion.identity, pinHolder);
			inputPin.pinType = Pin.PinType.ChipInput;
			inputPin.chip = chip;
			inputPin.pinName = chip.signalGenerators[i].outputPins[0].pinName;
			chip.inputPins[i] = inputPin;
			inputPin.SetScale ();
		}

		for (int i = 0; i < chip.outputPins.Length; i++) {
			Pin outputPin = Instantiate (chipPinPrefab, pinHolder.position, Quaternion.identity, pinHolder);
			outputPin.pinType = Pin.PinType.ChipOutput;
			outputPin.chip = chip;
			outputPin.pinName = chip.outputSignals[i].inputPins[0].pinName;
			chip.outputPins[i] = outputPin;
			outputPin.SetScale ();
		}

	}

	public void SetSizeAndSpacing (Chip chip) {
		nameText.fontSize = (Player.SmallMode) ? 2 : 2.5f;

		float containerHeightPadding = (Player.SmallMode) ? 0.05f : 0.1f;
		float containerWidthPadding = 0.1f + customPadding.x;
		float pinSpacePadding = (Player.SmallMode) ? 0.005f : 0.015f;
		float containerWidth = nameText.preferredWidth + Pin.interactionRadius * 2f + containerWidthPadding;
		if (horizontal || horizontal2) {
			containerWidth = Pin.interactionRadius * 3f + containerWidthPadding;
		}

		int numPins = Mathf.Max (chip.inputPins.Length - (useCustomLastInputPos?1 : 0), chip.outputPins.Length);
		float unpaddedContainerHeight = numPins * (Pin.interactionRadius * 2 + pinSpacePadding) + customPadding.y;
		float containerHeight = unpaddedContainerHeight + containerHeightPadding;
		float topPinY = unpaddedContainerHeight / 2 - Pin.radius;
		float bottomPinY = -unpaddedContainerHeight / 2 + Pin.radius;
		const float z = -0.05f;

		// Input pins
		int numInputPinsToAutoPlace = chip.inputPins.Length - (useCustomLastInputPos?1 : 0);
		for (int i = 0; i < numInputPinsToAutoPlace; i++) {
			float percent = 0.5f;
			if (chip.inputPins.Length > 1) {
				percent = i / (numInputPinsToAutoPlace - 1f);
			}
			float posX = -containerWidth / 2f;

			float posY = Mathf.Lerp (topPinY, bottomPinY, percent);
			chip.inputPins[i].transform.localPosition = new Vector3 (posX, posY, z);
		}
		if (useCustomLastInputPos) {
			chip.inputPins[chip.inputPins.Length - 1].transform.localPosition = customLastInputPos;
		}

		// Output pins
		for (int i = 0; i < chip.outputPins.Length; i++) {
			float percent = 0.5f;
			if (chip.outputPins.Length > 1) {
				percent = i / (chip.outputPins.Length - 1f);

			}

			float posX = containerWidth / 2f;
			float posY = Mathf.Lerp (topPinY, bottomPinY, percent);
			chip.outputPins[i].transform.localPosition = new Vector3 (posX, posY, z);
		}

		// Set container size
		container.transform.localScale = new Vector3 (containerWidth, containerHeight, 1);

		GetComponent<BoxCollider2D> ().size = new Vector2 (containerWidth, containerHeight);

		if (horizontal) {
			transform.eulerAngles = Vector3.forward * -90;
		} else if (horizontal2) {
			transform.eulerAngles = Vector3.forward * 90;
		} else {
			transform.eulerAngles = Vector3.forward * 0;
		}

		nameText.transform.eulerAngles = Vector3.forward * 0;

	}
}