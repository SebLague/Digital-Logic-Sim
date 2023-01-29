using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DLS.ChipData;
using System.Collections.ObjectModel;
using SebInput;

namespace DLS.ChipCreation
{
	public class ChipEditor : MonoBehaviour
	{
		// ---- Events ----
		public event System.Action<ChipBase> SubChipAdded;
		public event System.Action<ChipBase, int> SubChipDeleted;

		public MouseInteractionGroup<Pin> PinInteractions { get; private set; }
		public bool CanEdit { get; private set; }
		public ChipEditorActions ChipEditorActions { get; private set; }

		// ---- Controllers -----
		public ChipPlacer ChipPlacer { get; private set; }
		public ChipSelector ChipSelector { get; private set; }
		public PinPlacer PinPlacer { get; private set; }
		public WireEditor WireEditor { get; private set; }
		public ChipMover ChipMover { get; private set; }
		public WorkArea WorkArea => workArea;

		// Mouse collision info
		public ChipBase ChipUnderMouse { get; private set; }
		public Pin PinUnderMouse { get; private set; }
		public Wire WireUnderMouse => WireEditor.WireUnderMouse;

		// Misc data
		public ChipDescription LastSavedDescription { get; private set; }
		public string ProjectName => Settings.ProjectName;
		public ReadOnlyCollection<ChipBase> AllSubChips => new(allSubChips);
		public ProjectSettings Settings;
		public Palette ColourThemes => palette;


		[SerializeField] WorkArea workArea;
		[SerializeField] Palette palette;

		List<ChipBase> allSubChips;
		Dictionary<int, ChipBase> subChipByID;
		Dictionary<int, Pin> mainPinByID;
		ControllerBase[] controllers;

		// TODO move to one of the controllers
		bool subChipPinNamesVisible;
		bool mainChipPinNamesVisible;


		public void SetUp(ProjectSettings settings, bool isViewOnly)
		{
			// Init
			ChipEditorActions = new ChipEditorActions();
			ChipEditorActions.Enable();

			CanEdit = !isViewOnly;
			Settings = settings;
			PinInteractions = new();

			controllers = GetComponentsInChildren<ControllerBase>();
			ChipPlacer = controllers.OfType<ChipPlacer>().First();
			PinPlacer = controllers.OfType<PinPlacer>().First();
			WireEditor = controllers.OfType<WireEditor>().First();
			ChipSelector = controllers.OfType<ChipSelector>().First();
			ChipMover = controllers.OfType<ChipMover>().First();

			allSubChips = new List<ChipBase>();
			subChipByID = new Dictionary<int, ChipBase>();
			mainPinByID = new Dictionary<int, Pin>();

			ChipPlacer.FinishedPlacingOrLoadingChip += OnChipPlaced;
			PinPlacer.PinCreated += OnInputOrOutputPinCreated;
			PinPlacer.PinDeleted += OnMainPinDeleted;

			workArea.SetUp();

			// Set up all controllers
			foreach (var controller in controllers)
			{
				controller.SetUp(this);
			}
			ChipEditorActions.ActionMap.TogglePinNameDisplay.performed += OnPinNameDisplayToggle;
		}

		void OnEnable()
		{
			ChipEditorActions?.Enable();
			if (Settings != null)
			{
				UpdatePinDisplaySettings();
			}
		}

		public void LoadChip(ChipDescription chipDescription)
		{
			LastSavedDescription = chipDescription;

			// Load sub chips
			foreach (ChipInstanceData subChipDescription in chipDescription.SubChips)
			{
				ChipPlacer.Load(ChipDescriptionLoader.GetChipDescription(subChipDescription.Name), subChipDescription);
			}

			// Load I/O pins
			foreach (PinDescription pinDescription in chipDescription.InputPins)
			{
				PinPlacer.LoadPin(isInputPin: true, pinDescription);
			}
			foreach (PinDescription pinDescription in chipDescription.OutputPins)
			{
				PinPlacer.LoadPin(isInputPin: false, pinDescription);
			}

			// Load wires
			foreach (ConnectionDescription connection in chipDescription.Connections)
			{
				WireEditor.Load(connection);
			}
		}

		public bool AnyControllerBusy()
		{
			foreach (var controller in controllers)
			{
				if (controller.IsBusy())
				{
					return true;
				}
			}
			return false;
		}



		public ChipBase GetSubChip(int subChipID)
		{
			return subChipByID[subChipID];
		}

		// Get pin on chip or subchip
		public Pin GetPin(PinAddress address)
		{

			if (address.BelongsToSubChip)
			{
				return GetSubChip(address.SubChipID).GetPinByID(address.PinID);
			}
			return mainPinByID[address.PinID];
		}

		public int SubChipIndex(ChipBase chipDisplay)
		{
			return allSubChips.IndexOf(chipDisplay);
		}

		// Get pin from address
		public PinAddress GetPinAddress(Pin pin)
		{
			int subChipID = (pin.BelongsToSubChip) ? pin.Chip.ID : 0;
			return new PinAddress(subChipID, pin.ID, pin.GetPinType());
		}

