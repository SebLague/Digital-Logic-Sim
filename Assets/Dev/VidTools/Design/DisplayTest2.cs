using System.Diagnostics;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.Internal;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace DLS.Dev
{
	public class DisplayTest2 : MonoBehaviour
	{
		public int pixelCount = 16;
		public float size;
		public float pixelSizeT;
		public bool circle;
		public int seed;

		ShapeData[] quads;

		void Reset()
		{
			// init quads
			quads = new ShapeData[pixelCount * pixelCount];
			int i = 0;
			for (int x = 0; x < pixelCount; x++)
			{
				for (int y = 0; y < pixelCount; y++)
				{
					Bounds2D pixelBounds = GetGridPixelBounds(x, y, pixelCount, pixelCount, Vector2.one * size);
					quads[i] = ShapeData.CreateQuad(pixelBounds.Centre, pixelBounds.Size * pixelSizeT, Color.black, new Vector2(-1000, -1000), new Vector2(10000, 10000));
					i++;
				}
			}
		}

		void Start()
		{
			Reset();
		}


		// Update is called once per frame
		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space)) Reset();

			DrawDisp();
		}

		void DrawDisp()
		{
			Stopwatch sw = Stopwatch.StartNew();
			Draw.StartLayer(Vector2.zero, 1, false);
			Random rng = new(seed);

			int i = 0;
			for (int x = 0; x < pixelCount; x++)
			{
				for (int y = 0; y < pixelCount; y++)
				{
					int val = rng.Next(0, 256);
					val = val % 2 == 0 ? 0 : 255;
					val = y * pixelCount + x;
					float r = ((val >> 0) & 0b11) / 3f;
					float g = ((val >> 2) & 0b11) / 3f;
					float b = ((val >> 4) & 0b11) / 3f;
					Color col = new(r, g, b);
					quads[i].col = col;
					i++;
				}
			}

			Draw.ShapeBatch(quads);

			sw.Stop();

			Debug.Log("draw dispatch time: " + sw.ElapsedMilliseconds + " ms");
		}


		static Bounds2D GetGridPixelBounds(int x, int y, int numX, int numY, Vector2 size)
		{
			float pixelSizeX = size.x / numX;
			float pixelSizeY = size.y / numY;
			Vector2 pixelSize = new(pixelSizeX, pixelSizeY);

			Vector2 bottomLeftCorner = -size / 2;
			Vector2 bottomLeftPixelCentre = bottomLeftCorner + pixelSize * 0.5f;

			Vector2 pixelCentre = bottomLeftPixelCentre + new Vector2(x * pixelSizeX, y * pixelSizeY);
			return Bounds2D.CreateFromCentreAndSize(pixelCentre, pixelSize);
		}
	}
}