using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation
{
	public class ColourHelper
	{
		public static Color TextBlackOrWhite(Color backgroundCol)
		{
			return Luminance(backgroundCol) > 0.57f ? Color.black : Color.white;
		}

		public static Color GenerateRandomChipColour()
		{
			System.Random rng = new();
			float hue = (float)rng.NextDouble();
			float sat = Mathf.Lerp(0.2f, 1, (float)rng.NextDouble());
			float val = Mathf.Lerp(0.2f, 1, (float)rng.NextDouble());
			return Color.HSVToRGB(hue, sat, val);
		}

		public static float Luminance(Color col)
		{
			return 0.2126f * col.r + 0.7152f * col.g + 0.0722f * col.b;
		}

		public static Color Darken(Color col, float darkenAmount, float desaturateAmount = 0)
		{
			Color.RGBToHSV(col, out float h, out float s, out float v);
			v = Mathf.Clamp01(v - darkenAmount);
			s = Mathf.Clamp01(s - desaturateAmount);
			return Color.HSVToRGB(h, s, v);
		}

	}
}