using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateMenu : MonoBehaviour {
	public Button menuOpenButton;
	public GameObject menuHolder;
	public TMP_InputField chipNameField;
	public Button doneButton;
	public Button cancelButton;

	void Start () {
		doneButton.onClick.AddListener (FinishCreation);
		menuOpenButton.onClick.AddListener (OpenMenu);
		cancelButton.onClick.AddListener (CloseMenu);

		chipNameField.onValueChanged.AddListener (ChipNameFieldChanged);
	}

	void ChipNameFieldChanged (string value) {
		doneButton.interactable = value.Trim ().Length > 0;
	}

	void OpenMenu () {
		menuHolder.SetActive (true);
		chipNameField.text = "";
		ChipNameFieldChanged ("");
		chipNameField.Select ();
	}

	void CloseMenu () {
		menuHolder.SetActive (false);
	}

	void FinishCreation () {
		print ("done");
	}
}