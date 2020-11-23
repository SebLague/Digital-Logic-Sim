using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HLTest : MonoBehaviour {

	void Start () {
		Signal signal = new Signal (0b01001101);
		for (int i = 0; i < 8; i++) {
			Debug.Log (signal[i]);
		}
		Debug.Log ("-----");
		for (int i = 0; i < 8; i++) {
			Debug.Log ((i % 2));
			signal[i] = i % 2;
		}
		Debug.Log ("-----");
		for (int i = 0; i < 8; i++) {
			Debug.Log (signal[i]);
		}

	}

	// Update is called once per frame
	void Update () {

	}
}