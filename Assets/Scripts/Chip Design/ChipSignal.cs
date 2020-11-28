using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base class for input and output signals

public class ChipSignal : Chip {

	public string signalName;

	public virtual void UpdateSignalName (string newName) {
		signalName = newName;
	}
}