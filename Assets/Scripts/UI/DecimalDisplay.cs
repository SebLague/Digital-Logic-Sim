using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecimalDisplay : MonoBehaviour {
	public TMPro.TMP_Text text;
	public bool input;
	public int group;
	Chip[] signals;
	public bool twosComplement;

	// Start is called before the first frame update
	void Start () {
		if (input) {
			signals = FindObjectOfType<ChipSkeleton> ().inputSignalGroups[group];
		} else {
			signals = FindObjectOfType<ChipSkeleton> ().outputSignalGroups[group];
		}
	}

	void Update () {
		int n = signals.Length-1;
		int val = 0;
		for (int i = 0; i < signals.Length; i++) {
			Pin pin = (input) ? signals[i].outputPins[0] : signals[i].inputPins[0];
			if (pin.currentState == 1) {
				val |= 1 << (n - i);
			}
		}
		if (twosComplement) {
			if (val >> n == 1) {
				//Debug.Log ("Neg: " + (val) + "   " + (1 >> 3));
				val = (val & ~(1 << n)) - (1 << n);
			}
		}
		text.text = val.ToString ();
	}
}