using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Allows player to add/remove/move/rename inputs or outputs of a chip.
public class ChipInterfaceEditor : InteractionHandler {

	const int maxGroupSize = 8;

	public event System.Action<Chip> onDeleteChip;
	public event System.Action onChipsAddedOrDeleted;

	public enum EditorType { Input, Output }
	public enum HandleState { Default, Highlighted, Selected }
	const float forwardDepth = -0.1f;

	public List<ChipSignal> signals { get; private set; }

	public EditorType editorType;

	[Header ("References")]
	public Transform chipContainer;
	public ChipSignal signalPrefab;
	public RectTransform propertiesUI;
	public TMPro.TMP_InputField nameField;
	public UnityEngine.UI.Button deleteButton;
	public UnityEngine.UI.Toggle twosComplementToggle;
	public TMPro.TMP_Dropdown modeDropdown;
	public Transform signalHolder;

	[Header ("Appearance")]
	public Vector2 handleSize;
	public Color handleCol;
	public Color highlightedHandleCol;
	public Color selectedHandleCol;
	public float propertiesUIX;
	public Vector2 propertiesHeightMinMax;
	public bool showPreviewSignal;
	public float groupSpacing = 1;

	ChipSignal highlightedSignal;
	public List<ChipSignal> selectedSignals { get; private set; }
	ChipSignal[] previewSignals;

	BoxCollider2D inputBounds;

	Mesh quadMesh;
	Material handleMat;
	Material highlightedHandleMat;
	Material selectedHandleMat;
	bool mouseInInputBounds;

	// Dragging
	bool isDragging;
	float dragHandleStartY;
	float dragMouseStartY;

	// Grouping
	int currentGroupSize = 1;
	int currentGroupID;
	Dictionary<int, ChipSignal[]> groupsByID;

	void Awake () {
		signals = new List<ChipSignal> ();
		selectedSignals = new List<ChipSignal> ();
		groupsByID = new Dictionary<int, ChipSignal[]> ();

		inputBounds = GetComponent<BoxCollider2D> ();
		MeshShapeCreator.CreateQuadMesh (ref quadMesh);
		handleMat = CreateUnlitMaterial (handleCol);
		highlightedHandleMat = CreateUnlitMaterial (highlightedHandleCol);
		selectedHandleMat = CreateUnlitMaterial (selectedHandleCol);

		previewSignals = new ChipSignal[maxGroupSize];
		for (int i = 0; i < maxGroupSize; i++) {
			var previewSignal = Instantiate (signalPrefab);
			previewSignal.SetInteractable (false);
			previewSignal.gameObject.SetActive (false);
			previewSignal.signalName = "Preview";
			previewSignal.transform.SetParent (transform, true);
			previewSignals[i] = previewSignal;
		}

		propertiesUI.gameObject.SetActive (false);
		deleteButton.onClick.AddListener (DeleteSelected);
		modeDropdown.onValueChanged.AddListener(ModeChanged);
	}

	// Event handler when changed input or output pin wire type
	private void ModeChanged(int mode)
	{
		if (selectedSignals.Count == 0)
			return;

		//Change output pin wire mode
		foreach (var pin in selectedSignals.SelectMany(x => x.inputPins))
		{
			pin.wireType = (Pin.WireType)mode;
		}

		//Change input pin wire mode
		if (selectedSignals[0] is InputSignal)
		{
			foreach (InputSignal signal in selectedSignals)
			{
				var pin = signal.outputPins[0];
				if (pin == null)
					return;
				pin.wireType = (Pin.WireType)mode;
				//Turn off input pin
				if (pin.State == 1)
					signal.ToggleActive();
			}
		}
		//modeDropdown.SetValueWithoutNotify(0);
	}

	public override void OrderedUpdate () {
		if (!InputHelper.MouseOverUIObject ()) {
			UpdateColours ();
			HandleInput ();
		}
		DrawSignalHandles ();
	}

	public void LoadSignal (ChipSignal signal) {
		signal.transform.parent = signalHolder;
		signals.Add (signal);
	}

