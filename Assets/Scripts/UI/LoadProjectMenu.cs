using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadProjectMenu : MonoBehaviour {
	public UnityEngine.UI.Button projectButtonPrefab;
	public Transform scrollHolder;

	void OnEnable () {
		string[] projectNames = SaveSystem.GetSaveNames ();
		for (int i = 0; i < projectNames.Length; i++) {
			string projectName = projectNames[i];
			var button = Instantiate (projectButtonPrefab, parent : scrollHolder);
			button.GetComponentInChildren<TMPro.TMP_Text> ().text = projectName.Trim ();
			button.onClick.AddListener (() => LoadProject (projectName));
		}
	}

	public void LoadProject (string projectName) {
		SaveSystem.SetActiveProject(projectName);
		UnityEngine.SceneManagement.SceneManager.LoadScene (1);
	}
}