using UnityEngine;

// Output signal of a chip.
public class OutputSignal : ChipSignal {
	public Palette palette;
	public MeshRenderer lightRenderer;

	protected override void Start () {
		base.Start ();
		SetDisplayState (0);
	}

	public override void ReceiveInputSignal (Pin inputPin) {

		SetDisplayState (inputPin.State);
	}

	public void SetDisplayState (int state) {
		bool powerOn = state == 1;
		if (lightRenderer) {
			lightRenderer.material.color = (powerOn) ? palette.onCol : palette.offCol;
		}
	}

	public override void UpdateSignalName (string newName) {
		base.UpdateSignalName (newName);
		inputPins[0].pinName = newName;
	}

}