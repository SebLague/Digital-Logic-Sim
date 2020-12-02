using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public Button button;
	public TMPro.TMP_Text buttonText;
	public Color normalCol = Color.white;
	public Color nonInteractableCol = Color.grey;
	public Color highlightedCol = Color.white;
	bool highlighted;

	void Start () {

	}

	void Update () {
		Color col = (highlighted) ? highlightedCol : normalCol;
		buttonText.color = (button.interactable) ? col : nonInteractableCol;
	}

	public void OnPointerEnter (PointerEventData eventData) {
		if (button.interactable) {
			highlighted = true;
		}
	}

	public void OnPointerExit (PointerEventData eventData) {
		highlighted = false;
	}

	void OnEnable () {
		highlighted = false;
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