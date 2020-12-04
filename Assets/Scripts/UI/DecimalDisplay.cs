using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DecimalDisplay : MonoBehaviour {

	public TMP_Text textPrefab;
	ChipInterfaceEditor signalEditor;

	List<SignalGroup> displayGroups;

	void Start () {
		displayGroups = new List<SignalGroup> ();

		signalEditor = GetComponent<ChipInterfaceEditor> ();
		signalEditor.onChipsAddedOrDeleted += RebuildGroups;
	}

	void Update () {
		UpdateDisplay ();
	}

	void UpdateDisplay () {
		for (int i = 0; i < displayGroups.Count; i++) {
			displayGroups[i].UpdateDisplay (transform);
		}
	}

	void RebuildGroups () {
		for (int i = 0; i < displayGroups.Count; i++) {
			Destroy (displayGroups[i].text.gameObject);
		}
		displayGroups.Clear ();

		var groups = signalEditor.GetGroups ();

		foreach (var group in groups) {
			if (group[0].displayGroupDecimalValue) {
				TMP_Text text = Instantiate (textPrefab);
				text.transform.SetParent (transform, true);
				displayGroups.Add (new SignalGroup () { signals = group, text = text });
			}
		}

		UpdateDisplay ();
	}

	public class SignalGroup {
		public ChipSignal[] signals;
		public TMP_Text text;

		public void UpdateDisplay (Transform transform) {
			float yPos = (signals[0].transform.position.y + signals[signals.Length - 1].transform.position.y) / 2f;
			text.transform.position = new Vector3 (transform.position.x, yPos, -0.5f);

			int decimalValue = 0;
			for (int i = 0; i < signals.Length; i++) {
				int signalState = signals[signals.Length - 1 - i].currentState;
				decimalValue |= signalState << i;
			}
			text.text = decimalValue + "";
		}
	}
}