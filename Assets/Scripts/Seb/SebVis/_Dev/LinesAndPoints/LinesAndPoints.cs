using Seb.Vis;
using UnityEngine;

public class LinesAndPoints : MonoBehaviour
{
	public float area;
	public int num;
	public float thickness;
	public float radius;

	Vector4[] linePoints;
	Vector2[] pointCentres;

	void Update()
	{
		if (linePoints == null || linePoints.Length != num) Gen();

		Draw.StartLayer(Vector2.zero, 1, false);
		for (int i = 0; i < num; i++)
		{
			Vector4 l = linePoints[i];
			Draw.Line(new Vector2(l.x, l.y), new Vector2(l.z, -l.w), thickness, Color.white);
			Draw.Point(pointCentres[i], radius, Color.red);
		}
	}

	void Gen()
	{
		linePoints = new Vector4[num];
		pointCentres = new Vector2[num];

		for (int i = 0; i < num; i++)
		{
			Vector2 a = Random.insideUnitCircle * area;
			Vector2 b = Random.insideUnitCircle * area;
			linePoints[i] = new Vector4(a.x, a.y, b.x, b.y);
			pointCentres[i] = Random.insideUnitCircle * area;
		}
	}
}