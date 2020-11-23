using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MSAATest : MonoBehaviour {
	void Update () {

		if (Input.GetKeyDown (KeyCode.M)) {
			Camera.main.allowMSAA = !Camera.main.allowMSAA;
			Debug.LogError (((Camera.main.allowMSAA) ? "MSAA ON" : "MSAA OFF"));
		}
	}
}