using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.ChipData;
using System.Linq;
using DLS.ChipCreation.UI;

namespace DLS.ChipCreation
{
	public class ProjectManager : MonoBehaviour
	{

		static string startupProjectName;

		public event System.Action<ChipEditor> ViewedChipChanged;
		public event System.Action<ChipEditor> EditedChipChanged;
		public event System.Action CurrentChipSaved;
		public event System.Action SavedChipDeleted;

		// The chip editor that's currently being viewed
		public ChipEditor ActiveViewChipEditor { get; private set; }
		// The chip editor that's currently being used to edit a chip
		public ChipEditor ActiveEditChipEditor { get; private set; }
		public ProjectSettings ProjectSettings { get; private set; }

		[Header("Debug")]
		public bool debug_LoadChipOnStartup;
		public string debug_StartupProjectName;
		public string debug_ChipEditName;

		[Header("References")]
		public BuiltinChipCodeView builtinCodeViewUI;
		public ChipEditor chipEditorPrefab;
		public ChipEditor disableThisOne;
		public StarredChipMenuBar starredChipMenuBar;
		public SimulationController simulationController;
		public Transform viewChainMenu;
		public TMPro.TMP_Text viewChainTextDisplay;
		public UnityEngine.UI.Button viewChainBackButton;
		public GameObject cursorGuide;

		List<View> viewChain;

		void Start()
		{
			QualitySettings.maxQueuedFrames = 1;
			viewChain = new();
			viewChainBackButton.onClick.AddListener(ViewChainBackStep);

			bool skippedMenu = string.IsNullOrEmpty(startupProjectName); // loaded directly into the scene (editor only)

			if (skippedMenu)
			{
				startupProjectName = debug_StartupProjectName;
			}

			ProjectSettings = ProjectSettingsLoader.LoadProjectSettings(startupProjectName);

			// Load all chip descriptions from disk
			ChipDescriptionLoader.LoadChips(ProjectSettings.ProjectName);
			simulationController.Init(ChipDescriptionLoader.AllChips.ToArray());

			if (skippedMenu && debug_LoadChipOnStartup && ChipDescriptionLoader.TryGetChipDescription(debug_ChipEditName, out ChipDescription startChip))
			{
				OpenChipEditor(startChip);
			}
			else
			{
				OpenNewChipEditor();
			}

			starredChipMenuBar.SetUp();
			UpdateDisplayOptions(ProjectSettings.DisplayOptions);
		}

		ChipEditor LoadChipEditor(ChipDescription chipDescription, ProjectSettings settings, bool isViewOnly)
		{
			disableThisOne.gameObject.SetActive(false);

			ChipEditor editor = Instantiate(chipEditorPrefab);
			editor.SetUp(settings, isViewOnly);

			editor.LoadChip(chipDescription);//
			if (editor.HasUnsavedChanges())
			{
				Debug.Log("Unsaved changes!");
			}
			return editor;
		}


		// For debug/dev purposes only
		public void ResaveAll()
		{
			foreach (ChipDescription chip in ChipDescriptionLoader.CustomChips)
			{
				OpenChipEditor(chip);
				ChipDescription descriptionToSave = ChipDescriptionCreator.CreateChipDescription(ActiveEditChipEditor);
				ChipSaver.SaveChip(descriptionToSave, ActiveEditChipEditor.ProjectName);
			}
		}


		public static void SetStartupProject(string projectName)
		{
			startupProjectName = projectName;
		}


		// Opens a new chip editor. Destroys any currently open editor.
		public void OpenNewChipEditor()
		{
			var emptyChipDescription = EmptyChipDescriptionCreator.CreateEmptyChipDescription(name: string.Empty);
			OpenChipEditor(emptyChipDescription);
		}

		// Opens an editor for the given chip. Destroys any currently open editor.
		public void OpenChipEditor(ChipDescription chipDescription)
		{
			CloseAllEditors();
			ActiveEditChipEditor = LoadChipEditor(chipDescription, ProjectSettings, false);
			SetActiveView(ActiveEditChipEditor);
			simulationController.SetEditedChip(ActiveEditChipEditor);
			ViewedChipChanged?.Invoke(ActiveViewChipEditor);
			EditedChipChanged?.Invoke(ActiveEditChipEditor);
		}

