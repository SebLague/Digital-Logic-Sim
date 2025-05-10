using UnityEngine;

public class QuadraticBezier : MonoBehaviour
{
	[SerializeField] int resolution = 10;
	[SerializeField] Vector2 p0;
	[SerializeField] Vector2 p1;
	[SerializeField] Vector2 p2;
	[SerializeField] float thickness;
	[SerializeField] Color col;


	public void Draw()
	{
		Vector2 offset = transform.position;

		Seb.Vis.Draw.QuadraticBezier(p0 + offset, p1 + offset, p2 + offset, thickness, col, resolution);
	}
}