using UnityEngine;

public class LineVisTest : MonoBehaviour
{
	public float thickness;
	public Vector2 offset;
	public Color col;

	[Range(0, 1)]
	public float t = 1;

	void Update()
	{
		//t = SebStuff.Ease.Cubic.InOut(Time.time / 3);
	}
}