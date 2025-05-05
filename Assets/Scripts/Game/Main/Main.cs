using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLS.Description;
using DLS.Graphics;
using DLS.SaveSystem;
using UnityEngine;

namespace DLS.Game
{
	public static class Main
	{
		public static readonly Version DLSVersion = new(2, 1, 6);
		public static readonly Version DLSVersion_EarliestCompatible = new(2, 0, 0);
		public const string LastUpdatedString = "5 May 2025";
		public static AppSettings ActiveAppSettings;

		public static Project ActiveProject { get; private set; }

		public static Vector2Int FullScreenResolution => new(Display.main.systemWidth, Display.main.systemHeight);
		public static AudioState audioState;

		public static void Init(AudioState audioState)
		{
			SavePaths.EnsureDirectoryExists(SavePaths.ProjectsPath);
			SaveAndApplyAppSettings(Loader.LoadAppSettings());
			Main.audioState = audioState;
		}

		public static void Update()
		{
			if (UIDrawer.ActiveMenu != UIDrawer.MenuType.MainMenu)
			{
				CameraController.Update();
				ActiveProject.Update();

				InteractionState.ClearFrame();
				WorldDrawer.DrawWorld(ActiveProject);
			}

			UIDrawer.Draw();

			HandleGlobalInput();
		}


		public static void SaveAndApplyAppSettings(AppSettings newSettings)
		{
			// Save new settings
			ActiveAppSettings = newSettings;
			Saver.SaveAppSettings(newSettings);
			// Apply settings to app
			int width = newSettings.fullscreenMode is FullScreenMode.Windowed ? newSettings.ResolutionX : FullScreenResolution.x;
			int height = newSettings.fullscreenMode is FullScreenMode.Windowed ? newSettings.ResolutionY : FullScreenResolution.y;
			Screen.SetResolution(width, height, newSettings.fullscreenMode);
			QualitySettings.vSyncCount = newSettings.VSyncEnabled ? 1 : 0;
		}

		public static void LoadMainMenu()
		{
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.MainMenu);
		}

		public static void CreateOrLoadProject(string projectName, string startupChipName = "")
		{
			if (Loader.ProjectExists(projectName)) ActiveProject = LoadProject(projectName);
			else ActiveProject = CreateProject(projectName);

			ActiveProject.LoadDevChipOrCreateNewIfDoesntExist(startupChipName);
			ActiveProject.StartSimulation();
			ActiveProject.audioState = audioState;
			UIDrawer.SetActiveMenu(UIDrawer.MenuType.None);
		}

		static Project CreateProject(string projectName)
		{
			ProjectDescription initialDescription = new()
			{
				ProjectName = projectName,
				DLSVersion_LastSaved = DLSVersion.ToString(),
				DLSVersion_EarliestCompatible = DLSVersion_EarliestCompatible.ToString(),
				CreationTime = DateTime.Now,
				Prefs_ChipPinNamesDisplayMode = PreferencesMenu.DisplayMode_OnHover,
				Prefs_MainPinNamesDisplayMode = PreferencesMenu.DisplayMode_OnHover,
				Prefs_SimTargetStepsPerSecond = 1000,
				Prefs_SimStepsPerClockTick = 250,
				Prefs_SimPaused = false,
				AllCustomChipNames = Array.Empty<string>(),
				StarredList = BuiltinCollectionCreator.GetDefaultStarredList().ToList(),
				ChipCollections = new List<ChipCollection>(BuiltinCollectionCreator.CreateDefaultChipCollections())
			};

			Saver.SaveProjectDescription(initialDescription);
			return LoadProject(projectName);
		}

		public static void OpenSaveDataFolderInFileBrowser()
		{
			try
			{
				string path = SavePaths.AllData;

				if (!Directory.Exists(path)) throw new Exception("Path does not not exist: " + path);

				path = path.Replace("\\", "/");
				string url = "file://" + (path.StartsWith("/") ? path : "/" + path);
				Application.OpenURL(url);
			}
			catch (Exception e)
			{
				Debug.LogError("Error opening folder: " + e.Message);
			}
		}

		static Project LoadProject(string projectName) => Loader.LoadProject(projectName);

		static void HandleGlobalInput()
		{
			if (KeyboardShortcuts.OpenSaveDataFolderShortcutTriggered) OpenSaveDataFolderInFileBrowser();
		}

		public class Version
		{
			public readonly int Major;
			public readonly int Minor;
			public readonly int Patch;

			public Version(int major, int minor, int patch)
			{
				Major = major;
				Minor = minor;
				Patch = patch;
			}

			public int ToInt() => Major * 100000 + Minor * 1000 + Patch;

			public static Version Parse(string versionString)
			{
				string[] versionParts = versionString.Split('.');
				int major = int.Parse(versionParts[0]);
				int minor = int.Parse(versionParts[1]);
				int patch = int.Parse(versionParts[2]);
				return new Version(major, minor, patch);
			}

			public static bool TryParse(string versionString, out Version version)
			{
				try
				{
					version = Parse(versionString);
					return true;
				}
				catch
				{
					version = null;
					return false;
				}
			}

			public override string ToString() => $"{Major}.{Minor}.{Patch}";
		}
	}
}