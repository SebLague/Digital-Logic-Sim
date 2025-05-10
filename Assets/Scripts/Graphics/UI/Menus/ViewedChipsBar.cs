using DLS.Game;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class ViewedChipsBar
	{
		const float pad = 0.75f;

		public static void DrawViewedChipsBanner(Project project, bool simPausedBannerIsActive)
		{
			Vector2 topLeft = UI.TopLeft;
			if (simPausedBannerIsActive) topLeft += Vector2.down * InfoBarHeight;

			UI.DrawPanel(topLeft, new Vector2(UI.Width, InfoBarHeight), ActiveUITheme.InfoBarCol, Anchor.TopLeft);


			Vector2 pos = new(pad, topLeft.y - InfoBarHeight / 2);
			UI.DrawText(project.viewedChipsString, ActiveUITheme.FontBold, ActiveUITheme.ButtonTheme.fontSize, pos, Anchor.TextCentreLeft, Color.white);

			// Back button
			Vector2 buttonSize = new(8, InfoBarHeight - pad);
			Vector2 buttonCentreRight = new(UI.Width - pad, pos.y);
			bool backButtonPressed = UI.Button("Back", ActiveUITheme.ChipButton, buttonCentreRight, buttonSize, true, false, false, Anchor.CentreRight);

			if (backButtonPressed || KeyboardShortcuts.CancelShortcutTriggered)
			{
				project.ReturnToPreviousViewedChip();
			}
		}
	}
}