		// Open a subchip in view-only mode
		public void OpenSubChipViewer(ChipBase chip)
		{
			viewChain.Add(new View() { chipEditor = ActiveViewChipEditor, subChipID = chip.ID });
			ActiveViewChipEditor.gameObject.SetActive(false);

			ChipDescription viewChipDescription = ChipDescriptionLoader.GetChipDescription(chip.Name);
			SetActiveView(LoadChipEditor(viewChipDescription, ProjectSettings, true));

			UpdateSimulatorView();
			UpdateViewChainUI();

			ViewedChipChanged?.Invoke(ActiveViewChipEditor);
		}

		void UpdateSimulatorView()
		{
			simulationController.SetView(ActiveViewChipEditor, viewChain.Select(v => v.subChipID).ToArray());
		}

		void ViewChainBackStep()
		{
			if (viewChain.Count > 0)
			{
				Destroy(ActiveViewChipEditor.gameObject);
				SetActiveView(viewChain[viewChain.Count - 1].chipEditor);
				ActiveViewChipEditor.gameObject.SetActive(true);
				viewChain.RemoveAt(viewChain.Count - 1);
				UpdateSimulatorView();
				UpdateViewChainUI();
				ViewedChipChanged?.Invoke(ActiveViewChipEditor);
			}
		}

		void UpdateViewChainUI()
		{
			// View chain UI (TODO: move somewhere else)
			if (viewChain.Count > 0)
			{
				viewChainMenu.gameObject.SetActive(true);
				string viewChainText = SetCol("<b>Viewing: </b>", "#f25b50");
				for (int i = 0; i < viewChain.Count; i++)
				{
					viewChainText += viewChain[i].chipEditor.GetSubChip(viewChain[i].subChipID).Name;
					if (i < viewChain.Count - 1)
					{
						viewChainText += SetCol(" > ", "#ffffff77");
					}
				}

				viewChainTextDisplay.text = viewChainText;
			}
			else
			{
				viewChainMenu.gameObject.SetActive(false);
			}

			string SetCol(string s, string colhex)
			{
				return $"<color={colhex}>{s}</color>";
			}
		}

		// Closes the current chip editor, as well as any editors that are being used to view subchips
		void CloseAllEditors()
		{
			if (ActiveViewChipEditor is not null)
			{
				Destroy(ActiveEditChipEditor.gameObject);
			}
			if (viewChain.Count > 0)
			{
				for (int i = 1; i < viewChain.Count; i++)
				{
					Destroy(viewChain[i].chipEditor.gameObject);
				}
				Destroy(ActiveViewChipEditor.gameObject);
				viewChain.Clear();
			}

			ActiveEditChipEditor = null;
			ActiveViewChipEditor = null;
		}

