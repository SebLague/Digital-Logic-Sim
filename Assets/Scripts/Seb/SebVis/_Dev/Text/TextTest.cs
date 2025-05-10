using Seb.Vis.Text.Rendering;
using UnityEngine;

namespace Seb.Vis.Tests
{
	[ExecuteAlways]
	public class TextTest : MonoBehaviour
	{
		public bool screenSpace;
		public FontType font;

		[Multiline(3)]
		public string text;

		public float fontSize;
		public float lineSpacing = 1;
		public Anchor anchor;
		public Color col;
		public Vector2 layerOffset;
		public float layerScale;
		public bool lineBreakMyMaxChars;
		public int maxCharCountPerLine;


		void Update()
		{
			string displayText = text;
			if (lineBreakMyMaxChars)
			{
				displayText = UI.UI.LineBreakByCharCount(text, maxCharCountPerLine);
			}

			Draw.StartLayerIfNotInMatching(layerOffset, layerScale, screenSpace);

			TextRenderer.BoundingBox bounds = Draw.CalculateTextBounds(displayText, font, fontSize, transform.position, anchor, lineSpacing);
			Draw.QuadMinMax(bounds.BoundsMin, bounds.BoundsMax, Color.black);


			Draw.Text(font, displayText, fontSize, transform.position, anchor, col, lineSpacing);
		}
	}
}