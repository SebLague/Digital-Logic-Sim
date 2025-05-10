using DLS.Game;
using Seb.Vis;
using UnityEngine;

namespace DLS.Graphics
{
	public static class WorldDrawer
	{
		public static void DrawWorld(Project project)
		{
			Draw.StartLayer(Vector2.zero, 1, false);


			if (UIDrawer.ActiveMenu is UIDrawer.MenuType.ChipCustomization)
			{
				CustomizationSceneDrawer.DrawCustomizationScene();
			}
			else
			{
				DevSceneDrawer.DrawActiveScene();
			}
		}

		public static void DrawGridIfActive(Color col)
		{
			bool isSnapping = KeyboardShortcuts.SnapModeHeld && Project.ActiveProject.controller.IsPlacingOrMovingElementOrCreatingWire;

			if (Project.ActiveProject.ShowGrid || isSnapping)
			{
				DevSceneDrawer.DrawGrid(col);
			}
		}

		public static void Reset()
		{
			CustomizationSceneDrawer.Reset();
		}
	}
}