	void HandleInput () {
		Vector2 mousePos = InputHelper.MouseWorldPos;

		mouseInInputBounds = inputBounds.OverlapPoint (mousePos);
		if (mouseInInputBounds) {
			RequestFocus ();
		}

		if (HasFocus) {

			highlightedSignal = GetSignalUnderMouse ();


			// If a signal is highlighted (mouse is over its handle), then select it on mouse press
			if (highlightedSignal) {
				if (Input.GetMouseButtonDown (0)) {
					SelectSignal (highlightedSignal);
				}
			}

			// If a signal is selected, handle movement/renaming/deletion
			if (selectedSignals.Count > 0) {
				if (isDragging) {
					float handleNewY = (mousePos.y + (dragHandleStartY - dragMouseStartY));
					bool cancel = Input.GetKeyDown (KeyCode.Escape);
					if (cancel) {
						handleNewY = dragHandleStartY;
					}

					for (int i = 0; i < selectedSignals.Count; i++) {
						float y = CalcY (handleNewY, selectedSignals.Count, i);
						SetYPos (selectedSignals[i].transform, y);
					}

					if (Input.GetMouseButtonUp (0)) {
						isDragging = false;
					}

					// Cancel drag and deselect
					if (cancel) {
						FocusLost ();
					}
				}

				UpdateUIProperties ();

				// Finished with selected signal, so deselect it
				if (Input.GetKeyDown (KeyCode.Return)) {
					FocusLost ();
				}

			}

			HidePreviews ();
			if (highlightedSignal == null && !isDragging) {
				if (mouseInInputBounds) {

					if (InputHelper.AnyOfTheseKeysDown (KeyCode.Plus, KeyCode.KeypadPlus, KeyCode.Equals)) {
						currentGroupSize = Mathf.Clamp (currentGroupSize + 1, 1, maxGroupSize);
					} else if (InputHelper.AnyOfTheseKeysDown (KeyCode.Minus, KeyCode.KeypadMinus, KeyCode.Underscore)) {
						currentGroupSize = Mathf.Clamp (currentGroupSize - 1, 1, maxGroupSize);
					}

					HandleSpawning ();

				}
			}
		}
	}

	float CalcY (float mouseY, int groupSize, int index) {
		float centreY = mouseY;
		float halfExtent = groupSpacing * (groupSize - 1f);
		float maxY = centreY + halfExtent + handleSize.y / 2f;
		float minY = centreY - halfExtent - handleSize.y / 2f;

		if (maxY > BoundsTop) {
			centreY -= (maxY - BoundsTop);
		} else if (minY < BoundsBottom) {
			centreY += (BoundsBottom - minY);
		}

		float t = (groupSize > 1) ? index / (groupSize - 1f) : 0.5f;
		t = t * 2 - 1;
		float posY = centreY - t * halfExtent;
		return posY;
	}

	public ChipSignal[][] GetGroups () {
		var keys = groupsByID.Keys;
		ChipSignal[][] groups = new ChipSignal[keys.Count][];
		int i = 0;
		foreach (var key in keys) {
			groups[i] = groupsByID[key];
			i++;
		}
		return groups;
	}

