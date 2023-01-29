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

		public bool IsPlacingChip => activeChips.Count > 0;
		public override bool IsBusy() => IsPlacingChip;

		// ====== Inspector fields ======
		[SerializeField] ChipOverrides overrides;
		[SerializeField] ChipBase chipPrefab;
		[SerializeField] Transform childChipHolder;

		// ====== Private fields ======
		List<ChipBase> activeChips;
		ChipDescription lastCreatedChipDescription;
		System.Random rng;
		Dictionary<string, ChipBase> chipOverrideLookup;
		List<Vector2> busPlacementPoints;
		const float multiChipSpacing = 0.1f;

		public override void SetUp(ChipEditor editor)
		{
			base.SetUp(editor);
			rng = new System.Random();
			chipOverrideLookup = overrides.CreateLookup();
			activeChips = new List<ChipBase>();
		}

		public ChipBase[] AllChipsInPlacementMode => activeChips.ToArray();

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

				bool isPlacingBus = activeChips[0].Name is BuiltinChipNames.BusName;

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
			SetActiveChipsPosition(chipPos);

			// Left click or enter to confirm placement of chip
			if (Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
			{
				if (CanPlaceActiveChips())
				{
					FinishPlacingActiveChips();
					if (Keyboard.current.leftShiftKey.isPressed)
					{
						StartPlacingChip(lastCreatedChipDescription);
						SetActiveChipsPosition(chipPos);
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
				if (a.size.x * a.size.y == 0 || b.size.x * b.size.y == 0)
				{
					return false;
				}
				bool overlapX = b.min.x < a.max.x && b.max.x > a.min.x;
				bool overlapY = b.min.y < a.max.y && b.max.y > a.min.y;
				return overlapX && overlapY;

			}
		}

		bool CanPlaceActiveChips()
		{
			foreach (var chip in activeChips)
			{
				if (!IsValidPlacement(chip))
				{
					return false;
				}
			}
			return true;
		}


		void HandleBusPlacementInput()
		{


			BusDisplay[] busChips = activeChips.Select(c => c as BusDisplay).ToArray();
			BusDisplay.PlacementState placementState = busChips[0].CurrentPlacementState;
			bool shiftKeyDown = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

			bool snapping = shiftKeyDown && busPlacementPoints != null && busPlacementPoints.Count > 0;
			Vector2 snapOrigin = snapping ? busPlacementPoints[^1] : Vector2.zero;

			Vector2 mousePos = MouseHelper.CalculateAxisSnappedMousePosition(snapOrigin, snapping);

			// Update position while placing first pin
			if (placementState == BusDisplay.PlacementState.PlacingFirstPin)
			{
				SetActiveChipsPosition(mousePos);
			}

			// Placing wire
			if (placementState == BusDisplay.PlacementState.PlacingWire)
			{
				Vector2 offset = mousePos - busPlacementPoints[^1];
				Vector2 dir;
				if (offset.sqrMagnitude > 0)
				{
					dir = offset.normalized;
				}
				else
				{
					dir = busPlacementPoints.Count > 1 ? (busPlacementPoints[^1] - busPlacementPoints[^2]).normalized : Vector2.right;
				}

				float spacing = multiChipSpacing + busChips[0].GetBounds().size.y;

				for (int i = 0; i < busChips.Length; i++)
				{
					float ti = i - (busChips.Length - 1) / 2f;
					Vector2 offsetFromLast = mousePos - busPlacementPoints[^1];
					Vector2 dirFromLast = offsetFromLast.normalized;

					Vector2 busPoint = mousePos + new Vector2(dir.y, -dir.x) * ti * spacing;

					// Rotate bus points to face new direction
					Vector2 prevBusPointDesired = busPlacementPoints[^1] + new Vector2(dir.y, -dir.x) * ti * spacing;
					// If anchor points have been added to the bus line, then handle offsetting those anchor points to make bus lines stay same dst apart
					// (for when placing multiple bus lines at a time)
					if (busPlacementPoints.Count > 1)
					{
						Vector2 dirOld = (busPlacementPoints[^1] - busPlacementPoints[^2]).normalized;
						Vector2 prevBusPoint = busPlacementPoints[^1] + new Vector2(dirOld.y, -dirOld.x) * ti * spacing;
						var info = MathsHelper.LineIntersectsLine(prevBusPoint, prevBusPoint + dirOld, busPoint, busPoint + dir);
						if (info.intersects)
						{
							prevBusPointDesired = info.intersectionPoint;
						}
						// Handle moving back on self (todo, write a comment that makes sense...)
						float xt = Mathf.InverseLerp(-0.5f, -0.95f, Vector2.Dot(dirOld, dir));
						prevBusPointDesired = Vector2.Lerp(prevBusPointDesired, prevBusPoint, xt);

						// Flip order of points if moving sharply back on self
						if (Vector2.Dot(dirOld, dir) < -0.707f)
						{
							busPoint = mousePos + new Vector2(dir.y, -dir.x) * (-ti) * spacing;
						}
					}
					busChips[i].UpdatePrevBusPoint(prevBusPointDesired);


					Wire wire = busChips[i].Wire;


					busChips[i].UpdateWirePlacementPreview(busPoint);
				}

				// Shift left click to add wire points when placing bus
				if (Mouse.current.leftButton.wasPressedThisFrame && shiftKeyDown)
				{
					busPlacementPoints.Add(mousePos);
					foreach (var b in busChips)
					{
						b.AddWireAnchor();
					}
				}
			}
			// Left click or enter to confirm placement of pin
			if ((Mouse.current.leftButton.wasPressedThisFrame && !shiftKeyDown) || Keyboard.current.enterKey.wasPressedThisFrame)
			{
				if (CanPlaceActiveChips())
				{
					for (int i = 0; i < busChips.Length; i++)
					{
						busChips[i].PlacePin();

					}
					if (placementState == BusDisplay.PlacementState.PlacingWire)
					{
						FinishPlacingActiveChips();
					}
					busPlacementPoints = new List<Vector2>() { mousePos };
				}
			}
		}

		void HandleChipCancellationInput()
		{
			// Right click or escape key to cancel placement of chip
			if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
			{
				DestroyActiveChips();
			}
		}

		void FinishPlacingActiveChips()
		{
			if (IsPlacingChip)
			{
				foreach (var chip in activeChips)
				{
					chip.ChipDeleted -= OnChipDeletedBeforePlacement;
					chip.transform.position = chip.transform.position.WithZ(RenderOrder.Chip);
					chip.FinishPlacing();
					OnFinishedPlacingOrLoadingChip(chip, wasLoaded: false);
				}

				activeChips.Clear();
			}
		}

		void StartPlacingChip(ChipDescription chipDescription, int id)
		{
			if (IsPlacingChip && activeChips[0].Name != chipDescription.Name)
			{
				DestroyActiveChips();
			}

			ChipBase newChip = InstantiateChip(chipDescription);
			activeChips.Add(newChip);
			newChip.ChipDeleted += OnChipDeletedBeforePlacement;
			newChip.StartPlacing(chipDescription, id);
			lastCreatedChipDescription = chipDescription;

			OnStartedPlacingOrLoadingChip(newChip, isLoading: false);
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

		void OnChipDeletedBeforePlacement(ChipBase chipBase)
		{
			if (activeChips.Contains(chipBase))
			{
				activeChips.Remove(chipBase);
			}
		}

		void DestroyActiveChips()
		{
			if (IsPlacingChip)
			{
				foreach (var chip in activeChips)
				{
					chip.ChipDeleted -= OnChipDeletedBeforePlacement;
					chip.Delete();
				}
				activeChips.Clear();
			}
		}

		void SetActiveChipsPosition(Vector2 centre)
		{
			float boundsSize = activeChips[0].GetBounds().size.y;

			for (int i = 0; i < activeChips.Count; i++)
			{
				Vector3 pos = centre.WithZ(RenderOrder.ChipMoving) + Vector3.down * CalculateSpacing(i, activeChips.Count, boundsSize);
				activeChips[i].transform.position = pos;
			}
		}

		float CalculateSpacing(int i, int count, float boundsSize)
		{
			return (boundsSize + multiChipSpacing) * (i - (count - 1) / 2f);
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