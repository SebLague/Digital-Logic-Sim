using UnityEngine;
using System.Collections.Generic;
using DLS.ChipData;
using System.Linq;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation
{
	// Handles instantiation and placement of new chips
	public class ChipPlacer : ControllerBase
	{
		// ====== Public stuff ======

		// Events triggered when a chip is starting or finishing the placement/loading process.
		// The bool is true if chip was loaded (as opposed to user manually placing it)
		public event System.Action<ChipBase, bool> StartedPlacingOrLoadingChip;
		public event System.Action<ChipBase, bool> FinishedPlacingOrLoadingChip;

		public bool IsPlacingChip => activeChip is not null;
		public override bool IsBusy() => IsPlacingChip;

		// ====== Inspector fields ======
		[SerializeField] ChipOverrides overrides;
		[SerializeField] ChipBase chipPrefab;
		[SerializeField] Transform childChipHolder;

		// ====== Private fields ======
		ChipBase activeChip;
		ChipDescription lastCreatedChipDescription;
		System.Random rng;
		Dictionary<string, ChipBase> chipOverrideLookup;

		public override void SetUp(ChipEditor editor)
		{
			base.SetUp(editor);
			rng = new System.Random();
			chipOverrideLookup = overrides.CreateLookup();
		}

		// Place a chip directly, without requiring any player input
		public void Load(ChipDescription description, ChipInstanceData instanceData)
		{
			ChipBase loadedChip = InstantiateChip(description);
			loadedChip.Load(description, instanceData);
			OnStartedPlacingOrLoadingChip(loadedChip, isLoading: true);
			OnFinishedPlacingOrLoadingChip(loadedChip, wasLoaded: true);
		}

		public void StartPlacingChip(ChipDescription chipDescription)
		{
			StartPlacingChip(chipDescription, GenerateID());
		}

		void Update()
		{
			if (IsPlacingChip)
			{
				bool isPlacingBus = activeChip.Name is BuiltinChipNames.BusName;

				if (isPlacingBus)
				{
					HandleBusPlacementInput();
				}
				else
				{
					HandleChipPlacementInput();
				}

				HandleChipCancellationInput();
			}
		}

		void HandleChipPlacementInput()
		{
			Vector3 chipPos = MouseHelper.GetMouseWorldPosition(RenderOrder.ChipMoving);
			activeChip.transform.position = chipPos;

			// Left click or enter to confirm placement of chip
			if (Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
			{
				if (IsValidPlacement(activeChip))
				{
					FinishPlacingActiveChip();
					if (Keyboard.current.leftShiftKey.isPressed)
					{
						StartPlacingChip(lastCreatedChipDescription);
						activeChip.transform.position = chipPos;
					}
				}
			}
		}



		public bool IsValidPlacement(ChipBase chip)
		{
			Bounds bounds = chip.GetBounds();
			foreach (ChipBase otherChip in chipEditor.AllSubChips)
			{
				if (otherChip != chip && BoundsOverlap2D(otherChip.GetBounds(), bounds))
				{
					return false;
				}
			}
			return !chipEditor.WorkArea.OutOfBounds(bounds);

			bool BoundsOverlap2D(Bounds a, Bounds b)
			{
				bool overlapX = b.min.x < a.max.x && b.max.x > a.min.x;
				bool overlapY = b.min.y < a.max.y && b.max.y > a.min.y;
				return overlapX && overlapY;

			}
		}


		void HandleBusPlacementInput()
		{
			Vector3 mousePos = MouseHelper.GetMouseWorldPosition(RenderOrder.ChipMoving);
			BusDisplay bus = activeChip as BusDisplay;
			BusDisplay.PlacementState placementState = bus.CurrentPlacementState;
			bool shiftKeyDown = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

			// Placing first pin
			if (placementState == BusDisplay.PlacementState.PlacingFirstPin)
			{
				activeChip.transform.position = mousePos;
			}
			// Placing wire
			if (placementState == BusDisplay.PlacementState.PlacingWire)
			{
				bus.UpdateWirePlacementPreview(mousePos);
				// Shif left click to add wire points when placing bus
				if (Mouse.current.leftButton.wasPressedThisFrame && shiftKeyDown)
				{
					bus.AddWirePoint(mousePos);
				}
			}
			// Left click or enter to confirm placement of pin
			if ((Mouse.current.leftButton.wasPressedThisFrame && !shiftKeyDown) || Keyboard.current.enterKey.wasPressedThisFrame)
			{
				if (IsValidPlacement(activeChip))
				{
					bus.PlacePin(mousePos);
					if (placementState == BusDisplay.PlacementState.PlacingWire)
					{
						FinishPlacingActiveChip();
					}
				}
			}
		}

		void HandleChipCancellationInput()
		{
			// Right click or escape key to cancel placement of chip
			if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
			{
				DestroyActiveChip();
			}
		}

		void FinishPlacingActiveChip()
		{
			if (IsPlacingChip)
			{
				activeChip.transform.position = activeChip.transform.position.WithZ(RenderOrder.Chip);
				activeChip.FinishPlacing();
				OnFinishedPlacingOrLoadingChip(activeChip, wasLoaded: false);
				activeChip = null;
			}
		}

		void StartPlacingChip(ChipDescription chipDescription, int id)
		{
			DestroyActiveChip();

			activeChip = InstantiateChip(chipDescription);
			activeChip.ChipDeleted += (chip) => activeChip = null;
			activeChip.StartPlacing(chipDescription, id);
			lastCreatedChipDescription = chipDescription;

			OnStartedPlacingOrLoadingChip(activeChip, isLoading: false);
		}


		ChipBase InstantiateChip(ChipDescription description)
		{
			ChipBase prefab;
			if (!chipOverrideLookup.TryGetValue(description.Name, out prefab))
			{
				prefab = chipPrefab;
			}

			ChipBase chip = Instantiate(prefab, parent: childChipHolder);
			return chip;
		}

		void OnStartedPlacingOrLoadingChip(ChipBase chip, bool isLoading)
		{
			StartedPlacingOrLoadingChip?.Invoke(chip, isLoading);
		}

		void OnFinishedPlacingOrLoadingChip(ChipBase chip, bool wasLoaded)
		{
			FinishedPlacingOrLoadingChip?.Invoke(chip, wasLoaded);
		}


		void DestroyActiveChip()
		{
			if (IsPlacingChip)
			{
				activeChip.Delete();
			}
		}

		int GenerateID()
		{
			int id;
			while (true)
			{
				id = rng.Next();
				// Should be incredibly unlikely to generate same ID twice, but will sleep better knowing I've made sure...
				if (!chipEditor.AllSubChips.Any(subChip => subChip.ID == id))
				{
					break;
				}
			}

			return id;
		}

	}
}