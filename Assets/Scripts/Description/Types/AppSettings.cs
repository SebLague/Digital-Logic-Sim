using UnityEngine;

namespace DLS.Description
{
	public struct AppSettings
	{
		public int ResolutionX;
		public int ResolutionY;
		public FullScreenMode fullscreenMode;
		public bool VSyncEnabled;

		public static AppSettings Default() =>
			new()
			{
				ResolutionX = 1920,
				ResolutionY = 1080,
				fullscreenMode = FullScreenMode.FullScreenWindow,
				VSyncEnabled = true
			};
	}
}