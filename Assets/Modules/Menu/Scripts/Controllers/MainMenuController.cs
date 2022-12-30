using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DLS.ChipCreation;

namespace DLS.MainMenu
{
	public class MainMenuController : MonoBehaviour
	{

		[Header("References")]
		[SerializeField] GameObject mainMenuHolder;
		[SerializeField] MenuData[] menus;
		[SerializeField] Button quitButton;
		[SerializeField] TMPro.TMP_Text versionText;

		[SerializeField, HideInInspector] int startupMenuDisplayIndex;
		[SerializeField, HideInInspector] int activeMenuDisplayIndex;
		[HideInInspector] public string SceneToLoadOnPlay;

		void Start()
		{
			// Register button listeners
			quitButton?.onClick.AddListener(Quit);
			for (int i = 0; i < menus.Length; i++)
			{
				int menuIndex = i;
				menus[i].OpenButton?.onClick.AddListener(() => ActiveMenuIndex = menuIndex);
				menus[i].BackButton?.onClick.AddListener(OpenStartupMenu);
			}

			versionText.text = $"Version: {Application.version} (alpha)";
			OpenStartupMenu();
		}

		public void Play()
		{
			int buildIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(SceneToLoadOnPlay);
			if (buildIndex == -1)
			{
				Debug.Log("Invalid scene, make sure it's set in the build settings. Path=" + SceneToLoadOnPlay);
			}
			else
			{
				UnityEngine.SceneManagement.SceneManager.LoadScene(SceneToLoadOnPlay);
			}
		}

		void Quit()
		{
			Application.Quit();
		}

		void OpenStartupMenu()
		{
			ActiveMenuIndex = StartUpMenuIndex;
		}

		// Determines which menu is currently displayed
		public int ActiveMenuIndex
		{
			get => activeMenuDisplayIndex;
			set
			{
				activeMenuDisplayIndex = value;
				for (int i = 0; i < menus.Length; i++)
				{
					menus[i].Menu.SetIsOpen(i == activeMenuDisplayIndex);
				}
			}
		}

		public int StartUpMenuIndex
		{
			get => startupMenuDisplayIndex;
			set => startupMenuDisplayIndex = value;
		}


		public string[] GetAllMenuNames()
		{
			string[] names = new string[menus.Length];
			for (int i = 0; i < menus.Length; i++)
			{
				string name = menus[i].Name;
				names[i] = string.IsNullOrWhiteSpace(name) ? "Untitled" : name;
			}
			return names;
		}



		[System.Serializable]
		public struct MenuData
		{
			public string Name;
			public Menu Menu;
			public Button OpenButton;
			public Button BackButton;
		}
	}
}