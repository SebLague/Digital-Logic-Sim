using System;
using System.Collections.Generic;
using Seb.Types;
using Seb.Vis.Internal;
using Seb.Vis.Text.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seb.Vis
{
	public class TextDrawManager : Drawer<TextDrawData>
	{
		static readonly TextRenderData sharedTextRenderData = new();

		// Holds text render for each layer as they are added.
		// Drawing the renderers from the pool early (i.e. before rendering) is done so that
		// the appropriate renderer is available in case calculating text bounding box is desired.
		readonly Queue<TextRenderer> textLayerRenderers = new();
		readonly Pool<TextRenderer> textRendererPool = new(CreateNewTextRenderer);

		public TextRenderer CurrentLayerTextRenderer => textLayerRenderers.Peek();

		static TextRenderer CreateNewTextRenderer() => new(sharedTextRenderData);

		protected override void InitFrame()
		{
			ReleaseUnusedTextRenderers();
			textRendererPool.ReturnAll();
			textLayerRenderers.Clear();
		}

		protected override void DrawLayer(CommandBuffer cmd, int startIndex, int count, Draw.LayerInfo layerInfo)
		{
			if (count == 0) return;

			TextRenderer renderer = textLayerRenderers.Dequeue();

			for (int i = startIndex; i < startIndex + count; i++)
			{
				TextDrawData data = allDrawData[i];
				TextRenderer.LayoutSettings layoutSettings = new(data.fontSize * layerInfo.scale, data.lineSpacing * layerInfo.scale, 1 * layerInfo.scale, 1 * layerInfo.scale);
				Vector2 pos = data.pos + layerInfo.offset;
				Vector2 maskMin = data.maskMin + layerInfo.offset;
				Vector2 maskMax = data.maskMax + layerInfo.offset;

				ReadOnlySpan<char> text = data.useCharArray ? data.charArray.AsSpan(0, data.textLength) : data.text.AsSpan();
				renderer.AddTextGroup(text, data.fontData, layoutSettings, pos, data.col, layerInfo.useScreenSpace, maskMin, maskMax, data.anchor);
			}

			renderer.Render(cmd);
		}

		protected override void OnLayerAdded(Draw.LayerInfo layerInfo)
		{
			base.OnLayerAdded(layerInfo);
			TextRenderer layerRenderer = textRendererPool.GetNextAvailableOrCreate();
			textLayerRenderers.Enqueue(layerRenderer);
		}

		public override void Release()
		{
			base.Release();
			textRendererPool.ReturnAll();
			ReleaseUnusedTextRenderers();
		}

		void ReleaseUnusedTextRenderers()
		{
			while (textRendererPool.HasAvailable())
			{
				textRendererPool.PurgeNextAvailable().Release();
			}
		}
	}
}