		public void SaveChip(bool isFirstTimeSaving, bool isRenamingExistingChip, ChipDescription descriptionToSave)
		{
			// Create list of chip descriptions that need to be saved. Typically this is just the current chip,
			// but if the current chip is already being used inside of other chips, then their descriptions may need updating as well.
			List<ChipDescription> allDescriptionsToSave = new() { descriptionToSave };

			// Saving a brand new chip
			if (isFirstTimeSaving)
			{
				ChipDescriptionLoader.AddChip(descriptionToSave);
				ProjectSettings.AddNewChip(descriptionToSave.Name, starByDefault: true);
			}
			else
			{
				ChipDescription lastSavedDescription = ActiveEditChipEditor.LastSavedDescription;
				ChipDescription[] parentDescriptions = ChipDescriptionHelper.GetParentChipDescriptions(lastSavedDescription.Name);

				// If any pins have been deleted, then parent chips will need to remove all connections to/from those pins.
				IEnumerable<int> allPinIDsOld = ChipDescriptionHelper.GetAllPinIDs(lastSavedDescription);
				IEnumerable<int> allPinsIDsNew = ChipDescriptionHelper.GetAllPinIDs(descriptionToSave);
				int[] deletedPinIDs = allPinIDsOld.Except(allPinsIDsNew).ToArray();
				if (deletedPinIDs.Length > 0)
				{
					for (int i = 0; i < parentDescriptions.Length; i++)
					{
						ChipDescriptionHelper.RemoveConnectionsToDeletedPins(ref parentDescriptions[i], deletedPinIDs, lastSavedDescription.Name);
					}
				}

				// Renaming an existing chip
				if (isRenamingExistingChip)
				{
					string projectName = ActiveEditChipEditor.ProjectName;

					// Delete the save file with old name and update in loaded chips and project settings
					ChipSaver.DeleteChip(lastSavedDescription.Name, projectName, backupInDeletedFolder: false);
					ChipDescriptionLoader.UpdateChipDescription(descriptionToSave, lastSavedDescription.Name);
					ProjectSettings.UpdateChipName(lastSavedDescription.Name, descriptionToSave.Name);

					// Update all parent chips to reflect the new name.
					for (int i = 0; i < parentDescriptions.Length; i++)
					{
						ChipDescriptionHelper.RenameSubChip(ref parentDescriptions[i], lastSavedDescription.Name, descriptionToSave.Name);
					}
				}
				// Saving over an existing chip with the same name
				else
				{
					ChipDescriptionLoader.UpdateChipDescription(descriptionToSave);
				}

				// If parent chips are affected by the changes to the current chip, then save their updated descriptions
				if (deletedPinIDs.Length > 0 || isRenamingExistingChip)
				{
					allDescriptionsToSave.AddRange(parentDescriptions);
					// Update the previously loaded chip descriptions so they're in agreement with the newly saved versions
					ChipDescriptionLoader.UpdateChipDescriptions(parentDescriptions);
				}
			}

			// Save to disk
			ChipSaver.SaveChips(allDescriptionsToSave.ToArray(), ProjectSettings.ProjectName);

			ActiveEditChipEditor.UpdateLastSavedDescription(descriptionToSave);
			CurrentChipSaved?.Invoke();
			simulationController.UpdateChipsFromDescriptions(allDescriptionsToSave.ToArray());

		}

		// Handle deleting a saved chip from the project
		public void DeleteChip(string chipToDeleteName)
		{
			ChipSaver.DeleteChip(chipToDeleteName, ProjectSettings.ProjectName);
			ProjectSettings.RemoveChip(chipToDeleteName);
			ChipDescriptionLoader.RemoveChip(chipToDeleteName);

			// Remove any instances of the deleted chip from the active chip
			ActiveEditChipEditor.DeleteSubchipsByName(chipToDeleteName);

			// Remove all instances of deleted chip from other saved chips
			ChipDescription[] parentChips = ChipDescriptionHelper.GetParentChipDescriptions(chipToDeleteName);
			if (parentChips.Length > 0)
			{
				for (int i = 0; i < parentChips.Length; i++)
				{
					ChipDescriptionHelper.RemoveSubChip(ref parentChips[i], chipToDeleteName);
				}

				ChipSaver.SaveChips(parentChips, ProjectSettings.ProjectName);
				simulationController.UpdateChipsFromDescriptions(parentChips.ToArray());
			}

			// Update the previously loaded chip descriptions so they're in agreement with the newly saved versions
			ChipDescriptionLoader.UpdateChipDescriptions(parentChips);

			SavedChipDeleted?.Invoke();

			// If has deleted the chip that's currently being edited, then open a blank chip
			if (ActiveEditChipEditor.LastSavedDescription.Name == chipToDeleteName)
			{
				CloseAllEditors();
				OpenNewChipEditor();
			}
		}

		public void UpdateDisplayOptions(DisplayOptions options)
		{
			ProjectSettings.UpdateDisplayOptions(options);
			ActiveViewChipEditor.UpdatePinDisplaySettings();
			cursorGuide.SetActive(options.ShowCursorGuide == DisplayOptions.ToggleState.On);
		}

		public static void QuitToMainMenu()
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}

		void SetActiveView(ChipEditor chipEditor)
		{
			ActiveViewChipEditor = chipEditor;
			ActiveViewChipEditor.gameObject.SetActive(true);
			builtinCodeViewUI.OnViewChanged(chipEditor.LastSavedDescription.Name);
		}

		void OnDestroy()
		{
			startupProjectName = string.Empty;
		}

		public struct View
		{
			public ChipEditor chipEditor;
			public int subChipID;
		}

	}
}