using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChipBarUI : MonoBehaviour {
	public Transform mask;
	public Transform buttonHolder;
	public Button buttonPrefab;
	public float buttonSpacing = 15f;
	public float buttonWidthPadding = 10;
	float rightmostButtonEdgeX;
	Player player;
	public List<string> hideList;

	void Awake () {
		player = FindObjectOfType<Player> ();
		player.onChipManufactured += AddChipButton;
		for (int i = 0; i < player.builtinChips.Length; i++) {
			AddChipButton (player.builtinChips[i]);
		}
	}

	void AddChipButton (Chip chip) {
		if (hideList.Contains (chip.chipName)) {
			//Debug.Log("Hiding")
			return;
		}
		Button button = Instantiate (buttonPrefab);
		button.gameObject.name = "Create (" + chip.chipName + ")";
		// Set button text
		var buttonTextUI = button.GetComponentInChildren<TMP_Text> ();
		buttonTextUI.text = chip.chipName;

		// Set button size
		var buttonRect = button.GetComponent<RectTransform> ();
		buttonRect.sizeDelta = new Vector2 (buttonTextUI.preferredWidth + buttonWidthPadding, buttonRect.sizeDelta.y);

		// Set button position
		buttonRect.SetParent (buttonHolder, false);
		buttonRect.localPosition = new Vector3 (rightmostButtonEdgeX + buttonSpacing + buttonRect.sizeDelta.x / 2f, 0, 0);
		rightmostButtonEdgeX = buttonRect.localPosition.x + buttonRect.sizeDelta.x / 2f;

		// Set button event
		ChipInteraction chipPlacement = FindObjectOfType<ChipInteraction> ();
		button.onClick.AddListener (() => chipPlacement.SpawnChip (chip));
	}

}