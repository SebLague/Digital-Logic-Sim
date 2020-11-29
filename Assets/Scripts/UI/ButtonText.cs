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

	void Start () {
		buttonText.color = (button.interactable) ? normalCol : nonInteractableCol;
	}

	public void OnPointerEnter (PointerEventData eventData) {
		if (button.interactable) {
			buttonText.color = highlightedCol;
		}
	}

	public void OnPointerExit (PointerEventData eventData) {
		buttonText.color = (button.interactable) ? normalCol : nonInteractableCol;
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