using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegisterDebugInfo : MonoBehaviour {
	CustomChip registerChip;
	TMPro.TMP_Text text;
	int storedVal = 0;

	void Start () {
		text = GetComponent<TMPro.TMP_Text> ();
	}

	void Update () {
		if (registerChip) {
			// load enable
			if (registerChip.inputPins[5].State == 1) {
				storedVal = 0;
				for (int i = 0; i < 4; i++) {
					storedVal |= registerChip.inputPins[i].State << (3 - i);
				}
			}

			string s = "";
			for (int i = 0; i < 4; i++) {
				s += storedVal.GetBit (3 - i);
			}
			s += "\n(" + storedVal + ")";
			text.text = s;

		} else {
			var allChips = FindObjectsOfType<CustomChip> ();
			CustomChip closestRegister = null;
			float dst = 999999;
			foreach (var r in allChips) {
				if (r.chipName == "REGISTER") {
					float d = (r.transform.position - transform.position).magnitude;
					if (d < dst) {
						dst = d;
						closestRegister = r;
					}
				}
			}
			registerChip = closestRegister;
		}
	}
}