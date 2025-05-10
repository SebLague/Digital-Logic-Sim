using UnityEngine;

namespace Seb.Vis.UI.Examples
{
	[ExecuteAlways]
	public class AATest : MonoBehaviour
	{
		public bool uiMode;

		public Vector2 posUI;
		public Vector2 sizeUI;
		public Color colUI;


		public Vector2 pos;
		public Vector2 size;
		public Color col;
		public Material mat;
		public bool circle;


		void Update()
		{
			if (uiMode)
			{
				using (UI.CreateFixedAspectUIScope())
				{
					UI.DrawCircle(posUI, sizeUI.x, colUI);
				}
			}
			else
			{
				mat.SetVector("pos", pos);
				mat.SetVector("size", size / 2f);

				Draw.StartLayer(Vector2.zero, 1, false);
				if (circle) Draw.Point(pos, size.x, col);
				else Draw.Quad(pos, size, col);
			}
		}
	}
}