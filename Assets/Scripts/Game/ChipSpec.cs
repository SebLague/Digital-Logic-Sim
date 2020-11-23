using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu ()]
public class ChipSpec : ScriptableObject {

	public int[] inputGroupSizes;
	public int[] outputGroupSizes;
	public string[] inputNames;
	public string[] outputNames;
	public float signalSpacing = 0.6f;
}