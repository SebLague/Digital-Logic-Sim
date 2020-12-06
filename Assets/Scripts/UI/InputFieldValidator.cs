using TMPro;
using UnityEngine;

public class InputFieldValidator : MonoBehaviour {

	public TMP_InputField inputField;
	public string validChars = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789()[]<>";

	void Awake () {
		inputField.onValueChanged.AddListener (OnEdit);
	}

	void OnEdit (string newString) {
		string validString = "";
		for (int i = 0; i < newString.Length; i++) {
			if (validChars.Contains (newString[i].ToString ())) {
				validString += newString[i];
			}
		}

		inputField.SetTextWithoutNotify (validString);
	}

	void OnValidate () {
		if (inputField == null) {
			inputField = GetComponent<TMP_InputField> ();
		}
	}
}