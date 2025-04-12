using UnityEngine;

namespace Seb.Vis.UI.Examples
{
	[ExecuteAlways]
	public class InputFieldExample : MonoBehaviour
	{
		public ThemeSelector themeSelector;
		public Vector2 pos;
		public Vector2 padding;
		public Anchor anchor;
		public int maxLength;

		void Update()
		{
			using (UI.CreateFixedAspectUIScope())
			{
				DrawField();
			}
		}

		void DrawField()
		{
			UIHandle id = new("InputFieldID");

			InputFieldTheme theme = themeSelector.ActiveTheme.inputFieldTheme;
			Vector2 charSize = UI.CalculateTextSize("M", theme.fontSize, theme.font);
			Vector2 size = new Vector2(charSize.x * maxLength, charSize.y) + padding * 2;

			UI.InputField(id, theme, pos, size, "Default text", anchor, padding.x, Validator);
		}

		bool Validator(string inputString) => inputString.Length <= maxLength;
	}
}