	// Handles spawning if user clicks, otherwise displays preview
	void HandleSpawning () {

		float containerX = chipContainer.position.x + chipContainer.localScale.x / 2 * ((editorType == EditorType.Input) ? -1 : 1);
		float centreY = ClampY (InputHelper.MouseWorldPos.y);

		// Spawn on mouse down
		if (Input.GetMouseButtonDown (0)) {
			bool isGroup = currentGroupSize > 1;
			ChipSignal[] spawnedSignals = new ChipSignal[currentGroupSize];

			for (int i = 0; i < currentGroupSize; i++) {
				float posY = CalcY (InputHelper.MouseWorldPos.y, currentGroupSize, i);
				Vector3 spawnPos = new Vector3 (containerX, posY, chipContainer.position.z + forwardDepth);

				ChipSignal spawnedSignal = Instantiate (signalPrefab, spawnPos, Quaternion.identity, signalHolder);
				if (isGroup) {
					spawnedSignal.GroupID = currentGroupID;
					spawnedSignal.displayGroupDecimalValue = true;
				}
				signals.Add (spawnedSignal);
				spawnedSignals[i] = spawnedSignal;

			}

			if (isGroup) {
				groupsByID.Add (currentGroupID, spawnedSignals);
				// Reset group size after spawning
				currentGroupSize = 1;
				// Generate new ID for next group
				// This will be used to identify which signals were created together as a group
				currentGroupID++;
			}
			SelectSignal (signals[signals.Count - 1]);
			onChipsAddedOrDeleted?.Invoke ();
		}
		// Draw handle and signal previews
		else {
			for (int i = 0; i < currentGroupSize; i++) {
				float posY = CalcY (InputHelper.MouseWorldPos.y, currentGroupSize, i);
				Vector3 spawnPos = new Vector3 (containerX, posY, chipContainer.position.z + forwardDepth);
				DrawHandle (posY, HandleState.Highlighted);
				if (showPreviewSignal) {
					previewSignals[i].gameObject.SetActive (true);
					previewSignals[i].transform.position = spawnPos - Vector3.forward * forwardDepth;
				}
			}
		}
	}

	void HidePreviews () {
		for (int i = 0; i < previewSignals.Length; i++) {
			previewSignals[i].gameObject.SetActive (false);
		}
	}

	float BoundsTop {
		get {
			return transform.position.y + transform.localScale.y / 2;
		}
	}

	float BoundsBottom {
		get {
			return transform.position.y - transform.localScale.y / 2f;
		}
	}

	float ClampY (float y) {
		return Mathf.Clamp (y, BoundsBottom + handleSize.y / 2f, BoundsTop - handleSize.y / 2f);
	}

	protected override bool CanReleaseFocus () {
		if (isDragging) {
			return false;
		}
		if (mouseInInputBounds) {
			return false;
		}
		return true;
	}

	protected override void FocusLost () {
		highlightedSignal = null;
		selectedSignals.Clear ();
		propertiesUI.gameObject.SetActive (false);

		HidePreviews ();
		currentGroupSize = 1;
	}

	void UpdateUIProperties () {
		if (selectedSignals.Count > 0) {
			Vector3 centre = (selectedSignals[0].transform.position + selectedSignals[selectedSignals.Count - 1].transform.position) / 2;
			propertiesUI.transform.position = new Vector3 (centre.x + propertiesUIX, centre.y, propertiesUI.transform.position.z);

			// Update signal properties
			for (int i = 0; i < selectedSignals.Count; i++) {
				selectedSignals[i].UpdateSignalName (nameField.text);
				selectedSignals[i].useTwosComplement = twosComplementToggle.isOn;
			}
		}
	}

	void DrawSignalHandles () {
		for (int i = 0; i < signals.Count; i++) {
			HandleState handleState = HandleState.Default;
			if (signals[i] == highlightedSignal) {
				handleState = HandleState.Highlighted;
			}
			if (selectedSignals.Contains (signals[i])) {
				handleState = HandleState.Selected;
			}

			DrawHandle (signals[i].transform.position.y, handleState);
		}
	}

	ChipSignal GetSignalUnderMouse () {
		ChipSignal signalUnderMouse = null;
		float nearestDst = float.MaxValue;

		for (int i = 0; i < signals.Count; i++) {
			ChipSignal currentSignal = signals[i];
			float handleY = currentSignal.transform.position.y;

			Vector2 handleCentre = new Vector2 (transform.position.x, handleY);
			Vector2 mousePos = InputHelper.MouseWorldPos;

			const float selectionBufferY = 0.1f;

			float halfSizeX = transform.localScale.x / 2f;
			float halfSizeY = (handleSize.y + selectionBufferY) / 2f;
			bool insideX = mousePos.x >= handleCentre.x - halfSizeX && mousePos.x <= handleCentre.x + halfSizeX;
			bool insideY = mousePos.y >= handleCentre.y - halfSizeY && mousePos.y <= handleCentre.y + halfSizeY;

			if (insideX && insideY) {
				float dst = Mathf.Abs (mousePos.y - handleY);
				if (dst < nearestDst) {
					nearestDst = dst;
					signalUnderMouse = currentSignal;
				}
			}
		}
		return signalUnderMouse;
	}

