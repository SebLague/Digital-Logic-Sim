using System;
using Seb.Helpers;
using Seb.Vis.Text.FontLoading;
using UnityEngine;

namespace Seb.Vis.Text.Rendering
{
	public static class TextLayoutHelper
	{
		public enum ChunkType
		{
			Empty,
			Glyph,
			RichTextTag
		}

		public enum RichTextTagType
		{
			None,
			ColorBlockStart,
			ColorBlockEnd,
			HalfSpace
		}

		const float SpaceSizeEM = 0.333f;
		const float LineHeightEM = 1.3f;

		static float SpaceSize(FontData fontData) => fontData.IsMonospaced ? fontData.MonospacedAdvanceWidth : SpaceSizeEM;

		public static Info CalculateNextAdvance(ReadOnlySpan<char> text, int index, FontData fontData, TextRenderer.LayoutSettings settings, Vector2 advance)
		{
			Info info = new()
			{
				type = ChunkType.Empty,
				advance = advance
			};

			char c = text[index];

			info.richTextInfo = RichTextTagParse(text, index);

			if (info.richTextInfo.tagType == RichTextTagType.None)
			{
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
			}
			else
			{
				info.type = ChunkType.RichTextTag;
				
				if (info.richTextInfo.tagType == RichTextTagType.HalfSpace)
				{
					info.advance.x += SpaceSize(fontData) * settings.WordSpacing * 0.5f;
				}
			}


			return info;
		}

		public static RichTextInfo RichTextTagParse(ReadOnlySpan<char> text, int index)
		{
			char c = text[index];
			RichTextInfo info = new();

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
								info.indexJump = endBracketIndex - index;
								info.tagType = RichTextTagType.ColorBlockStart;
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
						info.tagType = RichTextTagType.ColorBlockEnd;
						info.indexJump = colBlockEndString.Length - 1;
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
						info.tagType = RichTextTagType.HalfSpace;
						info.indexJump = halfSpaceString.Length - 1;
						return info;
					}
				}
			}

			return info;
		}

		public struct Info
		{
			public Vector2 advance;
			public Glyph glyph;
			public ChunkType type;
			public RichTextInfo richTextInfo;
		}

		public struct RichTextInfo
		{
			public RichTextTagType tagType;
			public int indexJump;
			public Color richTextCol;
		}
	}
}