using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinNameDisplayManager : MonoBehaviour {
	public PinAndWireInteraction pinInteraction;

	public PinNameDisplay pinNameDisplay;
	ChipEditorOptions editorDisplayOptions;
	Pin highlightedPin;

	void Awake () {
		editorDisplayOptions = FindObjectOfType<ChipEditorOptions> ();
		pinInteraction.onMouseOverPin += OnMouseOverPin;
		pinInteraction.onMouseExitPin += OnMouseExitPin;
		pinNameDisplay.gameObject.SetActive (false);
	}

	void Update () {
		var pinNameDisplayMode = editorDisplayOptions.activePinNameDisplayMode;
		pinNameDisplay.gameObject.SetActive (false);

		if (highlightedPin) {
			if (InputHelper.AnyOfTheseKeysHeld (KeyCode.LeftAlt, KeyCode.RightAlt) || pinNameDisplayMode == ChipEditorOptions.PinNameDisplayMode.Hover) {
				pinNameDisplay.gameObject.SetActive (true);
				pinNameDisplay.Set (highlightedPin);
			}
		}

	}

	void OnMouseOverPin (Pin pin) {
		highlightedPin = pin;
	}

	void OnMouseExitPin (Pin pin) {
		if (highlightedPin == pin) {
			highlightedPin = null;
		}
	}
}