using System;
using System.Collections.Generic;
using Seb.Vis.Text.Rendering.Helpers;
using UnityEngine;

namespace Seb.Vis.Text.FontLoading
{
	// Contains the raw data loaded from a TrueType font file.
	public class FontData
	{
		readonly Dictionary<uint, Glyph> glyphLookup;
		public readonly bool IsMonospaced;
		public readonly float MonospacedAdvanceWidth;
		public Glyph MissingGlyph;


		public FontData(FontParser.GlyphRaw[] rawGlyphs, int unitsPerEm, bool isMono)
		{
			IsMonospaced = isMono;
			// Scale factor from font's arbitrary 'design units' to standard 'em' units.
			float emScale = 1f / unitsPerEm;

			// -- Create processed glyph data from raw glyph data --
			Glyphs = new Glyph[rawGlyphs.Length];

			for (int i = 0; i < Glyphs.Length; i++)
			{
				Glyphs[i] = CreateFromRawGlyph(rawGlyphs[i], emScale);
			}

			MonospacedAdvanceWidth = Glyphs[0].AdvanceWidth;

			// -- Create char to glyph lookup -- 
			glyphLookup = new Dictionary<uint, Glyph>();

			foreach (Glyph c in Glyphs)
			{
				if (c == null) continue;
				glyphLookup.Add(c.UnicodeValue, c);
				if (c.GlyphIndex == 0) MissingGlyph = c;
			}

			if (MissingGlyph == null) throw new Exception("No missing character glyph provided!");
		}

		public Glyph[] Glyphs { get; }

		static Glyph CreateFromRawGlyph(FontParser.GlyphRaw raw, float emScale)
		{
			Vector2 bottomLeft = new Vector2(raw.MinX, raw.MinY) * emScale;
			Vector2 topRight = new Vector2(raw.MaxX, raw.MaxY) * emScale;

			Glyph glyph = new()
			{
				UnicodeValue = raw.UnicodeValue,
				GlyphIndex = raw.GlyphIndex,
				AdvanceWidth = raw.AdvanceWidth * emScale,
				LeftSideBearing = raw.LeftSideBearing * emScale,
				Contours = GlyphHelper.ProcessContours(raw.ContourEndIndices, raw.Points, emScale),
				// Bounds
				BottomLeft = bottomLeft,
				TopRight = topRight,
				Centre = (bottomLeft + topRight) * 0.5f,
				Size = topRight - bottomLeft
			};
			return glyph;
		}

		public bool TryGetGlyph(uint unicode, out Glyph glyph)
		{
			bool found = glyphLookup.TryGetValue(unicode, out glyph);
			if (!found) glyph = MissingGlyph;
			return found;
		}
	}

	// Glyph data
	// All positions/sizes in Em units (arbitrary unit independent of font size).
	public class Glyph
	{
		public float AdvanceWidth;

		// Bounds and advance info (in Em units)
		public Vector2 BottomLeft;
		public Vector2 Centre;
		public Vector2[][] Contours;
		public uint GlyphIndex;
		public float LeftSideBearing;
		public Vector2 Size;
		public Vector2 TopRight;
		public uint UnicodeValue;

		public int NumContours => Contours.Length;
	}
}