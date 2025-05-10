using UnityEngine;

namespace Seb.Vis.UI.Examples
{
	public class ThemeSelector : MonoBehaviour
	{
		public UIThemeLibrary.ThemeName themeName;
		UIThemeCLASS activeTheme;

		public UIThemeCLASS ActiveTheme
		{
			get
			{
				if (activeTheme == null || activeTheme.ThemeName != themeName)
				{
					activeTheme = UIThemeLibrary.CreateTheme(themeName);
				}

				return activeTheme;
			}
		}
	}
}