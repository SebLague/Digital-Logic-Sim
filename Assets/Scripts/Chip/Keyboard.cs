using System.Collections.Generic;
using UnityEngine;
using UnityRawInput;

public class Keyboard : BuiltinChip{
	public static string keycode = "";
	Dictionary<string, string> keys = new Dictionary<string,string>() {
		{"W", "0001"},
		{"A", "0010"},
		{"S", "0011"},
		{"D", "0100"},
		{"Space", "0101"},
		{"Q", "0110"},
		{"C", "0111"},
		{"V", "1000"},
		{"E", "1001"},
		{"F", "1010"},
		{"G", "1011"},
		{"H", "1100"},
		{"J", "1101"},
		{"K", "1110"},
		{"L", "1111"}
	};
	protected override void Awake() {
		base.Awake();
		RawKeyInput.Start(true);
		RawKeyInput.OnKeyDown += keyDown;
		RawKeyInput.OnKeyUp += keyUp;
	}
	void Update() {
		if(Simulation.active) {
			if(keycode != "") {
				ChipUtil.setPins(keycode, outputPins);
			} else {
				ChipUtil.setPins("0000", outputPins);
			}
		}
	}
	protected override void ProcessOutput () {}
	void keyDown(RawKey key) {
		try {
			keycode = keys[key.ToString()];
		} catch (KeyNotFoundException) {}
	}
	void keyUp(RawKey key) {
		keycode = "";
	}
	/*
			try {
				keycode = keys[e.keyCode];
			} catch (KeyNotFoundException) {}
	*/
}