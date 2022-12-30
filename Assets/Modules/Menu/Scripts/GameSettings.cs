using UnityEngine;

namespace DLS.MainMenu
{
	public struct GameSettings
	{
		public Vector2Int ScreenSize;
		public bool IsFullscreen;
		public bool VSyncEnabled;

		public void LoadSavedSettings()
		{
			// Screen settings (note: Unity remembers these automatically, so just lookup from Screen class)
			ScreenSize = new Vector2Int(Screen.width, Screen.height);
			IsFullscreen = Screen.fullScreen;

			// Other settings
			VSyncEnabled = PlayerPrefs.GetInt(nameof(VSyncEnabled), defaultValue: 1) == 1;
		}

		public void Save()
		{
			// Note: Unity remembers screen settings automatically, don't need to save screenSize/fullscreenMode
			PlayerPrefs.SetInt(nameof(VSyncEnabled), (VSyncEnabled) ? 1 : 0);

			// Write
			PlayerPrefs.Save();
		}
	}
}