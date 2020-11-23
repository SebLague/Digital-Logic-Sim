using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrimitiveCounter : MonoBehaviour {

	void Update () {
		if (Input.GetKeyDown (KeyCode.Equals)) {
			var player = FindObjectOfType<Player> ();
			int numAND = player.chipHolder.GetComponentsInChildren<AndGate> (includeInactive: true).Length;
			int numNOT = player.chipHolder.GetComponentsInChildren<NotGate> (includeInactive: true).Length;
			Debug.Log ("AND: " + numAND + " NOT: " + numNOT);
		}
	}
}