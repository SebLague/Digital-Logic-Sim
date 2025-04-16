using System;
using DLS.Description;
using DLS.Game;
using Seb.Types;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class PreferencesMenu
	{
		const float entrySpacing = 0.5f;
		const float menuWidth = 55;
		const float verticalOffset = 18;

		public const int DisplayMode_Always = 0;
		public const int DisplayMode_OnHover = 1;
		public const int DisplayMode_Never = 2;

		static readonly string[] PinDisplayOptions =
		{
			"Always",
			"On Hover",
			"Never"
		};

		static readonly string[] GridDisplayOptions =
		{
			"Off",
			"On"
		};

		static readonly string[] SimulationStatusOptions =
		{
			"Active",
			"Paused"
		};

		static readonly Vector2 entrySize = new(menuWidth, DrawSettings.SelectorWheelHeight);
		public static readonly Vector2 settingFieldSize = new(entrySize.x / 3, entrySize.y);

		// ---- State ----
		static readonly UIHandle ID_MainPinNames = new("PREFS_MainPinNames");
		static readonly UIHandle ID_ChipPinNames = new("PREFS_ChipPinNames");
		static readonly UIHandle ID_GridDisplay = new("PREFS_GridDisplay");
		static readonly UIHandle ID_SimStatus = new("PREFS_SimStatus");
		static readonly UIHandle ID_SimFrequencyField = new("PREFS_SimTickTarget");
		static readonly UIHandle ID_ClockSpeedInput = new("PREFS_ClockSpeed");
		static readonly Func<string, bool> integerInputValidator;

		static double simAvgTicksPerSec_delayedRefreshForUI;
		static float lastSimAvgTicksPerSecRefreshTime;
		static float lastSimTickRateSetTime;
		static ProjectDescription originalProjectDesc;
		static string currentSimSpeedString = string.Empty;
		static Color currentSimSpeedStringColour;


		public static void DrawMenu(Project project)
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			MenuHelper.DrawBackgroundOverlay();
			Draw.ID panelID = UI.ReservePanel();
			UpdateSimSpeedString(project);

			const int inputTextPad = 1;
			const float headerSpacing = 1.5f;
			Color labelCol = Color.white;
			Color headerCol = new(0.46f, 1, 0.54f);
			Vector2 topLeft = UI.Centre + new Vector2(-menuWidth / 2, verticalOffset);
			Vector2 labelPosCurr = topLeft;

			using (UI.BeginBoundsScope(true))
			{
				// ---- Draw settings ----
				DrawHeader("DISPLAY:");
				int mainPinNamesMode = MenuHelper.LabeledOptionsWheel("Show I/O pin names", labelCol, labelPosCurr, entrySize, ID_MainPinNames, PinDisplayOptions, settingFieldSize.x, true);
				AddSpacing();
				int chipPinNamesMode = MenuHelper.LabeledOptionsWheel("Show chip pin names", labelCol, labelPosCurr, entrySize, ID_ChipPinNames, PinDisplayOptions, settingFieldSize.x, true);
				AddSpacing();
				int gridDisplayMode = MenuHelper.LabeledOptionsWheel("Show grid", labelCol, labelPosCurr, entrySize, ID_GridDisplay, GridDisplayOptions, settingFieldSize.x, true);
				AddSpacing();

				DrawHeader("SIMULATION:");
				bool pauseSim = MenuHelper.LabeledOptionsWheel("Status", labelCol, labelPosCurr, entrySize, ID_SimStatus, SimulationStatusOptions, settingFieldSize.x, true) == 1;
				AddSpacing();
				InputFieldState clockSpeedInputFieldState = MenuHelper.LabeledInputField("Steps per clock tick", labelCol, labelPosCurr, entrySize, ID_ClockSpeedInput, integerInputValidator, settingFieldSize.x, true);
				AddSpacing();
				InputFieldState freqState = MenuHelper.LabeledInputField("Steps per second (target)", labelCol, labelPosCurr, entrySize, ID_SimFrequencyField, integerInputValidator, settingFieldSize.x, true);
				AddSpacing();
				// Draw current simulation speed
				Vector2 tickLabelRight = MenuHelper.DrawLabelSectionOfLabelInputPair(labelPosCurr, entrySize, "Steps per second (current)", labelCol * 0.75f, true);
				UI.DrawPanel(tickLabelRight, settingFieldSize, new Color(0.18f, 0.18f, 0.18f), Anchor.CentreRight);
				UI.DrawText(currentSimSpeedString, theme.FontBold, theme.FontSizeRegular, tickLabelRight + new Vector2(inputTextPad - settingFieldSize.x, 0), Anchor.TextCentreLeft, currentSimSpeedStringColour);

				// Draw cancel/confirm buttons
				Vector2 buttonTopLeft = new(labelPosCurr.x, UI.PrevBounds.Bottom);
				MenuHelper.CancelConfirmResult result = MenuHelper.DrawCancelConfirmButtons(buttonTopLeft, menuWidth, true);

				// Draw menu background
				Bounds2D menuBounds = UI.GetCurrentBoundsScope();
				MenuHelper.DrawReservedMenuPanel(panelID, menuBounds);

				// ---- Handle changes ----
				int.TryParse(clockSpeedInputFieldState.text, out int clockSpeed);

				// Parse target sim tick rate
				int.TryParse(freqState.text, out int targetSimTicksPerSecond);
				targetSimTicksPerSecond = Mathf.Max(1, targetSimTicksPerSecond);
				if (project.targetTicksPerSecond != targetSimTicksPerSecond || project.simPaused != pauseSim) lastSimTickRateSetTime = Time.time;

				// Assign changes immediately so can see them take effect in background
				project.description.Prefs_MainPinNamesDisplayMode = mainPinNamesMode;
				project.description.Prefs_ChipPinNamesDisplayMode = chipPinNamesMode;
				project.description.Prefs_GridDisplayMode = gridDisplayMode;
				project.description.Prefs_SimTargetStepsPerSecond = targetSimTicksPerSecond;
				project.description.Prefs_SimStepsPerClockTick = clockSpeed;
				project.description.Prefs_SimPaused = pauseSim;

				// Cancel / Confirm
				if (result == MenuHelper.CancelConfirmResult.Cancel)
				{
					// Restore original description
					project.description = originalProjectDesc;
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
				else if (result == MenuHelper.CancelConfirmResult.Confirm)
				{
					// Save changes
					project.UpdateAndSaveProjectDescription(project.description);
					UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
				}
			}

			return;

			void DrawHeader(string text)
			{
				AddHeaderSpacing();
				UI.DrawText(text, theme.FontBold, theme.FontSizeRegular, labelPosCurr, Anchor.TextCentreLeft, headerCol);
				AddHeaderSpacing();
			}

			void AddSpacing()
			{
				labelPosCurr.y -= entrySize.y + entrySpacing;
			}

			void AddHeaderSpacing()
			{
				labelPosCurr.y -= headerSpacing;
			}
		}

		public static void OnMenuOpened()
		{
			ProjectDescription projDesc = Project.ActiveProject.description;
			originalProjectDesc = projDesc;

			// Init ui from description
			// -- Wheels
			UI.GetWheelSelectorState(ID_MainPinNames).index = projDesc.Prefs_MainPinNamesDisplayMode;
			UI.GetWheelSelectorState(ID_ChipPinNames).index = projDesc.Prefs_ChipPinNamesDisplayMode;
			UI.GetWheelSelectorState(ID_GridDisplay).index = projDesc.Prefs_GridDisplayMode;
			UI.GetWheelSelectorState(ID_SimStatus).index = projDesc.Prefs_SimPaused ? 1 : 0;
			// -- Input fields
			UI.GetInputFieldState(ID_SimFrequencyField).SetText(projDesc.Prefs_SimTargetStepsPerSecond + "", false);
			UI.GetInputFieldState(ID_ClockSpeedInput).SetText(projDesc.Prefs_SimStepsPerClockTick + "", false);

			simAvgTicksPerSec_delayedRefreshForUI = Project.ActiveProject.simAvgTicksPerSec;
			lastSimAvgTicksPerSecRefreshTime = float.MinValue;
		}

		static void UpdateSimSpeedString(Project project)
		{
			// Annoying if sim tick rate value flickers too much, so use slower refresh rate for ui
			// (but if sim target rate has been recently changed, update fast so doesn't feel laggy)
			bool slowModeSimUI = Time.time - lastSimTickRateSetTime > Project.SimulationPerformanceTimeWindowSec;
			const float slowModeRefreshDelay = 0.35f;
			const float fastModeRefreshDelay = 0.05f;
			float refreshDelay = slowModeSimUI ? slowModeRefreshDelay : fastModeRefreshDelay;

			if (Time.time > lastSimAvgTicksPerSecRefreshTime + refreshDelay)
			{
				simAvgTicksPerSec_delayedRefreshForUI = project.simAvgTicksPerSec;
				lastSimAvgTicksPerSecRefreshTime = Time.time;
				currentSimSpeedString = project.simPaused ? "0" : $"{simAvgTicksPerSec_delayedRefreshForUI:0}";
				currentSimSpeedStringColour = GetSimFrequencyErrorCol();
			}
		}

		public static bool ValidateIntegerInput(string s)
		{
			if (string.IsNullOrEmpty(s)) return true;
			if (s.Contains(" ")) return false;
			return int.TryParse(s, out _);
		}

		public static Color GetSimFrequencyErrorCol()
		{
			Color frequencyErrorCol = new(0.3f, 0.92f, 0.32f);
			if (!Project.ActiveProject.simPaused)
			{
				int simFreqError = Mathf.RoundToInt(Project.ActiveProject.targetTicksPerSecond - (float)Project.ActiveProject.simAvgTicksPerSec);
				if (simFreqError > 10) frequencyErrorCol = new Color(0.95f, 0.25f, 0.13f);
				else if (simFreqError > 5) frequencyErrorCol = new Color(1, 0.38f, 0.27f);
				else if (simFreqError > 2) frequencyErrorCol = new Color(1, 0.7f, 0.27f);
			}

			return frequencyErrorCol;
		}
	}
}