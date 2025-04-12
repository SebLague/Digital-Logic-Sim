using System;
using Seb.Helpers;
using Seb.Vis.Text.FontLoading;
using UnityEngine;

namespace Seb.Vis.Text.Rendering
{
	public static class LayoutHelper
	{
		public enum ChunkType
		{
			Empty,
			Glyph,
			ColorBlockStart,
			ColorBlockEnd,
			HalfSpace
		}

		const float SpaceSizeEM = 0.333f;
		const float LineHeightEM = 1.3f;

		static float SpaceSize(FontData fontData) => fontData.IsMonospaced ? fontData.MonospacedAdvanceWidth : SpaceSizeEM;

		public static Info CalculateNextAdvance(ReadOnlySpan<char> text, int index, FontData fontData, TextRenderer.LayoutSettings settings, Vector2 advance)
		{
			Info info = new();
			info.type = ChunkType.Empty;

			info.advance = advance;
			char c = text[index];

			// rich text search
			if (c == '<')
			{
				int charsRemaining = text.Length - index;

				// Test for color block: <color=#RRGGBBAA>
				const string colString = "<color=#";
				if (charsRemaining >= colString.Length)
				{
					ReadOnlySpan<char> slice = text.Slice(index, colString.Length);
					// Matches color pattern
					if (slice.SequenceEqual(colString))
					{
						int endBracketIndex = index + text.Slice(index, charsRemaining).IndexOf('>');
						if (endBracketIndex != -1)
						{
							int colCodeStartIndex = index + colString.Length;
							ReadOnlySpan<char> colCode = text[colCodeStartIndex..endBracketIndex];
							if (ColHelper.TryParseHexCode(colCode, out Color col))
							{
								info.richTextCol = col;
								info.richTextIndexJump = endBracketIndex - index;
								info.type = ChunkType.ColorBlockStart;
								return info;
							}
						}
					}
				}

				// Test for end of color block
				const string colBlockEndString = "</color>";
				if (charsRemaining >= colBlockEndString.Length)
				{
					ReadOnlySpan<char> slice = text.Slice(index, colBlockEndString.Length);
					// Matches color pattern
					if (slice.SequenceEqual(colBlockEndString))
					{
						info.type = ChunkType.ColorBlockEnd;
						info.richTextIndexJump = colBlockEndString.Length - 1;
						return info;
					}
				}

				// Test for half-space special character
				const string halfSpaceString = "<halfSpace>";
				if (charsRemaining >= halfSpaceString.Length)
				{
					ReadOnlySpan<char> slice = text.Slice(index, halfSpaceString.Length);
					// Matches pattern
					if (slice.SequenceEqual(halfSpaceString))
					{
						info.type = ChunkType.HalfSpace;
						info.richTextIndexJump = halfSpaceString.Length - 1;
						info.advance.x += SpaceSize(fontData) * settings.WordSpacing * 0.5f;
						return info;
					}
				}
			}

			if (c == ' ')
			{
				info.advance.x += SpaceSize(fontData) * settings.WordSpacing;
			}
			else if (c == '\t')
			{
				info.advance.x += SpaceSize(fontData) * 4 * settings.WordSpacing; // TODO: proper tab implementation
			}
			else if (c == '\n')
			{
				info.advance.y -= LineHeightEM * settings.LineSpacing;
				info.advance.x = 0;
			}
			else if (!char.IsControl(c))
			{
				info.type = ChunkType.Glyph;
				fontData.TryGetGlyph(c, out info.glyph);
				info.advance.x += info.glyph.AdvanceWidth * settings.LetterSpacing;
			}


			return info;
		}

		public struct Info
		{
			public Vector2 advance;
			public Glyph glyph;
			public ChunkType type;
			public int richTextIndexJump;
			public Color richTextCol;
		}
	}
}