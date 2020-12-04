using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Allows player to add/remove/move/rename inputs or outputs of a chip.
public class ChipInterfaceEditor : InteractionHandler {

	public event System.Action<Chip> onDeleteChip;
	public enum EditorType { Input, Output }
	public enum HandleState { Default, Highlighted, Selected }
	const float forwardDepth = -0.1f;

	public List<ChipSignal> signals { get; private set; }

	public EditorType editorType;

	[Header ("References")]
	public Transform chipContainer;
	public ChipSignal signalPrefab;
	public TMPro.TMP_InputField nameField;
	public UnityEngine.UI.Button deleteButton;
	public Transform signalHolder;

	[Header ("Appearance")]
	public Vector2 handleSize;
	public Color handleCol;
	public Color highlightedHandleCol;
	public Color selectedHandleCol;
	public float renameFieldX = 2;
	public float deleteButtonX = 1;
	public bool showPreviewSignal;

	ChipSignal highlightedSignal;
	ChipSignal selectedSignal;
	ChipSignal previewSignal;

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

	void Awake () {
		signals = new List<ChipSignal> ();
		inputBounds = GetComponent<BoxCollider2D> ();
		MeshShapeCreator.CreateQuadMesh (ref quadMesh);
		handleMat = CreateUnlitMaterial (handleCol);
		highlightedHandleMat = CreateUnlitMaterial (highlightedHandleCol);
		selectedHandleMat = CreateUnlitMaterial (selectedHandleCol);
		previewSignal = Instantiate (signalPrefab);
		previewSignal.SetInteractable (false);
		previewSignal.gameObject.SetActive (false);
		previewSignal.signalName = "Preview";
		previewSignal.transform.SetParent (transform, true);

		deleteButton.onClick.AddListener (Delete);
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
			if (selectedSignal) {
				if (isDragging) {
					float handleNewY = ClampY (mousePos.y + (dragHandleStartY - dragMouseStartY));
					SetYPos (selectedSignal.transform, handleNewY);
					if (Input.GetMouseButtonUp (0)) {
						isDragging = false;
					}

					// Cancel drag and deselect
					if (Input.GetKeyDown (KeyCode.Escape)) {
						SetYPos (selectedSignal.transform, dragHandleStartY);
						FocusLost ();
					}
				}

				UpdateButtonAndNameField ();

				// Finished with selected signal, so deselect it
				if (Input.GetKeyDown (KeyCode.Return)) {
					FocusLost ();
				}

			}

			previewSignal.gameObject.SetActive (false);
			if (highlightedSignal == null && !isDragging) {
				if (mouseInInputBounds) {
					float handleY = ClampY (mousePos.y);

					DrawHandle (handleY, HandleState.Highlighted);
					float containerX = chipContainer.position.x + chipContainer.localScale.x / 2 * ((editorType == EditorType.Input) ? -1 : 1);
					Vector3 spawnPos = new Vector3 (containerX, handleY, chipContainer.position.z + forwardDepth);

					if (showPreviewSignal) {
						previewSignal.gameObject.SetActive (true);
						previewSignal.transform.position = spawnPos - Vector3.forward * forwardDepth;
					}

					// Spawn
					if (Input.GetMouseButtonDown (0)) {
						ChipSignal spawnedSignal = Instantiate (signalPrefab, spawnPos, Quaternion.identity, signalHolder);
						signals.Add (spawnedSignal);
						SelectSignal (spawnedSignal);
					}
				}
			}
		}
	}

	float ClampY (float y) {
		float minY = transform.position.y - transform.localScale.y / 2f + handleSize.y / 2f;
		float maxY = transform.position.y + transform.localScale.y / 2 - handleSize.y / 2f;
		return Mathf.Clamp (y, minY, maxY);
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
		selectedSignal = null;

		deleteButton.gameObject.SetActive (false);
		nameField.gameObject.SetActive (false);
		previewSignal.gameObject.SetActive (false);
	}

	void UpdateButtonAndNameField () {
		if (selectedSignal) {
			deleteButton.transform.position = Camera.main.WorldToScreenPoint (selectedSignal.transform.position + Vector3.right * deleteButtonX);
			// Update signal name
			selectedSignal.UpdateSignalName (nameField.text);
			nameField.transform.position = Camera.main.WorldToScreenPoint (selectedSignal.transform.position + Vector3.right * renameFieldX);
		}
	}

	void DrawSignalHandles () {
		for (int i = 0; i < signals.Count; i++) {
			HandleState handleState = HandleState.Default;
			if (signals[i] == highlightedSignal) {
				handleState = HandleState.Highlighted;
			}
			if (signals[i] == selectedSignal) {
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

			const float selectionBufferX = 0.1f;
			const float selectionBufferY = 0.1f;

			float halfSizeX = (handleSize.x + selectionBufferX) / 2f;
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
		selectedSignal = signalToDrag;
		isDragging = true;

		dragMouseStartY = InputHelper.MouseWorldPos.y;
		dragHandleStartY = selectedSignal.transform.position.y;

		// Name input field
		nameField.gameObject.SetActive (true);
		nameField.text = (selectedSignal).signalName;
		nameField.Select ();
		// Delete button
		deleteButton.gameObject.SetActive (true);
		UpdateButtonAndNameField ();

	}

	void Delete () {
		if (selectedSignal) {
			onDeleteChip?.Invoke (selectedSignal);
			signals.Remove (selectedSignal);
			Destroy (selectedSignal.gameObject);
			selectedSignal = null;
			FocusLost ();
		}
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