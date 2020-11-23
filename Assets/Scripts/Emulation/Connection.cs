using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connection : MonoBehaviour {

	public EmulatedPin parentPin;
	public EmulatedPin childPin;

	[SerializeField, HideInInspector]
	bool setupComplete;

	public void SetUp () {
		transform.SetParent (parentPin.transform);
		childPin.parentPin = parentPin;
		parentPin.childPins.Add (childPin);
	}

	public void Delete () {

	}

	void OnValidate () {
		if (parentPin && childPin && !setupComplete) {
			setupComplete = true;
			SetUp ();
		}
	}

	void OnDrawGizmos () {
		if (parentPin && childPin) {
			Gizmos.color = Color.black;
			Vector3 currentPoint = parentPin.transform.position;
			for (int i = 0; i < transform.childCount + 1; i++) {
				Vector3 nextPoint = (i < transform.childCount) ? transform.GetChild (i).position : childPin.transform.position;
				Gizmos.DrawLine (currentPoint, nextPoint);
				currentPoint = nextPoint;
			}
		}
	}
}