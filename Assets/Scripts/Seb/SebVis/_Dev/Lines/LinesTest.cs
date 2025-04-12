using Seb.Vis;
using UnityEngine;

public class LinesTest : MonoBehaviour
{
	public int num;
	public float thickness;
	public float yMul;

	float[] ys;

	void Update()
	{
		if (ys == null || ys.Length != num) GenY();

		float x = -thickness * num / 2f;
		Draw.StartLayer(Vector2.zero, 1, false);
		for (int i = 0; i < num; i++)
		{
			float h = ys[i] * yMul;
			Draw.Line(new Vector2(x, h), new Vector2(x, -h), thickness, Color.white);
			x += thickness;
		}
	}

	void GenY()
	{
		ys = new float[num];
		for (int i = 0; i < num; i++)
		{
			ys[i] = Random.value - 0.5f;
		}
	}
}