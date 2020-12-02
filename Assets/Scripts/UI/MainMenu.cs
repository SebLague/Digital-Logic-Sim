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
		SaveSystem.SetActiveProject (projectName);
		UnityEngine.SceneManagement.SceneManager.LoadScene (1);
	}

	public void SetResolution16x9 (int width) {
		Screen.SetResolution (width, Mathf.RoundToInt (width * (9 / 16f)), Screen.fullScreenMode);
	}

	public void ToggleFullScreen () {
		Screen.fullScreen = !Screen.fullScreen;
	}

	public void Quit () {
		Application.Quit ();
	}
}