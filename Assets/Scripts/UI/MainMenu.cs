using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {

	public TMP_InputField projectNameField;
	public Button confirmProjectButton;

	void Update () {
		confirmProjectButton.interactable = projectNameField.text.Trim ().Length > 0;
	}

	public void StartNewProject () {
		string projectName = projectNameField.text;
		SaveSystem.activeProjectName = projectName;
		UnityEngine.SceneManagement.SceneManager.LoadScene (1);
	}

	public void Quit () {
		Application.Quit ();
	}
}