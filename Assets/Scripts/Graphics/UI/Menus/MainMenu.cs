using System;
using System.Linq;
using DLS.Description;
using DLS.Game;
using DLS.SaveSystem;
using DLS.Simulation;
using Seb.Helpers;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Graphics
{
	public static class MainMenu
	{
		public const int MaxProjectNameLength = 20;
		const bool capitalize = true;

		static MenuScreen activeMenuScreen = MenuScreen.Main;
		static PopupKind activePopup = PopupKind.None;
		static AppSettings EditedAppSettings;

		static readonly UIHandle ID_ProjectNameInput = new("MainMenu_ProjectNameInputField");
		static readonly UIHandle ID_DisplayResolutionWheel = new("MainMenu_DisplayResolutionWheel");
		static readonly UIHandle ID_FullscreenWheel = new("MainMenu_FullscreenWheel");
		static readonly UIHandle ID_ProjectsScrollView = new("MainMenu_ProjectsScrollView");

		static readonly string[] SettingsWheelFullScreenOptions = { "OFF", "MAXIMIZED", "BORDERLESS", "EXCLUSIVE" };
		static readonly FullScreenMode[] FullScreenModes = { FullScreenMode.Windowed, FullScreenMode.MaximizedWindow, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
		static readonly string[] SettingsWheelVSyncOptions = { "DISABLED", "ENABLED" };

		static readonly Func<string, bool> projectNameValidator = ProjectNameValidator;
		static readonly UI.ScrollViewDrawContentFunc loadProjectScrollViewDrawer = DrawAllProjectsInScrollView;


		static readonly string[] menuButtonNames =
		{
			FormatButtonString("New Project"),
			FormatButtonString("Open Project"),
			FormatButtonString("Settings"),
			FormatButtonString("About"),
			FormatButtonString("Quit")
		};

		static readonly string[] openProjectButtonNames =
		{
			FormatButtonString("Back"),
			FormatButtonString("Delete"),
			FormatButtonString("Duplicate"),
			FormatButtonString("Rename"),
			FormatButtonString("Open")
		};

		static readonly Vector2Int[] Resolutions =
		{
			new(960, 540),
			new(1280, 720),
			new(1920, 1080),
			new(2560, 1440)
		};

		static readonly string[] ResolutionNames = Resolutions.Select(r => ResolutionToString(r)).ToArray();
		static readonly string[] FullScreenResName = Resolutions.Select(r => ResolutionToString(Main.FullScreenResolution)).ToArray();
		static readonly string[] settingsButtonGroupNames = { "EXIT", "APPLY" };
		static readonly bool[] settingsButtonGroupStates = new bool[settingsButtonGroupNames.Length];

		static readonly bool[] openProjectButtonStates = new bool[openProjectButtonNames.Length];

		static ProjectDescription[] allProjectDescriptions;
		static string[] allProjectNames;
		static (bool compatible, string message)[] projectCompatibilities;

		static int selectedProjectIndex;

		static readonly string authorString = "Created by: Sebastian Lague";
		static readonly string versionString = $"Version: {Main.DLSVersion} ({Main.LastUpdatedString})";
		static string SelectedProjectName => allProjectDescriptions[selectedProjectIndex].ProjectName;

		static string FormatButtonString(string s) => capitalize ? s.ToUpper() : s;

		public static void Draw()
		{
			Simulator.UpdateInPausedState();
			
			if (KeyboardShortcuts.CancelShortcutTriggered && activePopup == PopupKind.None)
			{
				BackToMain();
			}

			UI.DrawFullscreenPanel(ColHelper.MakeCol255(47, 47, 53));
			const string title = "DIGITAL LOGIC SIM";
			const float titleFontSize = 11.5f;
			const float titleHeight = 24;
			const float shaddowOffset = -0.33f;
			Color shadowCol = ColHelper.MakeCol255(87, 94, 230);

			UI.DrawText(title, FontType.Born2bSporty, titleFontSize, UI.Centre + Vector2.up * (titleHeight + shaddowOffset), Anchor.CentreTop, shadowCol);
			UI.DrawText(title, FontType.Born2bSporty, titleFontSize, UI.Centre + Vector2.up * titleHeight, Anchor.CentreTop, Color.white);
			DrawVersionInfo();

			switch (activeMenuScreen)
			{
				case MenuScreen.Main:
					DrawMainScreen();
					break;
				case MenuScreen.LoadProject:
					DrawLoadProjectScreen();
					break;
				case MenuScreen.Settings:
					DrawSettingsScreen();
					break;
				case MenuScreen.About:
					DrawAboutScreen();
					break;
			}

			switch (activePopup)
			{
				case PopupKind.DeleteConfirmation:
					DrawDeleteProjectConfirmationPopup();
					break;
				case PopupKind.NamePopup_RenameProject:
					DrawNamePopup();
					break;
				case PopupKind.NamePopup_DuplicateProject:
					DrawNamePopup();
					break;
				case PopupKind.NamePopup_NewProject:
					DrawNamePopup();
					break;
			}
		}

		public static void OnMenuOpened()
		{
			activeMenuScreen = MenuScreen.Main;
			activePopup = PopupKind.None;
			selectedProjectIndex = -1;
		}

		static void DrawMainScreen()
		{
			if (activePopup != PopupKind.None) return;

			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			float buttonWidth = 15;

			int buttonIndex = UI.VerticalButtonGroup(menuButtonNames, theme.MainMenuButtonTheme, UI.Centre + Vector2.up * 6, new Vector2(buttonWidth, 0), false, true, 1);

			if (buttonIndex == 0 || KeyboardShortcuts.MainMenu_NewProjectShortcutTriggered) // New project
			{
				RefreshLoadedProjects();
				activePopup = PopupKind.NamePopup_NewProject;
			}
			else if (buttonIndex == 1 || KeyboardShortcuts.MainMenu_OpenProjectShortcutTriggered) // Load project
			{
				RefreshLoadedProjects();
				selectedProjectIndex = -1;
				activeMenuScreen = MenuScreen.LoadProject;
			}
			else if (buttonIndex == 2 || KeyboardShortcuts.MainMenu_SettingsShortcutTriggered) // Settings
			{
				EditedAppSettings = Main.ActiveAppSettings;
				activeMenuScreen = MenuScreen.Settings;
				OnSettingsMenuOpened();
			}
			else if (buttonIndex == 3) // About
			{
				activeMenuScreen = MenuScreen.About;
			}
			else if (buttonIndex == 4 || KeyboardShortcuts.MainMenu_QuitShortcutTriggered) // Quit
			{
				Quit();
			}
		}

		static void DrawLoadProjectScreen()
		{
			const int backButtonIndex = 0;
			const int deleteButtonIndex = 1;
			const int duplicateButtonIndex = 2;
			const int renameButtonIndex = 3;
			const int openButtonIndex = 4;
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			Vector2 pos = UI.Centre + new Vector2(0, -1);
			Vector2 size = new(68, 32);

			UI.DrawScrollView(ID_ProjectsScrollView, pos, size, Anchor.Centre, theme.ScrollTheme, loadProjectScrollViewDrawer);
			ButtonTheme buttonTheme = DrawSettings.ActiveUITheme.MainMenuButtonTheme;

			bool projectSelected = selectedProjectIndex >= 0 && selectedProjectIndex < allProjectDescriptions.Length;
			bool compatibleProject = projectSelected && projectCompatibilities[selectedProjectIndex].compatible;

			for (int i = 0; i < openProjectButtonStates.Length; i++)
			{
				bool buttonEnabled = activePopup == PopupKind.None && (compatibleProject || i == backButtonIndex || (i == deleteButtonIndex && projectSelected));
				openProjectButtonStates[i] = buttonEnabled;
			}

			Vector2 buttonRegionPos = UI.PrevBounds.BottomLeft + Vector2.down * DrawSettings.VerticalButtonSpacing;
			int buttonIndex = UI.HorizontalButtonGroup(openProjectButtonNames, openProjectButtonStates, buttonTheme, buttonRegionPos, UI.PrevBounds.Width, UILayoutHelper.DefaultSpacing, 0, Anchor.TopLeft);

			if (projectSelected && !compatibleProject)
			{
				Vector2 errorMessagePos = UI.PrevBounds.BottomLeft + Vector2.down * (DrawSettings.DefaultButtonSpacing * 2);
				UI.DrawText(projectCompatibilities[selectedProjectIndex].message, buttonTheme.font, buttonTheme.fontSize, errorMessagePos, Anchor.TopLeft, Color.yellow);
			}

			// ---- Handle button input ----
			if (buttonIndex == backButtonIndex) BackToMain();
			else if (buttonIndex == deleteButtonIndex) activePopup = PopupKind.DeleteConfirmation;
			else if (buttonIndex == duplicateButtonIndex) activePopup = PopupKind.NamePopup_DuplicateProject;
			else if (buttonIndex == renameButtonIndex) activePopup = PopupKind.NamePopup_RenameProject;
			else if (buttonIndex == openButtonIndex) Main.CreateOrLoadProject(SelectedProjectName, string.Empty);
		}

		static bool ProjectNameValidator(string inputString) => inputString.Length <= 20 && !SaveUtils.NameContainsForbiddenChar(inputString);

		static void DrawAllProjectsInScrollView(Vector2 topLeft, float width, bool isLayoutPass)
		{
			float spacing = 0;
			bool enabled = activePopup == PopupKind.None;

			for (int i = 0; i < allProjectDescriptions.Length; i++)
			{
				ProjectDescription desc = allProjectDescriptions[i];
				bool selected = i == selectedProjectIndex;
				ButtonTheme buttonTheme = selected ? DrawSettings.ActiveUITheme.ProjectSelectionButtonSelected : DrawSettings.ActiveUITheme.ProjectSelectionButton;
				if (!projectCompatibilities[i].compatible) buttonTheme.textCols.normal.a = 0.5f;

				if (UI.Button(desc.ProjectName, buttonTheme, topLeft, new Vector2(width, 0), enabled, false, true, Anchor.TopLeft))
				{
					selectedProjectIndex = i;
				}

				topLeft = UI.PrevBounds.BottomLeft + Vector2.down * spacing;
			}
		}


		static void RefreshLoadedProjects()
		{
			allProjectDescriptions = Loader.LoadAllProjectDescriptions();
			allProjectNames = allProjectDescriptions.Select(d => d.ProjectName).ToArray();
			projectCompatibilities = allProjectDescriptions.Select(d => CanOpenProject(d)).ToArray();
		}

		static (bool canOpen, string failureReason) CanOpenProject(ProjectDescription projectDescription)
		{
			try
			{
				Main.Version earliestCompatible = Main.Version.Parse(projectDescription.DLSVersion_EarliestCompatible);
				Main.Version currentVersion = Main.DLSVersion;

				// In case project was made with a newer version of the sim, check if this version is able to open it
				bool canOpen = currentVersion.ToInt() >= earliestCompatible.ToInt();
				string failureReason = canOpen ? string.Empty : $"This project requires version {earliestCompatible} or later.";
				return (canOpen, failureReason);
			}
			catch
			{
				Debug.Log("Incompatible project: " + projectDescription.ProjectName);
				return (false, "Unrecognized project format");
			}
		}

		static void BackToMain()
		{
			UI.GetInputFieldState(ID_ProjectNameInput).ClearText();
			activeMenuScreen = MenuScreen.Main;
			activePopup = PopupKind.None;
		}


		static void OnSettingsMenuOpened()
		{
			// Automatically select whichever resolution option is closest to current window size
			WheelSelectorState resolutionWheelState = UI.GetWheelSelectorState(ID_DisplayResolutionWheel);
			int closestMatchError = int.MaxValue;
			for (int i = 0; i < Resolutions.Length; i++)
			{
				int matchError = Mathf.Min(Mathf.Abs(Screen.width - Resolutions[i].x), Mathf.Abs(Screen.height - Resolutions[i].y));
				if (matchError < closestMatchError)
				{
					closestMatchError = matchError;
					resolutionWheelState.index = i;
				}
			}

			// Automatically set curr fullscreen mode
			WheelSelectorState fullscreenWheelState = UI.GetWheelSelectorState(ID_FullscreenWheel);
			for (int i = 0; i < FullScreenModes.Length; i++)
			{
				if (Screen.fullScreenMode == FullScreenModes[i])
				{
					fullscreenWheelState.index = i;
					break;
				}
			}
		}

		static void DrawSettingsScreen()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			float regionWidth = 30;
			float labelOriginLeft = UI.Centre.x - regionWidth / 2;
			float elementOriginRight = UI.Centre.x + regionWidth / 2;
			Vector2 wheelSize = new(16, 2.5f);
			Vector2 pos = new(labelOriginLeft, UI.Centre.y + 4);
			using (UI.BeginBoundsScope(true))
			{
				Draw.ID backgroundPanelID = UI.ReservePanel();

				// -- Resolution --
				bool resEnabled = EditedAppSettings.fullscreenMode == FullScreenMode.Windowed;
				UI.DrawText("Resolution", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				string[] resNames = resEnabled ? ResolutionNames : FullScreenResName;
				int resIndex = UI.WheelSelector(ID_DisplayResolutionWheel, resNames, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight, enabled: resEnabled);
				EditedAppSettings.ResolutionX = Resolutions[resIndex].x;
				EditedAppSettings.ResolutionY = Resolutions[resIndex].y;

				// -- Full screen --
				pos += Vector2.down * 4;
				UI.DrawText("Fullscreen", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				int fullScreenSettingIndex = UI.WheelSelector(ID_FullscreenWheel, SettingsWheelFullScreenOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight);
				EditedAppSettings.fullscreenMode = FullScreenModes[fullScreenSettingIndex];
				pos += Vector2.down * 4;

				// -- Vsync --
				UI.DrawText("VSync", theme.FontRegular, theme.FontSizeRegular, pos, Anchor.CentreLeft, Color.white);
				int vsyncSetting = UI.WheelSelector(EditedAppSettings.VSyncEnabled ? 1 : 0, SettingsWheelVSyncOptions, new Vector2(elementOriginRight, pos.y), wheelSize, theme.OptionsWheel, Anchor.CentreRight);
				EditedAppSettings.VSyncEnabled = vsyncSetting == 1;

				// Background panel
				UI.ModifyPanel(backgroundPanelID, UI.GetCurrentBoundsScope().Centre, UI.GetCurrentBoundsScope().Size + Vector2.one * 3, ColHelper.MakeCol255(37, 37, 43));
			}

			Vector2 buttonPos = UI.PrevBounds.BottomLeft + Vector2.down * DrawSettings.VerticalButtonSpacing;
			settingsButtonGroupStates[0] = true;
			settingsButtonGroupStates[1] = true;

			int buttonIndex = UI.HorizontalButtonGroup(settingsButtonGroupNames, settingsButtonGroupStates, theme.MainMenuButtonTheme, buttonPos, UI.PrevBounds.Width, UILayoutHelper.DefaultSpacing, 0, Anchor.TopLeft);

			if (buttonIndex == 0)
			{
				BackToMain();
			}
			else if (buttonIndex == 1)
			{
				Main.SaveAndApplyAppSettings(EditedAppSettings);
			}
		}

		static void DrawNamePopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			UI.StartNewLayer();
			UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			using (UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = UI.ReservePanel();

				InputFieldTheme inputTheme = theme.ChipNameInputField;

				Vector2 charSize = UI.CalculateTextSize("M", inputTheme.fontSize, inputTheme.font);
				Vector2 padding = new(2, 2);
				Vector2 inputFieldSize = new Vector2(charSize.x * MaxProjectNameLength, charSize.y) + padding * 2;


				InputFieldState state = UI.InputField(ID_ProjectNameInput, inputTheme, UI.Centre, inputFieldSize, "", Anchor.Centre, padding.x, projectNameValidator, true);

				string projectName = state.text;
				bool validProjectName = !string.IsNullOrWhiteSpace(projectName) && SaveUtils.ValidFileName(projectName);
				bool projectNameAlreadyExists = false;
				foreach (string existingProjectName in allProjectNames)
				{
					projectNameAlreadyExists |= string.Equals(projectName, existingProjectName, StringComparison.CurrentCultureIgnoreCase);
				}

				bool canCreateProject = validProjectName && !projectNameAlreadyExists;

				Vector2 buttonsRegionSize = new(inputFieldSize.x, 5);
				Vector2 buttonsRegionCentre = UILayoutHelper.CalculateCentre(UI.PrevBounds.BottomLeft, buttonsRegionSize, Anchor.TopLeft);
				(Vector2 size, Vector2 centre) layoutCancel = UILayoutHelper.HorizontalLayout(2, 0, buttonsRegionCentre, buttonsRegionSize);
				(Vector2 size, Vector2 centre) layoutConfirm = UILayoutHelper.HorizontalLayout(2, 1, buttonsRegionCentre, buttonsRegionSize);

				bool cancelButton = UI.Button("CANCEL", theme.MainMenuButtonTheme, layoutCancel.centre, new Vector2(layoutCancel.size.x, 0), true, false, true);
				bool confirmButton = UI.Button("CONFIRM", theme.MainMenuButtonTheme, layoutConfirm.centre, new Vector2(layoutConfirm.size.x, 0), canCreateProject, false, true);

				if (cancelButton || KeyboardShortcuts.CancelShortcutTriggered)
				{
					state.ClearText();
					activePopup = PopupKind.None;
				}

				if (confirmButton || KeyboardShortcuts.ConfirmShortcutTriggered)
				{
					state.ClearText();
					PopupKind kind = activePopup;
					activePopup = PopupKind.None;
					OnNamePopupConfirmed(kind, projectName);
				}

				UI.ModifyPanel(panelID, UI.GetCurrentBoundsScope().Centre, UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));
			}
		}

		static void OnNamePopupConfirmed(PopupKind kind, string name)
		{
			if (kind is PopupKind.NamePopup_RenameProject or PopupKind.NamePopup_DuplicateProject)
			{
				if (kind is PopupKind.NamePopup_RenameProject) Saver.RenameProject(SelectedProjectName, name);
				if (kind is PopupKind.NamePopup_DuplicateProject) Saver.DuplicateProject(SelectedProjectName, name);

				RefreshLoadedProjects();
				selectedProjectIndex = 0; // the modified project will now be at top of list
				UI.GetScrollbarState(ID_ProjectsScrollView).scrollY = 0; // scroll to top so selection is visible
			}
			else if (kind is PopupKind.NamePopup_NewProject)
			{
				Main.CreateOrLoadProject(name);
			}
		}

		static void DrawDeleteProjectConfirmationPopup()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;

			UI.StartNewLayer();
			UI.DrawFullscreenPanel(theme.MenuBackgroundOverlayCol);

			using (UI.BeginBoundsScope(true))
			{
				Draw.ID panelID = UI.ReservePanel();
				UI.DrawText("Are you sure you want to delete this project?", theme.FontRegular, theme.FontSizeRegular, UI.Centre, Anchor.Centre, Color.yellow);

				Vector2 buttonRegionTopLeft = UI.PrevBounds.BottomLeft + Vector2.down * DrawSettings.VerticalButtonSpacing;
				float buttonRegionWidth = UI.PrevBounds.Width;
				int buttonIndex = UI.HorizontalButtonGroup(new[] { "CANCEL", "DELETE" }, theme.MainMenuButtonTheme, buttonRegionTopLeft, buttonRegionWidth, DrawSettings.HorizontalButtonSpacing, 0, Anchor.TopLeft);
				UI.ModifyPanel(panelID, UI.GetCurrentBoundsScope().Centre, UI.GetCurrentBoundsScope().Size + Vector2.one * 2, ColHelper.MakeCol255(37, 37, 43));

				if (buttonIndex == 0) // Cancel
				{
					activePopup = PopupKind.None;
				}
				else if (buttonIndex == 1) // Delete
				{
					Saver.DeleteProject(SelectedProjectName);
					selectedProjectIndex = -1;
					RefreshLoadedProjects();
					activePopup = PopupKind.None;
				}
			}
		}

		static void DrawAboutScreen()
		{
			ButtonTheme theme = DrawSettings.ActiveUITheme.MainMenuButtonTheme;

			UI.DrawText("Todo: write something helpful here...", theme.font, theme.fontSize, UI.Centre, Anchor.Centre, Color.white);
			if (UI.Button("Back", theme, UI.CentreBottom + Vector2.up * 22, Vector2.zero, true, true, true))
			{
				BackToMain();
			}
		}

		static void DrawVersionInfo()
		{
			DrawSettings.UIThemeDLS theme = DrawSettings.ActiveUITheme;
			UI.DrawPanel(UI.BottomLeft, new Vector2(UI.Width, 4), ColHelper.MakeCol255(37, 37, 43), Anchor.BottomLeft);

			float pad = 1;
			Color col = new(1, 1, 1, 0.5f);

			Vector2 versionPos = UI.PrevBounds.CentreLeft + Vector2.right * pad;
			Vector2 datePos = UI.PrevBounds.CentreRight + Vector2.left * pad;
			UI.DrawText(authorString, theme.FontRegular, theme.FontSizeRegular, versionPos, Anchor.TextCentreLeft, col);
			UI.DrawText(versionString, theme.FontRegular, theme.FontSizeRegular, datePos, Anchor.TextCentreRight, col);
		}

		static string ResolutionToString(Vector2Int r) => $"{r.x} x {r.y}";

		static void Quit()
		{
			Application.Quit();
		}

		enum MenuScreen
		{
			Main,
			LoadProject,
			Settings,
			About
		}

		enum PopupKind
		{
			None,
			DeleteConfirmation,
			NamePopup_RenameProject,
			NamePopup_DuplicateProject,
			NamePopup_NewProject
		}
	}
}