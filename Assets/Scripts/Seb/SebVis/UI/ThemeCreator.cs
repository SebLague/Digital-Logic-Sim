using UnityEngine;

namespace Seb.Vis.UI
{
	public class ThemeCreator
	{
		public const float FontSizeSmall = 1;
		public const float FontSizeMedium = 1.5f;
		public const float FontSizeLarge = 2;
		public const float FontSizeVeryLarge = 3;
		public const float FontSizeButton = FontSizeMedium;

		public static readonly Vector2 PaddingScaleButton = new(1.2f, 1.5f);

		public readonly UITheme ThemeA;

		public ThemeCreator(FontType font)
		{
			ThemeA = CreateThemeA(font);
		}

		static UITheme CreateThemeA(FontType font)
		{
			Color colBase = MakeCol(87, 100, 144);
			Color colAccentBright = MakeCol(163, 188, 249);
			Color colAccentDark = MakeCol(119, 150, 203);
			Color colInactive = MakeCol(142, 146, 148);
			Color colBG = MakeCol(48, 50, 58);

			return CreateTheme(font, colBG, colBase, colAccentBright, colAccentDark, colInactive);
		}

		static UITheme CreateTheme(FontType font, Color colBG, Color colNormal, Color colAccentBright, Color colAccentDark, Color colInactive) =>
			new()
			{
				fontSizeSmall = FontSizeSmall,
				fontSizeMedium = FontSizeMedium,
				fontSizeLarge = FontSizeLarge,
				colBG = colBG,

				buttonTheme = new ButtonTheme
				{
					font = font,
					fontSize = FontSizeMedium,
					textCols = new ButtonTheme.StateCols(Color.white, Color.white, Color.white, Brighten(colInactive, 0.1f)),
					buttonCols = new ButtonTheme.StateCols(colNormal, colAccentBright, colAccentDark, colInactive),
					paddingScale = PaddingScaleButton
				},

				inputFieldTheme = new InputFieldTheme
				{
					font = font,
					fontSize = FontSizeMedium,
					bgCol = Color.white,
					defaultTextCol = MakeCol(149, 150, 150),
					textCol = Color.black,
					focusBorderCol = colAccentBright
				}
			};


		static Color MakeCol(int r, int g, int b)
		{
			const float scale = 1 / 255f;
			return new Color(r * scale, g * scale, b * scale, 1);
		}

		static Color Darken(Color col, float darkenAmount, float desaturateAmount = 0) => TweakHSV(col, 0, -desaturateAmount, -darkenAmount);

		static Color Brighten(Color col, float brightenAmount) => TweakHSV(col, 0, 0, brightenAmount);

		static Color TweakHSV(Color col, float deltaH, float deltaS, float deltaV)
		{
			Color.RGBToHSV(col, out float h, out float s, out float v);
			h = (h + deltaH + 1) % 1;
			s = Mathf.Clamp01(s + deltaS);
			v = Mathf.Clamp01(v + deltaV);
			return Color.HSVToRGB(h, s, v);
		}
	}
}