using System;
using System.Collections.Generic;
using Seb.Helpers;
using Seb.Vis.Text.FontLoading;
using Seb.Vis.Text.Rendering.Helpers;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Seb.Vis.Text.Rendering
{
	public class TextRenderer
	{
		const string ShaderFileName = "TextShader";

		static readonly int id_bezierBuffer = Shader.PropertyToID("BezierData");
		static readonly int id_metadataBuffer = Shader.PropertyToID("GlyphMetaData");
		static readonly int id_instanceDataBuffer = Shader.PropertyToID("PerInstanceData");
		static readonly int id_textGroupBuffer = Shader.PropertyToID("TextGroups");
		readonly List<InstanceData> perGlyphInstanceData = new();
		readonly List<TextGroup> textGroups = new();

		readonly TextRenderData textRenderData;

		ComputeBuffer argsBuffer;
		ComputeBuffer bezierBuffer;

		(Vector2 min, Vector2 max) bounds;
		Material fontMat;
		ComputeBuffer instanceDataBuffer;
		int lastInitFrame = -1;
		ComputeBuffer metadataBuffer;
		Mesh quadMesh;
		ComputeBuffer textGroupBuffer;

		// Create a text renderer that can share the font data it loads with other text renderers
		public TextRenderer(TextRenderData sharedTextRenderData)
		{
			textRenderData = sharedTextRenderData;
		}

		public TextRenderer()
		{
			textRenderData = new TextRenderData();
		}

		public Vector2 BoundsSize => bounds.max - bounds.min;

		void InitFrame()
		{
			if (Time.frameCount > lastInitFrame)
			{
				lastInitFrame = Time.frameCount;

				perGlyphInstanceData.Clear();
				textGroups.Clear();

				if (quadMesh == null) quadMesh = QuadMeshGenerator.GenerateQuadMesh();
				if (fontMat == null) fontMat = new Material(Resources.Load<Shader>(ShaderFileName));
			}
		}

		public void AddTextGroup(ReadOnlySpan<char> text, FontData fontData, LayoutSettings settings, Vector2 pos, Color textCol, bool useScreenspace, Vector2 maskMin, Vector2 maskMax, Anchor anchor)
		{
			InitFrame();
			BoundingBox localBounds = CreateTextLayout(text, fontData, settings, textCol, true, perGlyphInstanceData, textRenderData, textGroups.Count);
			Vector2 worldOffset = CalculateWorldOffset(localBounds, fontData, pos, anchor, settings);

			TextGroup groupData = new(worldOffset, maskMin, maskMax, useScreenspace ? 1 : 0);
			textGroups.Add(groupData);
		}

		public void Render(CommandBuffer cmd)
		{
			// Create buffers
			ComputeHelper.CreateArgsBuffer(ref argsBuffer, quadMesh, perGlyphInstanceData.Count);
			ComputeHelper.CreateStructuredBuffer_DontShrink(ref instanceDataBuffer, perGlyphInstanceData);
			ComputeHelper.CreateStructuredBuffer_DontShrink(ref bezierBuffer, textRenderData.bezierPoints);
			ComputeHelper.CreateStructuredBuffer_DontShrink(ref metadataBuffer, textRenderData.glyphMetadata);
			ComputeHelper.CreateStructuredBuffer_DontShrink(ref textGroupBuffer, textGroups);

			// Assign buffers
			fontMat.SetBuffer(id_bezierBuffer, bezierBuffer);
			fontMat.SetBuffer(id_metadataBuffer, metadataBuffer);
			fontMat.SetBuffer(id_instanceDataBuffer, instanceDataBuffer);
			fontMat.SetBuffer(id_textGroupBuffer, textGroupBuffer);

			// Render
			// Note: world bounds are: (globalOffset + boundsCentre, boundsSize)
			cmd.DrawMeshInstancedIndirect(quadMesh, 0, fontMat, 0, argsBuffer, 0);
		}

		public static BoundingBox CalculateLocalBounds(ReadOnlySpan<char> text, FontData fontData, LayoutSettings settings) => CreateTextLayout(text, fontData, settings);

		// Note: bounding box does not respect layer offset or scale
		public static BoundingBox CalculateWorldBounds(ReadOnlySpan<char> text, FontData fontData, LayoutSettings settings, Vector2 pos, Anchor anchor)
		{
			BoundingBox local = CalculateLocalBounds(text, fontData, settings);
			Vector2 worldOffset = CalculateWorldOffset(local, fontData, pos, anchor, settings);
			return new BoundingBox(local.BoundsMin + worldOffset, local.BoundsMax + worldOffset);
		}

		static Vector2 CalculateWorldOffset(BoundingBox localBounds, FontData fontData, Vector2 pos, Anchor anchor, LayoutSettings settings)
		{
			Vector2 centre = localBounds.Centre;
			Vector2 size = localBounds.Size;
			float halfWidth = size.x / 2;
			float halfHeight = size.y / 2;

			return anchor switch
			{
				Anchor.Centre => pos - centre,
				Anchor.CentreLeft => pos - centre + Vector2.right * halfWidth,
				Anchor.CentreRight => pos - centre - Vector2.right * halfWidth,
				Anchor.CentreTop => pos - centre - Vector2.up * halfHeight,
				Anchor.CentreBottom => pos - centre + Vector2.up * halfHeight,
				Anchor.TopLeft => pos - centre + new Vector2(halfWidth, -halfHeight),
				Anchor.TopRight => pos - centre + new Vector2(-halfWidth, -halfHeight),
				Anchor.BottomLeft => pos - centre + size / 2,
				Anchor.BottomRight => pos - centre + new Vector2(-halfWidth, halfHeight),
				Anchor.TextCentreLeft => pos - new Vector2(centre.x, CalculateLocalBounds("M".AsSpan(), fontData, settings).Size.y / 2) + new Vector2(size.x, 0) / 2,
				Anchor.TextCentreRight => pos - new Vector2(centre.x, CalculateLocalBounds("M".AsSpan(), fontData, settings).Size.y / 2) - new Vector2(size.x, 0) / 2,
				Anchor.TextFirstLineCentre => pos - new Vector2(centre.x, CalculateLocalBounds("M".AsSpan(), fontData, settings).Size.y / 2),
				Anchor.TextCentre => pos - new Vector2(centre.x, centre.y),
				_ => pos
			};
		}

		static BoundingBox CreateTextLayout(ReadOnlySpan<char> text, FontData fontData, LayoutSettings settings) => CreateTextLayout(text, fontData, settings, Color.white, false, null, null, 0);

		// Calculates positions of all glyphs and adds each to the supplied instances array.
		// Returns the local bounding box of the text (i.e. not transformed by anchor or world pos)
		// Note: can optionally avoid populating instance data list (list can be null in this case) if only care about bounding box.
		// Note2: this function also stores data about each glyph (bezier points, etc) in the supplied renderData object, as this is required for rendering the
		// glyphs, and it's convenient to do it here.
		static BoundingBox CreateTextLayout(ReadOnlySpan<char> text, FontData fontData, LayoutSettings settings, Color textCol, bool populateInstances, List<InstanceData> instances, TextRenderData renderData, int groupIndex)
		{
			Color currCol = textCol;
			Vector2 advanceEm = Vector2.zero;

			const float boundsMinX = 0;
			float boundsMaxX = boundsMinX;
			float boundsMinY = 0;
			float boundsMaxY = 0;

			for (int i = 0; i < text.Length; i++)
			{
				TextLayoutHelper.Info info = TextLayoutHelper.CalculateNextAdvance(text, i, fontData, settings, advanceEm);
				// Handle rich text chunks
				if (info.type is TextLayoutHelper.ChunkType.RichTextTag)
				{
					TextLayoutHelper.RichTextInfo richTextInfo = info.richTextInfo;
					i += richTextInfo.indexJump;

					if (richTextInfo.tagType is TextLayoutHelper.RichTextTagType.ColorBlockStart)
					{
						currCol = richTextInfo.richTextCol;
						continue;
					}
					else if (richTextInfo.tagType is TextLayoutHelper.RichTextTagType.ColorBlockEnd)
					{
						currCol = textCol;
						continue;
					}
				}

				Vector2 nextAdvanceEm = info.advance;
				boundsMaxX = Mathf.Max(boundsMaxX, nextAdvanceEm.x * settings.FontSize);

				if (info.type is TextLayoutHelper.ChunkType.Glyph)
				{
					Vector2 centre = (advanceEm + info.glyph.Centre) * settings.FontSize;
					Vector2 size = info.glyph.Size * settings.FontSize;
					boundsMinY = Mathf.Min(boundsMinY, centre.y - size.y / 2);
					boundsMaxY = Mathf.Max(boundsMaxY, centre.y + size.y / 2);

					// Create instance data for this glyph
					if (populateInstances)
					{
						// If this glyph has not been added yet, create all required data for rendering
						if (!renderData.glyphMetadataIndexLookup.TryGetValue(info.glyph, out int metadataIndex))
						{
							metadataIndex = renderData.glyphMetadata.Count;
							renderData.glyphMetadataIndexLookup.Add(info.glyph, metadataIndex);
							renderData.glyphMetadata.Add(renderData.bezierPoints.Count); // metadata: bezier data offset for this glyph
							renderData.glyphMetadata.Add(info.glyph.NumContours); // metadata: num contours in glyph

							foreach (Vector2[] contour in info.glyph.Contours)
							{
								renderData.glyphMetadata.Add(contour.Length - 1); // metadata: num points in contour (minus one)

								// Add glyph's bezier points to the list of all bezier points
								foreach (Vector2 contourPoint in contour)
								{
									renderData.bezierPoints.Add(contourPoint - info.glyph.Centre);
								}
							}
						}

						InstanceData instanceData = new(centre, info.glyph.Size, currCol, settings.FontSize, metadataIndex, groupIndex);
						instances.Add(instanceData);
					}
				}

				advanceEm = nextAdvanceEm;
			}

			return new BoundingBox(new Vector2(boundsMinX, boundsMinY), new Vector2(boundsMaxX, boundsMaxY));
		}


		public void Release()
		{
			ComputeHelper.Release(argsBuffer, instanceDataBuffer, bezierBuffer, metadataBuffer, textGroupBuffer);

			if (fontMat != null)
			{
				if (Application.isPlaying) Object.Destroy(fontMat);
				else Object.DestroyImmediate(fontMat);
			}
		}

		public readonly struct InstanceData
		{
			public readonly Vector2 pos;
			public readonly Vector2 sizeEm;
			public readonly Color col;
			public readonly float fontSize;
			public readonly int dataOffset;
			public readonly int groupIndex;

			public InstanceData(Vector2 pos, Vector2 sizeEm, Color col, float fontSize, int dataOffset, int groupIndex)
			{
				this.pos = pos;
				this.sizeEm = sizeEm;
				this.fontSize = fontSize;
				this.dataOffset = dataOffset;
				this.groupIndex = groupIndex;
				this.col = col;
			}
		}

		public readonly struct TextGroup
		{
			public readonly Vector2 Offset;
			public readonly Vector2 MaskMin;
			public readonly Vector2 MaskMax;
			public readonly int UseScreenSpace;

			public TextGroup(Vector2 offset, Vector2 maskMin, Vector2 maskMax, int useScreenSpace)
			{
				Offset = offset;
				MaskMin = maskMin;
				MaskMax = maskMax;
				UseScreenSpace = useScreenSpace;
			}
		}

		public readonly struct BoundingBox
		{
			public readonly Vector2 BoundsMin;
			public readonly Vector2 BoundsMax;

			public Vector2 Centre => (BoundsMin + BoundsMax) * 0.5f;
			public Vector2 Size => BoundsMax - BoundsMin;

			public BoundingBox(Vector2 boundsMin, Vector2 boundsMax)
			{
				BoundsMin = boundsMin;
				BoundsMax = boundsMax;
			}
		}

		[Serializable]
		public struct LayoutSettings : IEquatable<LayoutSettings>
		{
			public float FontSize;
			public float LineSpacing;
			public float LetterSpacing;
			public float WordSpacing;

			public LayoutSettings(float fontSize, float lineSpacing, float letterSpacing, float wordSpacing)
			{
				FontSize = fontSize;
				LineSpacing = lineSpacing;
				LetterSpacing = letterSpacing;
				WordSpacing = wordSpacing;
			}

			public bool Equals(LayoutSettings other) => FontSize == other.FontSize && LineSpacing == other.LineSpacing && LetterSpacing == other.LetterSpacing && WordSpacing == other.WordSpacing;

			public static LayoutSettings CreateDefault(float fontSize) => new(fontSize, 1, 1, 1);
		}
	}
}