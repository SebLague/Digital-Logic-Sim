using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour {

	public GameObject createMenu;

	public void OpenCreateMenu () {
		createMenu.SetActive (true);
	}

	public void CloseCreateMenu () {
		createMenu.SetActive (false);
	}

	public void CompleteManufacturing () {
		string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789()[]{}/:-–";
		string chipName = "";

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
		foreach (var v in FindObjectsOfType<OutputSignal> ()) {
			v.inputPins[0].ReceiveSignal (0);
			v.inputPins[0].parentPin = null;
		}
		foreach (var v in FindObjectsOfType<InputSignal> ()) {
			v.outputPins[0].childPins = new List<Pin> ();
		}
	}

}