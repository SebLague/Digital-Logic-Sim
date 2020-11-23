using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ChipSkeleton : MonoBehaviour {

	public ChipSpec chipSpec;
	public Chip inputSignalPrefab;
	public Chip outputSignalPrefab;
	//public Group[] inputGroups;
	public Transform container;

	//public float groupSpacing = 0.05f;
	public float signalSpacing;
	public Transform signalHolder;
	Transform inputSignals;
	Transform outputSignals;
	public float offsetY;
	//public bool hide;
	//public GameObject hidePin;

	public Chip[][] inputSignalGroups;
	public Chip[][] outputSignalGroups;
	[SerializeField, HideInInspector]
	bool active;

	void Awake () {
		if (Application.isPlaying) {
			Delete ();

			if (active && chipSpec != null) {
				RefreshSignalPlacement ();
			}
		}
	}

	public void SpecUpdated () {
		Delete ();
		if (active) {
			RefreshSignalPlacement ();
		}
	}

	void RefreshSignalPlacement () {
		inputSignalGroups = new Chip[chipSpec.inputGroupSizes.Length][];
		outputSignalGroups = new Chip[chipSpec.outputGroupSizes.Length][];
		for (int i = 0; i < chipSpec.inputGroupSizes.Length; i++) {
			inputSignalGroups[i] = new Chip[chipSpec.inputGroupSizes[i]];
		}
		for (int i = 0; i < chipSpec.outputGroupSizes.Length; i++) {
			outputSignalGroups[i] = new Chip[chipSpec.outputGroupSizes[i]];
		}

		signalHolder.transform.localPosition = new Vector3 (0, 0);
		Organize (chipSpec.inputGroupSizes, transform.position.x - container.localScale.x / 2, inputSignalPrefab, inputSignals);
		Organize (chipSpec.outputGroupSizes, transform.position.x + container.localScale.x / 2, outputSignalPrefab, outputSignals);
		signalHolder.transform.localPosition = new Vector3 (0, offsetY);

		// Set order (not just doing when spawning, since sometimes I'm manually placing some inputs/outputs)
		var inputsSortedByHeight = new List<InputSignal> (FindObjectsOfType<InputSignal> ());
		inputsSortedByHeight.Sort ((a, b) => b.transform.position.y.CompareTo (a.transform.position.y));
		var outputsSortedByHeight = new List<OutputSignal> (FindObjectsOfType<OutputSignal> ());
		outputsSortedByHeight.Sort ((a, b) => b.transform.position.y.CompareTo (a.transform.position.y));
		for (int i = 0; i < inputsSortedByHeight.Count; i++) {
			inputsSortedByHeight[i].order = i;
		}
		for (int i = 0; i < outputsSortedByHeight.Count; i++) {
			outputsSortedByHeight[i].order = i;
		}
	}

	void Organize (int[] groupSizes, float x, Chip signalPrefab, Transform currentSignalHolder) {
		bool isInputSignals = signalPrefab is InputSignal;
		var resultArray = (isInputSignals) ? inputSignalGroups : outputSignalGroups;

		float height = container.localScale.y;

		int numGroups = groupSizes.Length;
		int elementCount = 0;
		for (int i = 0; i < numGroups; i++) {
			elementCount += groupSizes[i];
		}

		//float spacing = height / (elementCount + 1f) - (numGroups - 1) * groupSpacing;
		float spacing = signalSpacing;
		float totalSpacing = spacing * elementCount;

		float leftOverSpace = height - totalSpacing;
		float spaceBetweenGroups = leftOverSpace / (numGroups + 1f);
		int pinIndex = 0;
		float currentY = height / 2;
		for (int i = 0; i < numGroups; i++) {
			currentY -= spaceBetweenGroups;
			for (int j = 0; j < groupSizes[i]; j++) {
				currentY -= spacing / 2f;
				var signal = Instantiate (signalPrefab, parent : currentSignalHolder);
				signal.transform.position = new Vector3 (x, transform.position.y + currentY);
				currentY -= spacing / 2f;

				if (signal.outputPins.Length > 0) {
					if (chipSpec.inputNames.Length > pinIndex) {
						signal.outputPins[0].pinName = chipSpec.inputNames[pinIndex];
					}
				} else {
					if (chipSpec.outputNames.Length > pinIndex) {
						signal.inputPins[0].pinName = chipSpec.outputNames[pinIndex];
					}
				}
				//if (pinIndex == 2 && signalPrefab is SignalGenerator) {
				//hidePin = signal.gameObject;
				//}
				resultArray[i][j] = signal;
				pinIndex++;

			}
		}
	}

	void Delete () {
		for (int i = signalHolder.childCount - 1; i >= 0; i--) {
			DestroyImmediate (signalHolder.GetChild (i).gameObject);
		}

		inputSignals = new GameObject ("Input Signals").transform;
		inputSignals.transform.parent = signalHolder;

		outputSignals = new GameObject ("Output Signals").transform;
		outputSignals.transform.parent = signalHolder;
	}

	public void SetActive (bool active) {
		this.active = active;
	}


	[System.Serializable]
	public class Group {
		public Transform[] elements;
	}
}