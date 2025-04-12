using UnityEngine;

namespace Seb.Vis.UI.Examples
{
	[ExecuteAlways]
	public class WheelSelectorExample : MonoBehaviour
	{
		public ThemeSelector themeSelector;
		public Vector2 pos;
		public Vector2 size;
		public Color col;
		public Color textCol;
		public Anchor anchor;
		public bool allowWrapAround = true;

		public string[] elements;

		void Update()
		{
			using (UI.CreateFixedAspectUIScope())
			{
				DrawWheel();
			}
		}

		void DrawWheel()
		{
			UIThemeCLASS theme = themeSelector.ActiveTheme;

			UIHandle id = new("WheelSelector");
			UI.WheelSelector(id, elements, pos, size, theme.wheelSelector, anchor, allowWrapAround);
		}
	}
}