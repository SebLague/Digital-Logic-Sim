using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SebUI;
using System.Linq;

namespace DLS.MainMenu
{
	public class SettingsMenuController : MonoBehaviour
	{
		// Triggered when settings are first loaded at start of game, as well as whenever a setting is changed from the menu
		public event System.Action<GameSettings> SettingsUpdated;

		[Header("References")]
		[SerializeField] Menu settingsMenu;
		[SerializeField] ValueWheel resolutionWheel;
		[SerializeField] RectTransform resolutionDisable;
		[SerializeField] Toggle fullscreenToggle;
		[SerializeField] Toggle vsyncToggle;
		[SerializeField] Button applyButton;

		GameSettings currentSettings;
		GameSettings lastSavedSettings;

		int[] resolutionOptions;

		void Awake()
		{
			settingsMenu.MenuOpened += RefreshSettings;
			settingsMenu.MenuClosed += OnSettingsMenuClosed;
		}

		void Start()
		{
			RefreshSettings();
			OnSettingsUpdated();
			AddListeners();
			//ApplySettings(Settings.LoadSavedSettings());
			//SetUpScreen();
		}

		// Reloads settings from disc and updates the UI to reflect those settings
		void RefreshSettings()
		{
			currentSettings = new GameSettings();
			currentSettings.LoadSavedSettings();
			lastSavedSettings = currentSettings;
			UpdateUIFromSettings();
		}

		void AddListeners()
		{
			applyButton.onClick.AddListener(OnApplyButtonPressed);

			fullscreenToggle.onValueChanged.AddListener((value) => { currentSettings.IsFullscreen = value; OnSettingsUpdated(); });
			vsyncToggle.onValueChanged.AddListener((value) => { currentSettings.VSyncEnabled = value; OnSettingsUpdated(); });
			resolutionWheel.onValueChanged += OnResolutionSettingChanged;

		}

		// Set UI state from loaded settings
		void UpdateUIFromSettings()
		{
			// Graphics
			fullscreenToggle.SetIsOnWithoutNotify(currentSettings.IsFullscreen);
			vsyncToggle.SetIsOnWithoutNotify(currentSettings.VSyncEnabled);
			resolutionDisable.gameObject.SetActive(currentSettings.IsFullscreen);
			SetResolutionOptions();
		}

		void OnResolutionSettingChanged(int index)
		{
			int height = resolutionOptions[index];
			int width = (int)(height * 16 / 9f);
			currentSettings.ScreenSize = new Vector2Int(width, height);
			OnSettingsUpdated();
		}

		void SetResolutionOptions()
		{
			int[] defaultHeights = new int[] { 480, 720, 1080, 1440, 2160 };

			List<int> heights = new List<int>(defaultHeights.Where(h => h <= MonitorResolution.y));

			if (!heights.Contains(currentSettings.ScreenSize.y))
			{
				heights.Add(currentSettings.ScreenSize.y);
				heights.Sort();
			}

			int heightIndex = heights.IndexOf(currentSettings.ScreenSize.y);
			if (heightIndex < 0)
			{
				heightIndex = 0;
				Debug.Log("Huh?");
			}
			string[] resNames = heights.Select(r => GetName(r)).ToArray();
			resolutionOptions = heights.ToArray();
			resolutionWheel.SetPossibleValues(resNames, heightIndex);

			string GetName(int height)
			{
				int width = (int)(height * 16 / 9f);
				return $"{width} × {height}";
			}
		}

		// Apply current settings to the game
		void ApplyCurrentSettings()
		{
			QualitySettings.vSyncCount = currentSettings.VSyncEnabled ? 1 : 0;
			var mode = (currentSettings.IsFullscreen) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
			if (currentSettings.IsFullscreen)
			{
				currentSettings.ScreenSize = MonitorResolution;
			}
			Screen.SetResolution(currentSettings.ScreenSize.x, currentSettings.ScreenSize.y, mode);
		}

		void SaveCurrentSettings()
		{
			currentSettings.Save();
			lastSavedSettings = currentSettings;
		}

		void OnApplyButtonPressed()
		{
			ApplyCurrentSettings();
			SaveCurrentSettings();
			UpdateUIFromSettings();
		}


		void OnSettingsMenuClosed()
		{
			if (!currentSettings.Equals(lastSavedSettings))
			{
				currentSettings = lastSavedSettings;
				OnSettingsUpdated();
			}
		}

		void OnSettingsUpdated()
		{
			SettingsUpdated?.Invoke(currentSettings);
		}

		Vector2Int MonitorResolution => new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
	}
}