	// Select signal (starts dragging, shows rename field)
	void SelectSignal (ChipSignal signalToDrag) {
		// Dragging
		selectedSignals.Clear ();

		for (int i = 0; i < signals.Count; i++) {
			if (signals[i] == signalToDrag || ChipSignal.InSameGroup (signals[i], signalToDrag)) {
				selectedSignals.Add (signals[i]);
			}
		}
		bool isGroup = selectedSignals.Count > 1;

		isDragging = true;

		var wireType = Pin.WireType.Simple;
		if (selectedSignals[0] is InputSignal)
		{
			var signal = selectedSignals[0];
			var pin = signal.outputPins[0];
			wireType = pin.wireType;
		}
		if (selectedSignals[0] is OutputSignal)
		{
			var signal = selectedSignals[0];
			var pin = signal.inputPins[0];
			wireType = pin.wireType;
		}

		dragMouseStartY = InputHelper.MouseWorldPos.y;
		if (selectedSignals.Count % 2 == 0) {
			int indexA = Mathf.Max (0, selectedSignals.Count / 2 - 1);
			int indexB = selectedSignals.Count / 2;
			dragHandleStartY = (selectedSignals[indexA].transform.position.y + selectedSignals[indexB].transform.position.y) / 2f;
		} else {
			dragHandleStartY = selectedSignals[selectedSignals.Count / 2].transform.position.y;
		}

		// Enable UI:
		propertiesUI.gameObject.SetActive (true);
		propertiesUI.sizeDelta = new Vector2 (propertiesUI.sizeDelta.x, (isGroup) ? propertiesHeightMinMax.y : propertiesHeightMinMax.x);
		nameField.text = selectedSignals[0].signalName;
		nameField.Select ();
		nameField.caretPosition = nameField.text.Length;
		twosComplementToggle.gameObject.SetActive (isGroup);
		twosComplementToggle.isOn = selectedSignals[0].useTwosComplement;
		modeDropdown.gameObject.SetActive(!isGroup);

		modeDropdown.SetValueWithoutNotify((int)wireType);
		UpdateUIProperties ();
	}

	void DeleteSelected () {
		for (int i = selectedSignals.Count - 1; i >= 0; i--) {
			ChipSignal signalToDelete = selectedSignals[i];
			if (groupsByID.ContainsKey (signalToDelete.GroupID)) {
				groupsByID.Remove (signalToDelete.GroupID);
			}
			onDeleteChip?.Invoke (signalToDelete);
			signals.Remove (signalToDelete);
			Destroy (signalToDelete.gameObject);
		}
		onChipsAddedOrDeleted?.Invoke ();
		selectedSignals.Clear ();
		FocusLost ();
	}

	void DrawHandle (float y, HandleState handleState = HandleState.Default) {
		float renderZ = forwardDepth;
		Material currentHandleMat;
		switch (handleState) {
			case HandleState.Highlighted:
				currentHandleMat = highlightedHandleMat;
				break;
			case HandleState.Selected:
				currentHandleMat = selectedHandleMat;
				renderZ = forwardDepth * 2;
				break;
			default:
				currentHandleMat = handleMat;
				break;
		}

		Vector3 scale = new Vector3 (handleSize.x, handleSize.y, 1);
		Vector3 pos3D = new Vector3 (transform.position.x, y, transform.position.z + renderZ);
		Matrix4x4 handleMatrix = Matrix4x4.TRS (pos3D, Quaternion.identity, scale);
		Graphics.DrawMesh (quadMesh, handleMatrix, currentHandleMat, 0);
	}

	Material CreateUnlitMaterial (Color col) {
		var mat = new Material (Shader.Find ("Unlit/Color"));
		mat.color = col;
		return mat;
	}

	void SetYPos (Transform t, float y) {
		t.position = new Vector3 (t.position.x, y, t.position.z);
	}

	void UpdateColours () {
		handleMat.color = handleCol;
		highlightedHandleMat.color = highlightedHandleCol;
		selectedHandleMat.color = selectedHandleCol;
	}
}