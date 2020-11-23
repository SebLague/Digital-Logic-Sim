using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constant : Chip {
	public bool high;
	public MeshRenderer meshRenderer;
	public Palette palette;
	
	public void SendSignal () {
		outputPins[0].ReceiveSignal ((high) ? 1 : 0);
		//Debug.Log ("Send const signal to " + outputPins[0].childPins[0].pinName + " " + outputPins[0].childPins[0].chip.chipName);
	}

	void Update () {
		meshRenderer.material.color = (high) ? palette.onCol : palette.offCol;
	}
}