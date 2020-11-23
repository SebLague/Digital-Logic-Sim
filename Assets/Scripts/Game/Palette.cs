using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu ()]
public class Palette : ScriptableObject {
	public Color onCol;
	public Color offCol;
	public Color highZCol;
	public Color fadedOffCol;
	public Color fadedOnCol;
	public Color controlWireOnCol;
	public Color clockWireOnCol;
	public ChipCol[] chipCols;

	[System.Serializable]
	public struct ChipCol {
		public string name;
		public Color col;
	}
}