using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Emulator : MonoBehaviour {
	public bool emulationActive;
	public static int emulationFrame;

	void Update () {
		if (emulationActive) {
			emulationFrame++;
		
		}
	}
}