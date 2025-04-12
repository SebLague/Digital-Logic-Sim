using UnityEngine;

public class ArrowVisTest : MonoBehaviour
{
	public Color col;
	public float thickness;
	public Vector2 offset;
	[Range(0, 1)] public float t;

	public bool useHeadDefaults;
	public float headAngle;
	public float headLength;
}