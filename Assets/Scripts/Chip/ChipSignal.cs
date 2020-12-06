using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base class for input and output signals
public class ChipSignal : Chip {

	public int currentState;

	public Palette palette;
	public MeshRenderer indicatorRenderer;
	public MeshRenderer pinRenderer;
	public MeshRenderer wireRenderer;

	int groupID = -1;
	public bool displayGroupDecimalValue { get; set; } = false;
	public bool useTwosComplement { get; set; } = true;

	[HideInInspector]
	public string signalName;
	protected bool interactable = true;

	public virtual void SetInteractable (bool interactable) {
		this.interactable = interactable;

		if (!interactable) {
			indicatorRenderer.material.color = palette.nonInteractableCol;
			pinRenderer.material.color = palette.nonInteractableCol;
			wireRenderer.material.color = palette.nonInteractableCol;
		}
	}

	public void SetDisplayState (int state) {

		if (indicatorRenderer && interactable) {
			indicatorRenderer.material.color = (state == 1) ? palette.onCol : palette.offCol;
		}
	}

	public static bool InSameGroup (ChipSignal signalA, ChipSignal signalB) {
		return (signalA.groupID == signalB.groupID) && (signalA.groupID != -1);
	}

	public int GroupID {
		get {
			return groupID;
		}
		set {
			groupID = value;
		}
	}

	public virtual void UpdateSignalName (string newName) {
		signalName = newName;
	}
}