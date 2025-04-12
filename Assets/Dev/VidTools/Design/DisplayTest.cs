using System;
using System.Diagnostics;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.Internal;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace DLS.Dev
{
	public class DisplayTest : MonoBehaviour
	{
		public bool useInputString;
		public string inputString;
		public bool randomizeSeed = true;
		public int seed;

		public int pixelCount = 16;
		public float size;
		public float pixelSizeT;
		public bool circle;
		public float updateDelay;
		public bool keepRules;
		public int[] rules;

		public int bestRuleIndex = -1;
		public string[] bestRules;

		int[,] map;

		float nextUpdateTime;

		ShapeData[] quads;
		int yCurr;

		void Reset()
		{
			yCurr = 0;
			if (randomizeSeed)
			{
				seed = new Random().Next();
			}

			Random rng = new(seed);
			map = new int[pixelCount, pixelCount];
			for (int x = 0; x < pixelCount; x++)
			{
				map[x, yCurr] = rng.NextDouble() < 0.5 ? 1 : 0;
				if (useInputString) map[x, yCurr] = inputString[x] == '0' ? 0 : 1;
			}

			if (!keepRules || bestRuleIndex >= 0)
			{
				rules = new int[8];
				for (int x = 0; x < rules.Length; x++)
				{
					rules[x] = rng.NextDouble() < 0.5 ? 1 : 0;
					if (bestRuleIndex >= 0)
					{
						string ruleString = bestRules[bestRuleIndex];
						rules[x] = ruleString[x] == '0' ? 0 : 1;
					}
				}
			}


			nextUpdateTime = Time.time + updateDelay;

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


			if (Time.time > nextUpdateTime && yCurr < pixelCount - 1)
			{
				nextUpdateTime = Time.time + updateDelay;
				for (int x = 0; x < pixelCount; x++)
				{
					UpdateCell(x);
				}

				yCurr++;
			}
		}

		void DrawDisp()
		{
			Stopwatch sw = Stopwatch.StartNew();
			Draw.StartLayer(Vector2.zero, 1, false);

			int i = 0;
			for (int x = 0; x < pixelCount; x++)
			{
				for (int y = 0; y < pixelCount; y++)
				{
					bool b = map[x, pixelCount - y - 1] == 0;
					Color col = b ? Color.black : Color.white;
					quads[i].col = col;
					i++;
				}
			}

			Draw.ShapeBatch(quads);
			/*
			for (int y = 0; y < pixelCount; y++)
			{
				for (int x = 0; x < pixelCount; x++)
				{
					Bounds2D pixelBounds = GetGridPixelBounds(x, y, pixelCount, pixelCount, Vector2.one * size);
					Color col = GetCol(x, y);
					if (circle)
					{
						Draw.Point(pixelBounds.Centre, pixelBounds.Size.x / 2 * pixelSizeT, col);
					}
					else
					{
						Draw.Quad(pixelBounds.Centre, pixelBounds.Size * pixelSizeT, col);
					}
				}
			}
			*/

			sw.Stop();

			Debug.Log("draw dispatch time: " + sw.ElapsedMilliseconds + " ms");
		}

		void UpdateCell(int cx)
		{
			int type = 0;
			int bitCount = (int)Math.Log(pixelCount, 2);
			int mask = (1 << bitCount) - 1;
			//Debug.Log(mask);

			for (int ox = -1; ox <= 1; ox++)
			{
				//if (ox == 0 && oy == 0) continue;
				int x = (cx + ox) & mask;

				if (map[x, yCurr] == 1)
				{
					int leftShift = 1 - ox;
					// leftShift = ox + 1;
					type |= 1 << leftShift;
				}
			}

			int stateNew = rules[type];

			map[cx, yCurr + 1] = stateNew;
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