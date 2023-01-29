using UnityEngine;
using DLS.ChipData;
using SebInput;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation
{
	[SelectionBase]
	public class Pin : MonoBehaviour
	{
		public enum HighlightState { None, Highlighted, HighlightedInvalid }


		public event System.Action<Pin> PinDeleted;
		public event System.Action<Pin> PinMoved;
		public event System.Action<Palette.VoltageColour> ColourThemeChanged;

		public string PinName { get; private set; }
		public PinType GetPinType() => pinType;
		public bool IsInputType => pinType is PinType.ChipInputPin or PinType.SubChipInputPin;
		public bool BelongsToSubChip => pinType is PinType.SubChipInputPin or PinType.SubChipOutputPin;
		public bool IsSourcePin => pinType is PinType.ChipInputPin or PinType.SubChipOutputPin;
		public bool IsTargetPin => !IsSourcePin;
		public bool IsHighlighted => activeHighlightState != HighlightState.None;
		public bool IsBusPin { get; set; }
		public DLS.Simulation.PinState State;
		public Palette.VoltageColour ColourTheme { get; private set; }


		// The chip that this pin belongs to. If the pin belongs to the chip currently being edited then this will be null.
		public ChipBase Chip { get; private set; }
		public MouseInteraction<Pin> MouseInteraction { get; private set; }
		[SerializeField] float interactionRadius;
		[SerializeField] MeshRenderer display;
		[SerializeField] Color defaultCol;
		[SerializeField] Color highlightedCol;
		[SerializeField] Color highlightedInvalidCol;
		[SerializeField] PinNameDisplay nameDisplay;
		public int ID;

		PinType pinType;
		HighlightState activeHighlightState;
		DisplayOptions.PinNameDisplayMode pinNameDisplayMode;

		public PinDebugInfo debugInfo;

		void Awake()
		{
			SetDisplaySize(DisplaySettings.PinSize);
		}

		public void SetUp(ChipBase chip, PinDescription description, PinType pinType, Palette.VoltageColour colourTheme)
		{
			ID = description.ID;
			GetComponent<CircleCollider2D>().radius = interactionRadius;
			display.material.color = defaultCol;
			SetColourTheme(colourTheme, isSetUp: true);

			MouseInteraction = new MouseInteraction<Pin>(gameObject, this);

			this.pinType = pinType;
			Chip = chip;

			gameObject.name = $"Pin ({description.Name})";
			nameDisplay.SetUp(IsSourcePin);

			SetName(description.Name);
			pinNameDisplayMode = DisplayOptions.PinNameDisplayMode.Never;
			UpdateNameDisplayVisibility();
		}

		public void SetHighlightState(HighlightState state)
		{
			activeHighlightState = state;
			Color col = state switch
			{
				HighlightState.None => defaultCol,
				HighlightState.Highlighted => highlightedCol,
				HighlightState.HighlightedInvalid => highlightedInvalidCol,
				_ => Color.black
			};

			display.sharedMaterial.color = col;
			display.transform.localScale = Vector3.one * (state == HighlightState.None ? 1 : interactionRadius * 2);

			UpdateNameDisplayVisibility();
		}

		public void NotifyOfDeletion()
		{
			PinDeleted?.Invoke(this);
		}

		public void NotifyMoved()
		{
			PinMoved?.Invoke(this);
		}

		public void SetName(string name)
		{
			PinName = name;
			nameDisplay.SetText(name);
			UpdateNameDisplayVisibility();
		}

		public void SetNameVisibility(bool vis)
		{
			nameDisplay.SetNameVisibility(vis);
		}

		public void UpdateNameDisplayVisibility()
		{
			SetNameVisibility(ShouldShowPinName());

			bool ShouldShowPinName()
			{
				switch (pinNameDisplayMode)
				{
					case DisplayOptions.PinNameDisplayMode.Always: return true;
					case DisplayOptions.PinNameDisplayMode.Never: return false;
					case DisplayOptions.PinNameDisplayMode.Toggle: return nameDisplay.GetVisibility();
					case DisplayOptions.PinNameDisplayMode.Hover: return IsHighlighted;
					default: return false;
				}
			}
		}

		public void SetNameDisplayMode(DisplayOptions.PinNameDisplayMode mode)
		{
			this.pinNameDisplayMode = mode;
			UpdateNameDisplayVisibility();
		}


		public void SetColourTheme(Palette.VoltageColour colours, bool isSetUp = false)
		{
			ColourTheme = colours;
			if (!isSetUp)
			{
				ColourThemeChanged?.Invoke(colours);
			}
		}

		void SetDisplaySize(float displaySize)
		{
			Transform parent = transform.parent;
			transform.parent = null;
			transform.localScale = Vector3.one * DisplaySettings.PinSize;
			transform.SetParent(parent);
		}

		public void UpdateDebugInfo(DLS.Simulation.SimPin simPin)
		{
			debugInfo.numInputs = simPin.numInputs;
			debugInfo.cycleFlag = simPin.cycleFlag;
			debugInfo.isFloating = simPin.isFloating;
			debugInfo.id = simPin.ID;
		}

		[System.Serializable]
		public struct PinDebugInfo
		{
			public int numInputs;
			public bool isFloating;
			public bool cycleFlag;
			public int id;
		}
	}
}