		public void UpdateLastSavedDescription(ChipDescription description)
		{
			LastSavedDescription = description;
		}

		public bool HasUnsavedChanges()
		{
			var descriptionToSave = ChipDescriptionCreator.CreateChipDescription(this);
			string chipStringNew = ChipSaver.SerializeChipDescription(descriptionToSave, false);
			string chipStringOld = ChipSaver.SerializeChipDescription(LastSavedDescription, false);
			bool hasUnsavedChanges = chipStringNew != chipStringOld;
			return hasUnsavedChanges;
		}

		public void UpdatePinDisplaySettings()
		{
			foreach (ChipBase subchip in AllSubChips)
			{
				foreach (Pin pin in subchip.AllPins)
				{
					pin.SetNameDisplayMode(Settings.DisplayOptions.SubChipPinNameDisplayMode);
				}
			}


			foreach (EditablePin editablePin in PinPlacer.AllPins)
			{
				editablePin.GetPin().SetNameDisplayMode(Settings.DisplayOptions.MainChipPinNameDisplayMode);
			}
		}

		public void DeleteSubchipsByName(string subchipName)
		{
			ChipBase[] chipsToDelete = AllSubChips.Where(s => s.Name == subchipName).ToArray();

			foreach (ChipBase subchip in chipsToDelete)
			{
				subchip.Delete();
			}
		}

		void OnPinNameDisplayToggle(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
		{
			if (Settings.DisplayOptions.MainChipPinNameDisplayMode is DisplayOptions.PinNameDisplayMode.Toggle)
			{
				mainChipPinNamesVisible = !mainChipPinNamesVisible;
				foreach (EditablePin editablePin in PinPlacer.AllPins)
				{
					editablePin.GetPin().SetNameVisibility(mainChipPinNamesVisible);
				}
			}


			if (Settings.DisplayOptions.SubChipPinNameDisplayMode is DisplayOptions.PinNameDisplayMode.Toggle)
			{
				subChipPinNamesVisible = !subChipPinNamesVisible;
				foreach (ChipBase subchip in AllSubChips)
				{
					foreach (Pin pin in subchip.AllPins)
					{
						pin.SetNameVisibility(subChipPinNamesVisible);
					}
				}

			}
		}

		void OnChipPlaced(ChipBase newChip, bool wasLoaded)
		{
			allSubChips.Add(newChip);
			subChipByID.Add(newChip.ID, newChip);

			SubChipAdded?.Invoke(newChip);
			newChip.ChipDeleted += OnChipDeleted;

			if (newChip.MouseInteraction is not null)
			{

				newChip.MouseInteraction.MouseEntered += (chip) => ChipUnderMouse = chip;
				newChip.MouseInteraction.MouseExitted += (chip) => ChipUnderMouse = null;
				if (newChip.MouseInteraction.MouseIsOver)
				{
					ChipUnderMouse = newChip;
				}
			}

			foreach (Pin pin in newChip.AllPins)
			{
				OnPinCreated(pin);
			}
		}


		void OnInputOrOutputPinCreated(EditablePin editablePin)
		{
			Pin pin = editablePin.GetPin();
			mainPinByID.Add(pin.ID, pin);
			OnPinCreated(pin);
		}

		void OnMainPinDeleted(EditablePin editablePin)
		{
			Pin pin = editablePin.GetPin();
			mainPinByID.Remove(pin.ID);
		}

		void OnPinCreated(Pin pin)
		{
			PinInteractions.AddInteractionToGroup(pin.MouseInteraction);
			pin.MouseInteraction.MouseEntered += (pin) => PinUnderMouse = pin;
			pin.MouseInteraction.MouseExitted += (pin) => PinUnderMouse = null;

			if (pin.BelongsToSubChip)
			{
				pin.SetNameDisplayMode(Settings.DisplayOptions.SubChipPinNameDisplayMode);
				if (Settings.DisplayOptions.SubChipPinNameDisplayMode is DisplayOptions.PinNameDisplayMode.Toggle)
				{
					pin.SetNameVisibility(subChipPinNamesVisible);
				}
			}
			else
			{
				pin.SetNameDisplayMode(Settings.DisplayOptions.MainChipPinNameDisplayMode);
				if (Settings.DisplayOptions.MainChipPinNameDisplayMode is DisplayOptions.PinNameDisplayMode.Toggle)
				{
					pin.SetNameVisibility(mainChipPinNamesVisible);
				}
			}
		}

		void OnChipDeleted(ChipBase deletedChip)
		{
			int subChipIndex = SubChipIndex(deletedChip);
			allSubChips.Remove(deletedChip);
			subChipByID.Remove(deletedChip.ID);

			SubChipDeleted?.Invoke(deletedChip, subChipIndex);
			if (ChipUnderMouse == deletedChip)
			{
				ChipUnderMouse = null;
			}
		}

		void OnDisable()
		{
			ChipEditorActions?.Disable();
		}

	}
}
