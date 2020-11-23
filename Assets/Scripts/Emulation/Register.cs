using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Register : MonoBehaviour {
	public TMP_Text stateUI;
	Signal state;

	void Update () {

	}

	void Draw () {
		stateUI.text = state.ToString ();
	}

}