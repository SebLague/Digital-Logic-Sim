using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Graphics
{
	public static class SimPausedUI
	{
		static int stepCountPrev;
		static string stepString;
		
		public static void DrawPausedBanner()
		{
			UI.DrawPanel(UI.TopLeft, new Vector2(UI.Width, InfoBarHeight), ActiveUITheme.InfoBarCol, Anchor.TopLeft);
			Bounds2D panelBounds = UI.PrevBounds;

			UI.DrawText("Simulation Paused <color=#886600ff>(press space to advance one step)", MenuHelper.Theme.FontBold, MenuHelper.Theme.FontSizeRegular, panelBounds.Centre, Anchor.TextCentre, Color.yellow);

			if (stepCountPrev != Project.ActiveProject.simPausedSingleStepCounter || string.IsNullOrEmpty(stepString))
			{
				stepCountPrev = Project.ActiveProject.simPausedSingleStepCounter;
				stepString = Project.ActiveProject.simPausedSingleStepCounter + "";
			}

			Vector2 frameLabelPos = panelBounds.CentreRight + Vector2.left * 1;
			UI.DrawText(stepString, ActiveUITheme.FontBold, ActiveUITheme.FontSizeRegular, frameLabelPos, Anchor.TextCentreRight, Color.white * 0.8f);
		}
	}
}