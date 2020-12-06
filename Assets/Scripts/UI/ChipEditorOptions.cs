using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChipEditorOptions : MonoBehaviour {

	public enum PinNameDisplayMode {
		AltHover,
		Hover
	}

	public PinNameDisplayMode activePinNameDisplayMode;

	public Toggle[] pinDisplayToggles;

	void Awake () {
		pinDisplayToggles[0].onValueChanged.AddListener ((isOn) => SetPinNameMode (isOn, PinNameDisplayMode.AltHover));
		pinDisplayToggles[1].onValueChanged.AddListener ((isOn) => SetPinNameMode (isOn, PinNameDisplayMode.Hover));
	}

	void SetPinNameMode (bool isOn, PinNameDisplayMode mode) {
		if (isOn) {
			activePinNameDisplayMode = mode;
		}
	}

}