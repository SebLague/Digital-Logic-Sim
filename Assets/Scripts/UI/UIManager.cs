using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour {
	public TMP_Text chipNameUI;

	void Start () { }

	public void SetChipDisplayName (string chipName) {
		chipNameUI.transform.parent.parent.GetComponent<TMPro.TMP_InputField> ().text = chipName;
	}

	public void CompleteManufacturing () {
		string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789()[]{}/:-–";
		string chipName = chipNameUI.text.Trim ();

		string cleanedChipName = "";
		foreach (char c in chipName) {
			if (allowedChars.Contains (c.ToString ().ToUpper ())) {
				cleanedChipName += c;
			}
		}
		chipName = cleanedChipName.ToString ();
		//manufactureMenu.SetActive (false);

		FindObjectOfType<Player> ().ManufactureChip (chipName, true);

		// Reset scene
		SetChipDisplayName ("");
		foreach (var v in FindObjectsOfType<OutputSignal> ()) {
			v.inputPins[0].ReceiveSignal (0);
			v.inputPins[0].parentPin = null;
		}
		foreach (var v in FindObjectsOfType<InputSignal> ()) {
			v.outputPins[0].childPins = new List<Pin> ();
		}
	}

}