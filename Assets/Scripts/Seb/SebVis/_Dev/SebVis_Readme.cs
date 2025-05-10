using System;
using Seb.Helpers;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace Seb.Readme
{
	// VERY WORK IN PROGRESS!!
	public static class SebVis_Readme
	{
		static Type[] dependencies =
		{
			typeof(Bounds2D),
			typeof(InputHelper),
			typeof(ComputeHelper)
		};

		//  -- DRAWING --
		static void Draw_Info()
		{
			// -- DRAWING --
			// Start drawing text/shapes by starting a new Layer.
			// When a layer is rendered, all the 2D shapes will be drawn first (in the order they were submitted), and all text 
			// for that layer willbe drawn afterwards. This means that it is impossible for a 2D shape to be drawn on top of text.
			// If this behaviour is required, a new layer must be started. There is some overhead to having multiple layers,
			// so this should be done reasonably sparingly.
			Draw.StartLayer(Vector2.zero, 1, false);
		}

		// -- UI --
		static void UI_Info()
		{
			// Start drawing UI elements by creating a UI scope.
			// Inside this scope, positions and sizes will be given in UISpace.
			// In this space, the canvas always has a width of 100 units (with the height depending on the given aspect ratio).
			using (UI.CreateFixedAspectUIScope())
			{
			}

			;
		}
	}
}