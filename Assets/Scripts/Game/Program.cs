using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu ()]
public class Program : ScriptableObject {

	public enum EditMode { Assembly, MachineCode }
	public EditMode editMode;

	[Multiline ()]
	public string description;

	public string[] assembly;
	public string[] machineCodeString;
	public int[] machineValues;

	void OnValidate () {
		if (!Application.isPlaying) {
			if (editMode == EditMode.Assembly) {
				Assembler.Assemble (this);
			} else if (editMode == EditMode.MachineCode) {
				Assembler.CalculateMachineCodeValues (this);
			}
		}

	}

}