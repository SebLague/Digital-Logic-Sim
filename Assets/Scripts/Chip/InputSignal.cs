using UnityEngine;

// Provides input signal (0 or 1) to a chip.
// When designing a chip, this input signal can be manually set to 0 or 1 by the player.
public class InputSignal : ChipSignal {

	public bool active;
	public MeshRenderer meshRenderer;
	public Palette palette;

	protected override void Start () {
		base.Start ();
		SetCol ();
	}

	public void ToggleActive () {
		active = !active;
		SetCol ();
	}

	public void SendSignal (int signal) {
		active = signal == 1;
		outputPins[0].ReceiveSignal (signal);
		SetCol ();
	}

	public void SendSignal () {
		int state = (active) ? 1 : 0;
		outputPins[0].ReceiveSignal (state);
	}

	void SetCol () {
		if (meshRenderer) {
			meshRenderer.material.color = (active) ? palette.onCol : palette.offCol;
		}
	}

	public override void UpdateSignalName (string newName) {
		base.UpdateSignalName (newName);
		outputPins[0].pinName = newName;
	}

	void OnMouseDown () {
		ToggleActive ();
	}
}