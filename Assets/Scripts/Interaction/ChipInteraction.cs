using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class ChipInteraction : InteractionHandler {

	public enum State { None, PlacingNewChips, MovingOldChips, SelectingChips }
	public event System.Action<Chip> onDeleteChip;

	public BoxCollider2D chipArea;
	public Transform chipHolder;
	public LayerMask chipMask;
	public Material selectionBoxMaterial;
	public float chipStackSpacing = 0.15f;
	public float selectionBoundsBorderPadding = 0.1f;
	public Color selectionBoxCol;
	public Color invalidPlacementCol;

	public EditChipMenu editChipMenu;

	const float dragDepth = -50;
	const float chipDepth = -0.2f;

	public List<Chip> allChips { get; private set; }

	State currentState;
	List<Chip> newChipsToPlace;
	List<Chip> selectedChips;
	Vector2 selectionBoxStartPos;
	Mesh selectionMesh;
	Vector3[] selectedChipsOriginalPos;

	void Awake () {
		newChipsToPlace = new List<Chip> ();
		selectedChips = new List<Chip> ();
		allChips = new List<Chip> ();
		MeshShapeCreator.CreateQuadMesh (ref selectionMesh);
		editChipMenu = GameObject.Find("Edit Chip Menu").GetComponent <EditChipMenu> ();
		editChipMenu.Init();
	}

	public override void OrderedUpdate () {

		switch (currentState) {
			case State.None:
				HandleSelection ();
				HandleDeletion ();
				break;
			case State.PlacingNewChips:
				HandleNewChipPlacement ();
				break;
			case State.SelectingChips:
				HandleSelectionBox ();
				break;
			case State.MovingOldChips:
				HandleChipMovement ();
				break;
		}
		DrawSelectedChipBounds ();
	}

	public void LoadChip (Chip chip) {
		chip.transform.parent = chipHolder;
		allChips.Add (chip);
	}

	public void SpawnChip (Chip chipPrefab) {
		RequestFocus ();
		if (HasFocus) {
			if (Input.GetMouseButtonDown(0))
			{
				// Spawn chip
				currentState = State.PlacingNewChips;
				if (newChipsToPlace.Count == 0)
				{
					selectedChips.Clear();
				}
				var newChip = Instantiate(chipPrefab, parent: chipHolder);
				newChip.gameObject.SetActive(true);
				selectedChips.Add(newChip);
				newChipsToPlace.Add(newChip);
			}
			else if (Input.GetMouseButtonDown(1))
			{
				// Open chip edit menu
				editChipMenu.EditChip(chipPrefab);
			}
		}
	}

	void HandleSelection () {

		Vector2 mousePos = InputHelper.MouseWorldPos;

		// Left mouse down. Handle selecting a chip, or starting to draw a selection box.
		if (Input.GetMouseButtonDown (0) && !InputHelper.MouseOverUIObject ()) {
			RequestFocus ();
			if (HasFocus) {
				selectionBoxStartPos = mousePos;
				GameObject objectUnderMouse = InputHelper.GetObjectUnderMouse2D (chipMask);

				// If clicked on nothing, clear selected items and start drawing selection box
				if (objectUnderMouse == null) {
					currentState = State.SelectingChips;
					selectedChips.Clear ();
				}
				// If clicked on a chip, select that chip and allow it to be moved
				else {
					currentState = State.MovingOldChips;
					Chip chipUnderMouse = objectUnderMouse.GetComponent<Chip> ();
					// If object is already selected, then selection of any other chips should be maintained so they can be moved as a group.
					// But if object is not already selected, then any currently selected chips should be deselected.
					if (!selectedChips.Contains (chipUnderMouse)) {
						selectedChips.Clear ();
						selectedChips.Add (chipUnderMouse);
					}
					// Record starting positions of all selected chips for movement
					selectedChipsOriginalPos = new Vector3[selectedChips.Count];
					for (int i = 0; i < selectedChips.Count; i++) {
						selectedChipsOriginalPos[i] = selectedChips[i].transform.position;
					}
				}
			}
		}
	}

	void HandleDeletion () {
		// Delete any selected chips
		if (InputHelper.AnyOfTheseKeysDown (KeyCode.Backspace, KeyCode.Delete)) {
			for (int i = selectedChips.Count - 1; i >= 0; i--) {
				DeleteChip (selectedChips[i]);
				selectedChips.RemoveAt (i);
			}
			newChipsToPlace.Clear ();
		}

	}

	void DeleteChip (Chip chip) {
		if (onDeleteChip != null) {
			onDeleteChip.Invoke (chip);
		}

		allChips.Remove (chip);
		Destroy (chip.gameObject);
	}

	void HandleSelectionBox () {
		Vector2 mousePos = InputHelper.MouseWorldPos;
		// While holding mouse down, keep drawing selection box
		if (Input.GetMouseButton (0)) {
			var pos = (Vector3) (selectionBoxStartPos + mousePos) / 2 + Vector3.back * 0.5f;
			var scale = new Vector3 (Mathf.Abs (mousePos.x - selectionBoxStartPos.x), Mathf.Abs (mousePos.y - selectionBoxStartPos.y), 1);
			selectionBoxMaterial.color = selectionBoxCol;
			Graphics.DrawMesh (selectionMesh, Matrix4x4.TRS (pos, Quaternion.identity, scale), selectionBoxMaterial, 0);
		}
		// Mouse released, so selected all chips inside the selection box
		if (Input.GetMouseButtonUp (0)) {
			currentState = State.None;

			// Select all objects under selection box
			Vector2 boxSize = new Vector2 (Mathf.Abs (mousePos.x - selectionBoxStartPos.x), Mathf.Abs (mousePos.y - selectionBoxStartPos.y));
			var allObjectsInBox = Physics2D.OverlapBoxAll ((selectionBoxStartPos + mousePos) / 2, boxSize, 0, chipMask);
			selectedChips.Clear ();
			foreach (var item in allObjectsInBox) {
				if (item.GetComponent<Chip> ()) {
					selectedChips.Add (item.GetComponent<Chip> ());
				}
			}
		}
	}

	void HandleChipMovement () {
		var mousePos = InputHelper.MouseWorldPos;

		if (Input.GetMouseButton (0)) {
			// Move selected objects
			Vector2 deltaMouse = mousePos - selectionBoxStartPos;
			for (int i = 0; i < selectedChips.Count; i++) {
				selectedChips[i].transform.position = (Vector2) selectedChipsOriginalPos[i] + deltaMouse;
				SetDepth (selectedChips[i], dragDepth + selectedChipsOriginalPos[i].z);
			}
		}
		// Mouse released, so stop moving chips
		if (Input.GetMouseButtonUp (0)) {
			currentState = State.None;

			if (SelectedChipsWithinPlacementArea ()) {
				const float chipMoveThreshold = 0.001f;
				Vector2 deltaMouse = mousePos - selectionBoxStartPos;

				// If didn't end up moving the chips, then select just the one under the mouse
				if (selectedChips.Count > 1 && deltaMouse.magnitude < chipMoveThreshold) {
					var objectUnderMouse = InputHelper.GetObjectUnderMouse2D (chipMask);
					if (objectUnderMouse?.GetComponent<Chip> ()) {
						selectedChips.Clear ();
						selectedChips.Add (objectUnderMouse.GetComponent<Chip> ());
					}
				} else {
					for (int i = 0; i < selectedChips.Count; i++) {
						SetDepth (selectedChips[i], selectedChipsOriginalPos[i].z);
					}
				}
			}
			// If any chip ended up outside of placement area, then put all chips back to their original positions
			else {
				for (int i = 0; i < selectedChipsOriginalPos.Length; i++) {
					selectedChips[i].transform.position = selectedChipsOriginalPos[i];
				}
			}

		}
	}

	// Handle placement of newly spawned chips
	void HandleNewChipPlacement () {
		// Cancel placement if esc or right mouse down
		if (InputHelper.AnyOfTheseKeysDown (KeyCode.Escape, KeyCode.Backspace, KeyCode.Delete) || Input.GetMouseButtonDown (1)) {
			CancelPlacement ();
		}
		// Move selected chip/s and place them on left mouse down
		else {
			Vector2 mousePos = InputHelper.MouseWorldPos;
			float offsetY = 0;

			for (int i = 0; i < newChipsToPlace.Count; i++) {
				Chip chipToPlace = newChipsToPlace[i];
				chipToPlace.transform.position = mousePos + Vector2.down * offsetY;
				SetDepth (chipToPlace, dragDepth);
				offsetY += chipToPlace.BoundsSize.y + chipStackSpacing;
			}

			// Place object
			if (Input.GetMouseButtonDown (0) && SelectedChipsWithinPlacementArea ()) {
				PlaceNewChips ();
			}
		}
	}

	void PlaceNewChips () {
		float startDepth = (allChips.Count > 0) ? allChips[allChips.Count - 1].transform.position.z : 0;
		for (int i = 0; i < newChipsToPlace.Count; i++) {
			SetDepth (newChipsToPlace[i], startDepth + (newChipsToPlace.Count - i) * chipDepth);
		}

		allChips.AddRange (newChipsToPlace);
		selectedChips.Clear ();
		newChipsToPlace.Clear ();
		currentState = State.None;
	}

	void CancelPlacement () {
		for (int i = newChipsToPlace.Count - 1; i >= 0; i--) {
			Destroy (newChipsToPlace[i].gameObject);
		}
		newChipsToPlace.Clear ();
		selectedChips.Clear ();
		currentState = State.None;
	}

	void DrawSelectedChipBounds () {

		if (SelectedChipsWithinPlacementArea ()) {
			selectionBoxMaterial.color = selectionBoxCol;
		} else {
			selectionBoxMaterial.color = invalidPlacementCol;
		}

		foreach (var item in selectedChips) {
			var pos = item.transform.position + Vector3.forward * -0.5f;
			float sizeX = item.BoundsSize.x + (Pin.radius + selectionBoundsBorderPadding * 0.75f);
			float sizeY = item.BoundsSize.y + selectionBoundsBorderPadding;
			Matrix4x4 matrix = Matrix4x4.TRS (pos, Quaternion.identity, new Vector3 (sizeX, sizeY, 1));
			Graphics.DrawMesh (selectionMesh, matrix, selectionBoxMaterial, 0);
		}
	}

	bool SelectedChipsWithinPlacementArea () {
		float bufferX = Pin.radius + selectionBoundsBorderPadding * 0.75f;
		float bufferY = selectionBoundsBorderPadding;
		Bounds area = chipArea.bounds;

		for (int i = 0; i < selectedChips.Count; i++) {
			Chip chip = selectedChips[i];
			float left = chip.transform.position.x - (chip.BoundsSize.x + bufferX) / 2;
			float right = chip.transform.position.x + (chip.BoundsSize.x + bufferX) / 2;
			float top = chip.transform.position.y + (chip.BoundsSize.y + bufferY) / 2;
			float bottom = chip.transform.position.y - (chip.BoundsSize.y + bufferY) / 2;

			if (left < area.min.x || right > area.max.x || top > area.max.y || bottom < area.min.y) {
				return false;
			}
		}
		return true;
	}

	void SetDepth (Chip chip, float depth) {
		chip.transform.position = new Vector3 (chip.transform.position.x, chip.transform.position.y, depth);
	}

	protected override bool CanReleaseFocus () {
		return currentState != State.PlacingNewChips && currentState != State.MovingOldChips;
	}

	protected override void FocusLost () {
		currentState = State.None;
		selectedChips.Clear ();
	}

}