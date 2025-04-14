using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DLS.Description;
using DLS.Graphics;
using DLS.SaveSystem;
using DLS.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;
using ThreadPriority = System.Threading.ThreadPriority;

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

		public const float SimulationPerformanceTimeWindowSec = 1.5f;

		public static Project ActiveProject;
		static readonly bool logSimTime = false;
		public readonly ChipLibrary chipLibrary;

		// At the bottom of the stack is the chip that currently is being edited. 
		// If chips are entered in view mode, they will be placed above on the stack.
		public readonly Stack<DevChipInstance> chipViewStack = new();
		public bool advanceSingleSimStep;

		public ChipInteractionController controller;

		public ProjectDescription description;

		// The chip currently being edited. (This is not necessarily the currently viewed chip)
		DevChipInstance editModeChip;

		DevPinInstance[] inputPins = Array.Empty<DevPinInstance>();
		int mainThreadFrameCount;

		public bool showGrid;
		public int simPausedSingleStepCounter;

		bool simThreadActive;

		// String representation of the viewed chips stack for display purposes
		public string viewedChipsString = string.Empty;
		
		public SimChip rootSimChip => editModeChip.SimChip;
		SimChip ViewedSimChip => ViewedChip.SimChip;

		// The chip currently in view. This chip may be in view-only mode.
		public DevChipInstance ViewedChip => chipViewStack.Peek();

		public bool CanEditViewedChip => chipViewStack.Count == 1;

		public int targetTicksPerSecond => Mathf.Max(1, description.Prefs_SimTargetStepsPerSecond);
		public int stepsPerClockTransition => description.Prefs_SimStepsPerClockTick;
		public bool simPaused => description.Prefs_SimPaused;
		public double simAvgTicksPerSec { get; private set; }

		public string ActiveDevChipName => ViewedChip.ChipName;

		public bool ChipHasBeenSavedBefore => ViewedChip.LastSavedDescription != null;
		
		public Project(ProjectDescription description, ChipLibrary chipLibrary)
		{
			ActiveProject = this;
			this.description = description;
			this.chipLibrary = chipLibrary;
			SearchPopup.ClearRecentChips();
		}


		public void StartSimulation()
		{
			simThreadActive = true;
			Thread simThread = new(SimThread);
			simThread.Priority = ThreadPriority.Highest;
			simThread.Name = "DLS_SimThread";
			simThread.IsBackground = true;
			simThread.Start();
		}

		public void EnterViewMode(SubChipInstance subchip)
		{
			if (chipLibrary.TryGetChipDescription(subchip.Description.Name, out ChipDescription description))
			{
				SimChip simChipToView = ViewedChip.SimChip.GetSubChipFromID(subchip.ID);

				DevChipInstance viewChip = new(description, chipLibrary, simChipToView);
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

		void UpdateViewedChipsString()
		{
			string[] viewedChipNames = chipViewStack.Select(c => c.ChipName).SkipLast(1).Reverse().ToArray();
			viewedChipsString = "Viewing: " + string.Join(" > ", viewedChipNames);
		}


		public void SaveFromDescription(ChipDescription saveChipDescription, SaveMode saveMode = SaveMode.Normal)
		{
			if (saveMode is SaveMode.Rename)
			{
				string nameOld = ViewedChip.LastSavedDescription.Name;
				Saver.DeleteChip(nameOld, description.ProjectName, false);
				Saver.SaveChip(saveChipDescription, description.ProjectName);
				chipLibrary.NotifyChipRenamed(saveChipDescription, nameOld);
				RenameStarred(saveChipDescription.Name, nameOld, false, false);
				EnsureChipRenamedInCollections(nameOld, saveChipDescription.Name);
				UpdateAndSaveProjectDescription();

				// Change name in all affected chips (chips which use the renamed chip)
				ChipDescription[] affectedChips = chipLibrary.GetDirectParentChips(nameOld);
				for (int i = 0; i < affectedChips.Length; i++)
				{
					RenameSubChipInChipDescription(affectedChips[i], nameOld, saveChipDescription.Name);
					Saver.SaveChip(affectedChips[i], description.ProjectName);
				}
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

		static void RenameSubChipInChipDescription(ChipDescription desc, string nameOld, string nameNew)
		{
			for (int i = desc.SubChips.Length - 1; i >= 0; i--)
			{
				if (ChipDescription.NameMatch(desc.SubChips[i].Name, nameOld))
				{
					desc.SubChips[i].Name = nameNew;
				}
			}
		}

		// Remove all instances of a particular subchip (and any connecting wires) from this chip's description
		static void RemoveSubchipFromDescription(ChipDescription desc, string removeName)
		{
			HashSet<int> removedSubchipIDs = new();

			// Get IDs of all subchips to be removed
			for (int i = desc.SubChips.Length - 1; i >= 0; i--)
			{
				if (ChipDescription.NameMatch(desc.SubChips[i].Name, removeName))
				{
					removedSubchipIDs.Add(desc.SubChips[i].ID);
				}
			}

			// Create arrays with the subchips and any connected wires removed
			SubChipDescription[] newSubchips = desc.SubChips.Where(c => !removedSubchipIDs.Contains(c.ID)).ToArray();
			WireDescription[] newWires = desc.Wires.Where(w => !(removedSubchipIDs.Contains(w.SourcePinAddress.PinOwnerID) || removedSubchipIDs.Contains(w.TargetPinAddress.PinOwnerID))).ToArray();

			// Copy the new data into the existing arrays
			Array.Resize(ref desc.SubChips, newSubchips.Length);

			Array.Copy(newSubchips, desc.SubChips, newSubchips.Length);
			Array.Resize(ref desc.Wires, newWires.Length);
			Array.Copy(newWires, desc.Wires, newWires.Length);
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
			SetNewActiveDevChip(new DevChipInstance());
		}

		public void LoadDevChipOrCreateNewIfDoesntExist(string chipName)
		{
			if (chipLibrary.TryGetChipDescription(chipName, out ChipDescription description))
			{
				controller = new ChipInteractionController(this);
				SimChip simChip = Simulator.BuildSimChip(description, chipLibrary);
				SetNewActiveDevChip(new DevChipInstance(description, chipLibrary, simChip));
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
			keyChip.SetKeyChipActivationChar(newKey);
			SimChip simChip = rootSimChip.GetSubChipFromID(keyChip.ID);
			simChip.ChangeKeyBinding(newKey);
		}

		// Rom has been edited, so simulation must be updated
		public void NotifyRomContentsEdited(SubChipInstance romChip)
		{
			SimChip simChip = rootSimChip.GetSubChipFromID(romChip.ID);
			simChip.UpdateInternalState(romChip.InternalData);
		}

		public void DeleteChip(string chipToDeleteName)
		{
			// Delete chip save file, remove from library, and update project description
			Saver.DeleteChip(chipToDeleteName, description.ProjectName);
			chipLibrary.RemoveChip(chipToDeleteName);
			SetStarred(chipToDeleteName, false, false, false); // ensure removed from starred list
			EnsureChipRemovedFromCollections(chipToDeleteName);
			UpdateAndSaveProjectDescription();

			// Remove any instances of the deleted chip from the active chip
			ViewedChip.RemoveSubchipsByName(chipToDeleteName);

			// Remove all instances of deleted chip from saved chips (and resave them)
			ChipDescription[] affectedChips = chipLibrary.GetDirectParentChips(chipToDeleteName);
			foreach (ChipDescription chipDesc in affectedChips)
			{
				RemoveSubchipFromDescription(chipDesc, chipToDeleteName);
				Saver.SaveChip(chipDesc, description.ProjectName);
			}

			// If has deleted the chip that's currently being edited, then open a blank chip
			if (ChipDescription.NameMatch(ViewedChip.ChipName, chipToDeleteName))
			{
				CreateBlankDevChip();
			}
		}

		public void Update()
		{
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
		}

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
					if (logSimTime)
					{
						double elapsedMs = stopwatchTotal.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
						int frame = Simulator.simulationFrame;
						if (frame > 0) Debug.Log($"Avg sim step time: {elapsedMs / frame} ms NumSteps: {frame} secs: {elapsedMs / 1000.0:0.00}");
					}
				}

				// If sim is paused, sleep a bit and then check again
				// Also handle advancing a single step
				if (simPaused && !advanceSingleSimStep)
				{
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
				Simulator.RunSimulationStep(rootSimChip, inputPins);

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