using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DLS.Description;
using DLS.Graphics;
using DLS.SaveSystem;
using DLS.Simulation;
using Seb.Helpers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DLS.Game
{
	public class Project
	{
		public enum SaveMode
		{
			Normal,
			Rename,
			SaveAs
		}

		public static Project ActiveProject;
		public readonly ChipLibrary chipLibrary;

		public ChipInteractionController controller;
		public ProjectDescription description;

		// ---- Display state ----
		public bool ShowGrid => description.Prefs_GridDisplayMode == 1;
		public bool PinNameDisplayIsTabToggledOn;

		// ---- Chip view / edit state ----
		// At the bottom of the stack is the chip that currently is being edited. 
		// If chips are entered in view mode, they will be placed above on the stack.
		public readonly Stack<DevChipInstance> chipViewStack = new();

		SimChip ViewedSimChip => ViewedChip.SimChip;

		// The chip currently in view. This chip may be in view-only mode.
		public DevChipInstance ViewedChip => chipViewStack.Peek();
		public bool CanEditViewedChip => chipViewStack.Count == 1;
		public string ActiveDevChipName => ViewedChip.ChipName;

		public bool ChipHasBeenSavedBefore => ViewedChip.LastSavedDescription != null;

		// String representation of the viewed chips stack for display purposes
		public string viewedChipsString = string.Empty;

		// The chip currently being edited. (This is not necessarily the currently viewed chip)
		DevChipInstance editModeChip;
		public AudioState audioState;

		// ---- Simulation settings and state ----
		static readonly bool debug_logSimTime = false;
		static readonly bool debug_runSimMainThread = false;
		public const float SimulationPerformanceTimeWindowSec = 1.5f;

		bool simThreadActive;
		public bool advanceSingleSimStep;
		public int simPausedSingleStepCounter;
		int mainThreadFrameCount;
		DevPinInstance[] inputPins = Array.Empty<DevPinInstance>();
		public int targetTicksPerSecond => Mathf.Max(1, description.Prefs_SimTargetStepsPerSecond);
		public int stepsPerClockTransition => description.Prefs_SimStepsPerClockTick;
		public bool simPaused => description.Prefs_SimPaused;
		public double simAvgTicksPerSec { get; private set; }
		public SimChip rootSimChip => editModeChip.SimChip;

		public Project(ProjectDescription description, ChipLibrary chipLibrary)
		{
			ActiveProject = this;
			this.description = description;
			this.chipLibrary = chipLibrary;
			SearchPopup.ClearRecentChips();
		}

		public void Update()
		{
			HandleProjectInput();

			if (UIDrawer.ActiveMenu is UIDrawer.MenuType.None or UIDrawer.MenuType.BottomBarMenuPopup)
			{
				controller.Update();
			}

			if (UIDrawer.ActiveMenu == UIDrawer.MenuType.None)
			{
				Simulator.UpdateKeyboardInputFromMainThread();
			}

			inputPins = editModeChip.GetInputPins();
			mainThreadFrameCount++;

			if (debug_runSimMainThread)
			{
				Debug_RunMainThreadSimStep();
			}
		}

		public void StartSimulation()
		{
			if (debug_runSimMainThread)
			{
				Debug.Log("Simulation will run on main thread");
				return;
			}

			simThreadActive = true;
			Thread simThread = new(SimThread)
			{
				Priority = System.Threading.ThreadPriority.Highest,
				Name = "DLS_SimThread",
				IsBackground = true
			};
			simThread.Start();
		}

		public void EnterViewMode(SubChipInstance subchip)
		{
			if (chipLibrary.TryGetChipDescription(subchip.Description.Name, out ChipDescription description))
			{
				SimChip simChipToView = ViewedChip.SimChip.GetSubChipFromID(subchip.ID);

				DevChipInstance viewChip = DevChipInstance.LoadFromDescriptionTest(description, chipLibrary).devChip;
				viewChip.SetSimChip(simChipToView);

				controller.CancelEverything();
				chipViewStack.Push(viewChip);
				UpdateViewedChipsString();
			}
		}

		public void ReturnToPreviousViewedChip()
		{
			if (chipViewStack.Count > 1)
			{
				chipViewStack.Pop();
				controller.CancelEverything();
				UpdateViewedChipsString();
			}
		}

		public bool AlwaysDrawDevPinNames => AlwaysDrawPinNames(description.Prefs_MainPinNamesDisplayMode);
		public bool AlwaysDrawSubChipPinNames => AlwaysDrawPinNames(description.Prefs_ChipPinNamesDisplayMode);

		bool AlwaysDrawPinNames(int prefIndex) => prefIndex == PreferencesMenu.DisplayMode_Always || (prefIndex == PreferencesMenu.DisplayMode_TabToggle && PinNameDisplayIsTabToggledOn);

		void HandleProjectInput()
		{
			if (UIDrawer.ActiveMenu is UIDrawer.MenuType.None)
			{
				// Step to next simulation frame when paused
				if (simPaused && KeyboardShortcuts.SimNextStepShortcutTriggered)
				{
					advanceSingleSimStep = true;
				}

				if (InputHelper.IsKeyDownThisFrame(KeyCode.Tab))
				{
					PinNameDisplayIsTabToggledOn = !PinNameDisplayIsTabToggledOn;
				}
			}


			PreferencesMenu.HandleKeyboardShortcuts();
		}

		void UpdateViewedChipsString()
		{
			string[] viewedChipNames = chipViewStack.Select(c => c.ChipName).SkipLast(1).Reverse().ToArray();
			viewedChipsString = "Viewing: " + string.Join(" > ", viewedChipNames);
		}


		public void SaveFromDescription(ChipDescription saveChipDescription, SaveMode saveMode = SaveMode.Normal)
		{
			// If this chip hasn't been saved before, it can't have been used anyway so no need to update anything
			// (same thing if saving a new version of it)
			if (ViewedChip.LastSavedDescription != null && saveMode != SaveMode.SaveAs)
			{
				UpdateAndSaveAffectedChips(ViewedChip.LastSavedDescription, saveChipDescription, false);
			}

			if (saveMode is SaveMode.Rename)
			{
				string nameOld = ViewedChip.LastSavedDescription.Name;
				Saver.DeleteChip(nameOld, description.ProjectName, false);
				Saver.SaveChip(saveChipDescription, description.ProjectName);
				chipLibrary.NotifyChipRenamed(saveChipDescription, nameOld);
				RenameStarred(saveChipDescription.Name, nameOld, false, false);
				EnsureChipRenamedInCollections(nameOld, saveChipDescription.Name);
				UpdateAndSaveProjectDescription();
			}
			else
			{
				Saver.SaveChip(saveChipDescription, description.ProjectName);

				chipLibrary.NotifyChipSaved(saveChipDescription);
				bool isNewChip = !ChipHasBeenSavedBefore || saveMode is SaveMode.SaveAs;

				// New chips are automatically starred
				if (isNewChip)
				{
					SetStarred(saveChipDescription.Name, true, false, false);
					UpdateAndSaveProjectDescription();
				}
			}

			// Notify the chip itself that it has been saved
			ViewedChip.NotifySaved(saveChipDescription);
			SearchPopup.AddRecentChip(saveChipDescription.Name);
			CameraController.NotifyChipNameChanged(saveChipDescription.Name);
		}

		public bool ActiveChipHasUnsavedChanges()
		{
			// If chip has no last saved description, then has unsaved changes if any elements have been placed inside it
			if (ViewedChip.LastSavedDescription == null)
			{
				return ViewedChip.Elements.Count > 0;
			}

			return Saver.HasUnsavedChanges(ViewedChip.LastSavedDescription, DescriptionCreator.CreateChipDescription(ViewedChip));
		}

		public void CreateBlankDevChip()
		{
			controller = new ChipInteractionController(this);
			DevChipInstance devChip = new();
			devChip.SetSimChip(new SimChip());
			SetNewActiveDevChip(devChip);
		}

		public void LoadDevChipOrCreateNewIfDoesntExist(string chipName)
		{
			if (chipLibrary.TryGetChipDescription(chipName, out ChipDescription description))
			{
				controller = new ChipInteractionController(this);

				(DevChipInstance devChip, bool anyElementFailedToLoad) = DevChipInstance.LoadFromDescriptionTest(description, chipLibrary);

				// If any element (subchip, wire) failed to load, then save the updated description right away so that we have the correct version
				if (anyElementFailedToLoad)
				{
					ChipDescription descNew = DescriptionCreator.CreateChipDescription(devChip);
					SetNewActiveDevChip(devChip); // needs to be set before saving
					SaveFromDescription(descNew);
				}

				SimChip simChip = Simulator.BuildSimChip(devChip.LastSavedDescription, chipLibrary);
				devChip.SetSimChip(simChip);
				SetNewActiveDevChip(devChip);
			}
			else
			{
				CreateBlankDevChip();
			}
		}

		void SetNewActiveDevChip(DevChipInstance devChip)
		{
			editModeChip = devChip;
			chipViewStack.Clear();
			chipViewStack.Push(devChip);
			viewedChipsString = string.Empty;

			if (devChip.LastSavedDescription != null)
			{
				SearchPopup.AddRecentChip(devChip.LastSavedDescription.Name);
			}
		}

		// Key chip has been bound to a different key, so simulation must be updated
		public void NotifyKeyChipBindingChanged(SubChipInstance keyChip, char newKey)
		{
			SimChip simChip = rootSimChip.GetSubChipFromID(keyChip.ID);
			simChip.InternalState[0] = newKey;
			keyChip.SetKeyChipActivationChar(newKey);
		}

		// Chip's pulse width has been changed, so simulation must be updated
		public void NotifyPulseWidthChanged(SubChipInstance chip, uint widthNew)
		{
			SimChip simChip = rootSimChip.GetSubChipFromID(chip.ID);
			simChip.InternalState[0] = widthNew;
			chip.InternalData[0] = widthNew;
		}

		// Rom has been edited, so simulation must be updated
		public void NotifyRomContentsEdited(SubChipInstance romChip)
		{
			SimChip simChip = rootSimChip.GetSubChipFromID(romChip.ID);
			simChip.UpdateInternalState(romChip.InternalData);
		}

		public void NotifyLEDColourChanged(SubChipInstance ledChip, uint colIndex)
		{
			SimChip simChip = rootSimChip.GetSubChipFromID(ledChip.ID);
			simChip.InternalState[0] = colIndex;
			ledChip.InternalData[0] = colIndex;
		}

		public void DeleteChip(string chipToDeleteName)
		{
			// If the current chip only contains the deleted chip directly as a subchip, it will be removed from the sim and everything is fine.
			// However, if it is contained indirectly somewhere within one of the chip's subchips (or their subchips, etc), then it's a bit tricky (and
			// potentially expensive for large chips) to hunt down all references within the simulation and remove them. So, for now at least, simply
			// restart the simulation in this case (this is not ideal though, since state of latches etc will be lost)
			bool simReloadRequired = ChipContainsSubchipIndirectly(ViewedChip, chipToDeleteName);

			if (ChipContainsSubChipDirectly(ViewedChip, chipToDeleteName))
			{
				// if deleted chip is a subchip of the current chip, clear undo history as it may now be invalid
				// (Todo: maybe handle more gracefully...)
				ViewedChip.UndoController.Clear();
			}


			UpdateAndSaveAffectedChips(chipLibrary.GetChipDescription(chipToDeleteName), null, true);

			// Delete chip save file, remove from library, and update project description
			Saver.DeleteChip(chipToDeleteName, description.ProjectName);
			chipLibrary.RemoveChip(chipToDeleteName);
			SetStarred(chipToDeleteName, false, false, false); // ensure removed from starred list
			EnsureChipRemovedFromCollections(chipToDeleteName);
			UpdateAndSaveProjectDescription();


			// If has deleted the chip that's currently being edited, then open a blank chip
			if (ChipDescription.NameMatch(ViewedChip.ChipName, chipToDeleteName))
			{
				CreateBlankDevChip();
			}
			else
			{
				// Remove any instances of the deleted chip from the active chip
				ViewedChip.DeleteSubchipsByName(chipToDeleteName);
				if (simReloadRequired)
				{
					ViewedChip.RebuildSimulation();
				}
			}
		}

		// Test if chip's subchips (or any of their subchips, etc...) contain the target subchip
		bool ChipContainsSubchipIndirectly(DevChipInstance devChip, string targetSubchip)
		{
			HashSet<string> visited = new(ChipDescription.NameComparer);

			foreach (IMoveable element in devChip.Elements)
			{
				if (element is SubChipInstance subchip && visited.Add(subchip.Description.Name))
				{
					if (ChipContainsSubchipDirectlyOrIndirectly(chipLibrary.GetChipDescription(subchip.Description.Name), targetSubchip))
					{
						return true;
					}
				}
			}

			return false;
		}

		// Test if chip (or any of its subchips, or the subchips' subchips, etc...) contain the target subchip
		bool ChipContainsSubchipDirectlyOrIndirectly(ChipDescription descRoot, string targetSubchip)
		{
			HashSet<string> visited = new(ChipDescription.NameComparer);
			bool found = false;
			SearchRecursive(descRoot);
			return found;

			void SearchRecursive(ChipDescription desc)
			{
				foreach (SubChipDescription sub in desc.SubChips)
				{
					if (found || ChipDescription.NameMatch(sub.Name, targetSubchip))
					{
						found = true;
						return;
					}

					ChipDescription subDescFull = chipLibrary.GetChipDescription(sub.Name);
					if (visited.Add(subDescFull.Name)) // Hasn't visited before
					{
						SearchRecursive(subDescFull);
					}
				}
			}
		}

		bool ChipContainsSubChipDirectly(DevChipInstance chip, string targetName)
		{
			foreach (IMoveable element in chip.Elements)
			{
				if (element is SubChipInstance s && ChipDescription.NameMatch(s.Description.Name, targetName))
				{
					return true;
				}
			}

			return false;
		}

		// Must be called prior to library being updated with the change
		// If deleting, new description can be left null
		void UpdateAndSaveAffectedChips(ChipDescription root_desc, ChipDescription root_descNew, bool willDelete)
		{
			// There a few ways in which chips other than the one currently being edited can be affected, and require resaving:
			// -- A chip is deleted from the library -> all chips that contain the deleted chip must be resaved with that chip and its connections removed
			// -- A chip is renamed -> all chips containing the renamed chip must be resaved with the new name applied
			// -- A chip is saved after removing an input/output pin -> all chips contained the edited chip must be resaved with affected connections removed


			ChipDescription[] affectedChips = chipLibrary.GetDirectParentChips(root_desc.Name);
			bool willRename = !willDelete && !ChipDescription.NameMatch(root_desc.Name, root_descNew.Name);

			HashSet<int> newDesc_AllDevPinIDs = new();
			if (!willDelete)
			{
				foreach (PinDescription p in root_descNew.InputPins) newDesc_AllDevPinIDs.Add(p.ID);
				foreach (PinDescription p in root_descNew.OutputPins) newDesc_AllDevPinIDs.Add(p.ID);
			}

			foreach (ChipDescription desc in affectedChips)
			{
				bool anyChanges = willDelete | willRename;


				(DevChipInstance devChip, bool anyElementFailedToLoad) = DevChipInstance.LoadFromDescriptionTest(desc, chipLibrary);

				anyChanges |= anyElementFailedToLoad;

				if (willDelete)
				{
					devChip.DeleteSubchipsByName(root_desc.Name);
				}
				else
				{
					// Detect deleted dev pins, and remove any connections to the corresponding subchip pins in the affected chip
					foreach (PinDescription p in root_desc.InputPins)
					{
						if (!newDesc_AllDevPinIDs.Contains(p.ID)) anyChanges |= devChip.DeleteWiresAttachedToPinOfSubChip(p.ID);
					}

					foreach (PinDescription p in root_desc.OutputPins)
					{
						if (!newDesc_AllDevPinIDs.Contains(p.ID)) anyChanges |= devChip.DeleteWiresAttachedToPinOfSubChip(p.ID);
					}
				}

				if (anyChanges)
				{
					ChipDescription updatedDesc = DescriptionCreator.CreateChipDescription(devChip);

					if (willRename)
					{
						for (int i = 0; i < updatedDesc.SubChips.Length; i++)
						{
							if (ChipDescription.NameMatch(updatedDesc.SubChips[i].Name, root_desc.Name))
							{
								updatedDesc.SubChips[i].Name = root_descNew.Name;
							}
						}
					}

					Saver.SaveChip(updatedDesc, this.description.ProjectName);
					chipLibrary.NotifyChipSaved(updatedDesc);
				}
			}
		}


		public void ToggleGridDisplay()
		{
			description.Prefs_GridDisplayMode = 1 - description.Prefs_GridDisplayMode;
		}

		public bool ShouldSnapToGrid => KeyboardShortcuts.SnapModeHeld || (description.Prefs_Snapping == 1 && ShowGrid) || description.Prefs_Snapping == 2;
		public bool ForceStraightWires => KeyboardShortcuts.StraightLineModeHeld || (description.Prefs_StraightWires == 1 && ShowGrid) || description.Prefs_StraightWires == 2;

		public void NotifyExit()
		{
			simThreadActive = false;
		}

		void SimThread()
		{
			const int performanceTimeWindowMs = (int)(SimulationPerformanceTimeWindowSec * 1000);
			Queue<long> tickCounterOverTimeWindow = new();
			int simLastMainThreadSyncFrame = -1;

			Stopwatch stopwatch = new();
			Stopwatch stopwatchTotal = Stopwatch.StartNew();

			while (simThreadActive)
			{
				Simulator.ApplyModifications();
				// ---- A new frame has been reached on main thread  ----
				if (mainThreadFrameCount > simLastMainThreadSyncFrame)
				{
					simLastMainThreadSyncFrame = mainThreadFrameCount;
					// Update graphical state from sim
					// Note: update graphical state even when paused so that subchips are automatically if viewed
					ViewedChip.UpdateStateFromSim(ViewedSimChip, !CanEditViewedChip);

					// Log sim time
					if (debug_logSimTime)
					{
						double elapsedMs = stopwatchTotal.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
						int frame = Simulator.simulationFrame;
						if (frame > 0) UnityEngine.Debug.Log($"Avg sim step time: {elapsedMs / frame} ms NumSteps: {frame} secs: {elapsedMs / 1000.0:0.00}");
					}
				}

				// If sim is paused, sleep a bit and then check again
				// Also handle advancing a single step
				if (simPaused && !advanceSingleSimStep)
				{
					Simulator.UpdateInPausedState();
					stopwatchTotal.Stop();
					Thread.Sleep(10);
					continue;
				}

				if (advanceSingleSimStep)
				{
					simPausedSingleStepCounter++;
					advanceSingleSimStep = false;
				}
				else simPausedSingleStepCounter = 0;

				double targetTickDurationMs = 1000.0 / targetTicksPerSecond;
				stopwatch.Restart();
				if (!stopwatchTotal.IsRunning) stopwatchTotal.Start();

				// ---- Run sim ----
				Simulator.stepsPerClockTransition = stepsPerClockTransition;
				SimChip simChip = rootSimChip;
				if (simChip == null) continue; // Could potentially be null for a frame when switching between chips
				Simulator.RunSimulationStep(simChip, inputPins, audioState.simAudio);

				// ---- Wait some amount of time (if needed) to try to hit the target ticks per second ----
				while (true)
				{
					double elapsedMs = stopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
					double waitMs = targetTickDurationMs - elapsedMs;

					if (waitMs <= 0) break;

					// Wait some cycles before checking timer again (todo: better approach?)
					Thread.SpinWait(10);
				}

				// ---- Update perf counter (measures average num ticks over last n seconds) ----
				long elapsedMsTotal = stopwatchTotal.ElapsedMilliseconds;
				tickCounterOverTimeWindow.Enqueue(elapsedMsTotal);
				while (tickCounterOverTimeWindow.Count > 0)
				{
					if (elapsedMsTotal - tickCounterOverTimeWindow.Peek() > performanceTimeWindowMs)
					{
						tickCounterOverTimeWindow.Dequeue();
					}
					else break;
				}

				if (tickCounterOverTimeWindow.Count > 0)
				{
					double activeWindowMs = elapsedMsTotal - tickCounterOverTimeWindow.Peek();
					if (activeWindowMs > 0)
					{
						simAvgTicksPerSec = tickCounterOverTimeWindow.Count / activeWindowMs * 1000;
					}
				}
			}
		}

		void Debug_RunMainThreadSimStep()
		{
			Simulator.stepsPerClockTransition = stepsPerClockTransition;
			Simulator.ApplyModifications();
			Simulator.RunSimulationStep(rootSimChip, inputPins, audioState.simAudio);
			ViewedChip.UpdateStateFromSim(ViewedSimChip, !CanEditViewedChip);
		}

		public void UpdateAndSaveProjectDescription()
		{
			ProjectDescription newDesc = description;
			newDesc.AllCustomChipNames = chipLibrary.GetAllCustomChipNames();
			UpdateAndSaveProjectDescription(newDesc);
		}

		public void UpdateAndSaveProjectDescription(ProjectDescription editedProjectDesc)
		{
			description = editedProjectDesc;
			SaveCurrentProjectDescription();
		}

		public void SaveCurrentProjectDescription()
		{
			Saver.SaveProjectDescription(description);
		}

		public void RenameCollection(int collectionIndex, string nameNew, bool autoSave = true)
		{
			ChipCollection collection = description.ChipCollections[collectionIndex];
			RenameStarred(nameNew, collection.Name, true, false);
			collection.Name = nameNew;
			collection.UpdateDisplayStrings();

			if (autoSave) SaveCurrentProjectDescription();
		}

		// Rename starred chip/collection (if is starred)
		public void RenameStarred(string nameNew, string nameOld, bool isCollection, bool autoSave = true)
		{
			List<StarredItem> starred = description.StarredList;
			bool modified = false;
			for (int i = 0; i < starred.Count; i++)
			{
				if (starred[i].IsCollection == isCollection && ChipDescription.NameMatch(starred[i].Name, nameOld))
				{
					starred[i] = new StarredItem(nameNew, isCollection);
					modified = true;
					break;
				}
			}

			if (autoSave && modified) SaveCurrentProjectDescription();
		}

		public void SetStarred(string chipName, bool star, bool isCollection, bool autoSave = true)
		{
			List<StarredItem> starred = description.StarredList;
			bool alreadyStarred = false;
			bool modified = false;

			for (int i = 0; i < starred.Count; i++)
			{
				if (starred[i].IsCollection == isCollection && ChipDescription.NameMatch(chipName, starred[i].Name))
				{
					if (!star)
					{
						modified = true;
						starred.RemoveAt(i);
					}

					alreadyStarred = true;
					break;
				}
			}

			if (star && !alreadyStarred)
			{
				modified = true;
				starred.Add(new StarredItem(chipName, isCollection));
			}

			if (autoSave && modified) SaveCurrentProjectDescription();
		}

		void EnsureChipRenamedInCollections(string chipNameOld, string chipNameNew)
		{
			foreach (ChipCollection collection in description.ChipCollections)
			{
				for (int i = 0; i < collection.Chips.Count; i++)
				{
					if (ChipDescription.NameMatch(collection.Chips[i], chipNameOld))
					{
						collection.Chips[i] = chipNameNew;
						return;
					}
				}
			}
		}

		void EnsureChipRemovedFromCollections(string chipNameToRemove)
		{
			foreach (ChipCollection collection in description.ChipCollections)
			{
				for (int i = 0; i < collection.Chips.Count; i++)
				{
					if (ChipDescription.NameMatch(collection.Chips[i], chipNameToRemove))
					{
						collection.Chips.RemoveAt(i);
						return;
					}
				}
			}
		}
	}
}