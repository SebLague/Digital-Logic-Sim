using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {

	public TMP_InputField projectNameField;
	public Button confirmProjectButton;
	public Toggle fullscreenToggle;

	void Awake () {
		fullscreenToggle.onValueChanged.AddListener (SetFullScreen);
	}

	void LateUpdate () {
		confirmProjectButton.interactable = projectNameField.text.Trim ().Length > 0;
		if (fullscreenToggle.isOn != Screen.fullScreen) {
			fullscreenToggle.SetIsOnWithoutNotify (Screen.fullScreen);
		}
	}

	public void StartNewProject () {
		string projectName = projectNameField.text;
		SaveSystem.SetActiveProject (projectName);
		UnityEngine.SceneManagement.SceneManager.LoadScene (1);
	}

	public void SetResolution16x9 (int width) {
		Screen.SetResolution (width, Mathf.RoundToInt (width * (9 / 16f)), Screen.fullScreenMode);
	}

	public void SetFullScreen (bool fullscreenOn) {
		//Screen.fullScreen = fullscreenOn;
		var nativeRes = Screen.resolutions[Screen.resolutions.Length - 1];
		if (fullscreenOn) {
			Screen.SetResolution (nativeRes.width, nativeRes.height, FullScreenMode.FullScreenWindow);
		} else {
			float windowedScale = 0.75f;
			int x = nativeRes.width / 16;
			int y = nativeRes.height / 9;
			int m = (int) (Mathf.Min (x, y) * windowedScale);
			Screen.SetResolution (16 * m, 9 * m, FullScreenMode.Windowed);
		}

	}

	public void Quit () {
		Application.Quit ();
	}
}