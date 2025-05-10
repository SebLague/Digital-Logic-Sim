using System;
using Seb.Helpers;
using UnityEngine;

namespace Seb.Vis.UI
{
	public static class UIThemeLibrary
	{
		public enum ThemeName
		{
			RedTest,
			BlueTest
		}

		public const float FontSizeSmall = 1;
		public const float FontSizeMedium = 1.5f;
		public const float FontSizeLarge = 2;
		public const float FontSizeVeryLarge = 3;
		public const float FontSizeButton = FontSizeMedium;

		public const FontType DefaultFont = FontType.JetbrainsMonoBold;
		public const float FontSizeDefault = FontSizeMedium;

		public static readonly Vector2 PaddingScaleButton = new(1.2f, 1.5f);

		static readonly ThemeCols red = new(MakeCol(207, 101, 101), MakeCol(243, 168, 168), MakeCol(180, 90, 90), MakeCol(160, 130, 130));
		static readonly ThemeCols blue = new(MakeCol(101, 101, 207), MakeCol(168, 168, 243), MakeCol(90, 90, 180), MakeCol(130, 130, 160));

		public static readonly ButtonTheme RedTheme_Button = CreateButtonTheme(red);
		//public static readonly WheelSelectorTheme RedTheme_WheelSelector = CreateWheelSelectorTheme(red);

		public static UIThemeCLASS CreateTheme(ThemeName themeName)
		{
			return themeName switch
			{
				ThemeName.RedTest => CreateRedTheme(),
				ThemeName.BlueTest => CreateBlueTheme(),
				_ => throw new Exception(themeName + " not implemented")
			};
		}

		static UIThemeCLASS CreateRedTheme() => CreateTheme(ThemeName.RedTest, red);

		static UIThemeCLASS CreateBlueTheme() => CreateTheme(ThemeName.BlueTest, blue);

		static UIThemeCLASS CreateTheme(ThemeName themeName, ThemeCols cols)
		{
			ButtonTheme buttonTheme = CreateButtonTheme(cols);

			return new UIThemeCLASS(themeName)
			{
				buttonTheme = buttonTheme,
				wheelSelector = CreateWheelSelectorTheme(cols, buttonTheme),
				checkboxTheme = CreateCheckboxTheme(cols),
				inputFieldTheme = CreateInputFieldTheme(cols)
			};
		}

		static CheckboxTheme CreateCheckboxTheme(ThemeCols cols) =>
			new()
			{
				boxCol = Color.white,
				tickCol = Color.black
			};

		static InputFieldTheme CreateInputFieldTheme(ThemeCols cols) =>
			new()
			{
				bgCol = Color.white,
				defaultTextCol = Color.gray,
				font = DefaultFont,
				fontSize = FontSizeDefault,
				focusBorderCol = cols.AccentBright,
				textCol = Color.black
			};

		static ButtonTheme CreateButtonTheme(ThemeCols cols) =>
			new()
			{
				font = DefaultFont,
				fontSize = FontSizeDefault,
				textCols = new ButtonTheme.StateCols(Color.white, Color.white, Color.white, ColHelper.Brighten(cols.Inactive, 0.1f)),
				buttonCols = new ButtonTheme.StateCols(cols.Base, cols.AccentBright, cols.AccentDark, cols.Inactive),
				paddingScale = PaddingScaleButton
			};

		static WheelSelectorTheme CreateWheelSelectorTheme(ThemeCols cols, ButtonTheme buttonTheme) =>
			new()
			{
				buttonTheme = buttonTheme,
				backgroundCol = Color.white,
				textCol = Color.black
			};

		static Color MakeCol(int r, int g, int b)
		{
			const float scale = 1 / 255f;
			return new Color(r * scale, g * scale, b * scale, 1);
		}

		readonly struct ThemeCols
		{
			public readonly Color Base;
			public readonly Color AccentBright;
			public readonly Color AccentDark;
			public readonly Color Inactive;

			public ThemeCols(Color baseCol, Color accentBright, Color accentDark, Color inactive)
			{
				Base = baseCol;
				AccentBright = accentBright;
				AccentDark = accentDark;
				Inactive = inactive;
			}
		}
	}
}