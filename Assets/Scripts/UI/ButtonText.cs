using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ButtonText : MonoBehaviour {

	public Button button;
	public TMPro.TMP_Text buttonText;
	public Color interactableCol = Color.white;
	public Color nonInteractableCol = Color.grey;

	void Update () {
		if (button && buttonText) {
			buttonText.color = (button.interactable) ? interactableCol : nonInteractableCol;
		}
	}

	void OnValidate () {
		if (button == null) {
			button = GetComponent<Button> ();
		}
		if (buttonText == null) {
			buttonText = transform.GetComponentInChildren<TMPro.TMP_Text> ();
		}
	}
}