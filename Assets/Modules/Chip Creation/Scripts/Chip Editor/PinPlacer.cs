using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using DLS.ChipData;
using System.Linq;

namespace DLS.ChipCreation
{
	public class PinPlacer : ControllerBase
	{
		enum PinPreviewState { None, PreviewInput, PreviewOutput }

		public event System.Action<EditablePin> PinCreated;
		public event System.Action<EditablePin> PinDeleted;
		public ReadOnlyCollection<EditablePin> InputPins { get; private set; }
		public ReadOnlyCollection<EditablePin> OutputPins { get; private set; }
		public ReadOnlyCollection<EditablePin> AllPins { get; private set; }

		[SerializeField] Transform ioPinHolder;
		[SerializeField] EditablePin editablePinPrefab;
		[SerializeField] Color pinPreviewCol;

		List<EditablePin> inputPins;
		List<EditablePin> outputPins;
		Transform previewInputPin;
		Transform previewOutputPin;

		PinPreviewState pinPreviewState;
		EditablePin selectedPin;
		System.Random rng;

		public override void SetUp(ChipEditor editor)
		{
			base.SetUp(editor);
			rng = new System.Random();

			inputPins = new List<EditablePin>();
			outputPins = new List<EditablePin>();
			RefreshPinCollections();

			editor.WorkArea.InputBarMouseInteraction.LeftMouseDown += (input) => AddPin(true, true);
			editor.WorkArea.OutputBarMouseInteraction.LeftMouseDown += (input) => AddPin(false, true);

			editor.WorkArea.InputBarMouseInteraction.MouseEntered += OnMouseEnterPinBar;
			editor.WorkArea.OutputBarMouseInteraction.MouseEntered += OnMouseEnterPinBar;
			editor.WorkArea.InputBarMouseInteraction.MouseExitted += OnMouseExitBar;
			editor.WorkArea.OutputBarMouseInteraction.MouseExitted += OnMouseExitBar;

			editor.WorkArea.WorkAreaResized += OnWorkAreaResized;

			previewInputPin = CreatePreviewPin(true);
			previewOutputPin = CreatePreviewPin(false);
		}

		void Update()
		{
			if (chipEditor.AnyControllerBusy() || pinPreviewState == PinPreviewState.None)
			{
				previewInputPin.gameObject.SetActive(false);
				previewOutputPin.gameObject.SetActive(false);
			}
			else if (pinPreviewState == PinPreviewState.PreviewInput) {
				previewInputPin.gameObject.SetActive(true);
				previewInputPin.position = GetPosition(true).WithZ(RenderOrder.EditablePinPreview);
			}
			else if (pinPreviewState == PinPreviewState.PreviewOutput) {
				previewOutputPin.gameObject.SetActive(true);
				previewOutputPin.position = GetPosition(false).WithZ(RenderOrder.EditablePinPreview);
			}
		}

		void OnMouseEnterPinBar(bool isInputPin)
		{
			if (chipEditor.CanEdit)
			{
				if (selectedPin is null || !selectedPin.Handle.IsDragging)
				{
					pinPreviewState = isInputPin ? PinPreviewState.PreviewInput : PinPreviewState.PreviewOutput;
				}
			}
		}

		void OnMouseExitBar(bool isInputPin)
		{
			pinPreviewState = PinPreviewState.None;

		}

		public void AddPin(bool isInputPin, bool select = false)
		{
			if (chipEditor.CanEdit)
			{
				AddPin(isInputPin, GetPosition(isInputPin).y, select: select);
			}
		}

		public void AddPin(bool isInputPin, float posY, string name = "Pin", bool select = false)
		{
			if (chipEditor.CanEdit)
			{
				float posX = GetPosition(isInputPin).x;
				int id = GenerateID();
				AddPin(isInputPin, new Vector2(posX, posY), name, select, "", id);
			}
		}

		public void LoadPin(bool isInputPin, PinDescription description)
		{
			float posX = GetPosition(isInputPin).x;
			AddPin(isInputPin, new Vector2(posX, description.PositionY), description.Name, false, description.ColourThemeName, description.ID);
		}

		void AddPin(bool isInputPin, Vector2 pos, string name, bool select, string themeName, int id)
		{
			var editablePin = SpawnPin(pos);
			editablePin.SetUp(isInputPin, name, chipEditor.ColourThemes.GetTheme(themeName), id);


			(isInputPin ? inputPins : outputPins).Add(editablePin);
			RefreshPinCollections();
			editablePin.EditablePinDeleted += OnPinDeleted;
			PinCreated?.Invoke(editablePin);

			editablePin.Handle.HandleSelected += OnPinSelected;
			editablePin.Handle.HandleDeselected += OnPinDeselected;

			if (select)
			{
				editablePin.Handle.Select();
			}

		}

		EditablePin SpawnPin(Vector2 pos)
		{
			var editablePin = Instantiate(editablePinPrefab, parent: ioPinHolder);
			editablePin.transform.position = pos.WithZ(RenderOrder.EditablePin);
			return editablePin;
		}

		float GetPosX(bool isInputPin)
		{
			return (isInputPin ? -1 : 1) * chipEditor.WorkArea.Width / 2;
		}

		Vector3 GetPosition(bool isInputPin)
		{
			float posY = MouseHelper.GetMouseWorldPosition().y;
			float posX = GetPosX(isInputPin);
			return new Vector3(posX, posY, RenderOrder.EditablePin);
		}

		void OnPinDeleted(EditablePin editablePin)
		{
			PinDeleted?.Invoke(editablePin);
			(editablePin.GetPin().IsInputType ? inputPins : outputPins).Remove(editablePin);
			RefreshPinCollections();
		}

		void OnPinSelected(EditablePin pin)
		{
			selectedPin = pin;
		}

		void OnPinDeselected(EditablePin pin)
		{
			if (selectedPin == pin)
			{
				selectedPin = null;
			}
		}

		void OnWorkAreaResized()
		{
			foreach (var pin in inputPins)
			{
				pin.transform.position = new Vector3(GetPosX(pin.GetPin().IsInputType), pin.transform.position.y, pin.transform.position.z);
				pin.GetPin().NotifyMoved();
			}
			foreach (var pin in outputPins)
			{
				pin.transform.position = new Vector3(GetPosX(pin.GetPin().IsInputType), pin.transform.position.y, pin.transform.position.z);
				pin.GetPin().NotifyMoved();
			}
		}

		Transform CreatePreviewPin(bool inputPin)
		{
			var previewPin = SpawnPin(GetPosition(inputPin));
			previewPin.ConfigureGraphics(inputPin);
			foreach (MeshRenderer r in previewPin.transform.GetComponentsInChildren<MeshRenderer>())
			{
				r.material.color = pinPreviewCol;
			}
			foreach (Collider2D c in previewPin.transform.GetComponentsInChildren<Collider2D>())
			{
				Destroy(c);
			}
			foreach (MonoBehaviour b in previewPin.transform.GetComponentsInChildren<MonoBehaviour>())
			{
				Destroy(b);
			}

			previewPin.gameObject.SetActive(false);
			return previewPin.transform;
		}

		void RefreshPinCollections()
		{
			InputPins = new ReadOnlyCollection<EditablePin>(inputPins);
			OutputPins = new ReadOnlyCollection<EditablePin>(outputPins);
			AllPins = new ReadOnlyCollection<EditablePin>(inputPins.Concat(outputPins).ToArray());
		}

		int GenerateID()
		{
			int id;

			// Just for peace of mind...
			while (true)
			{
				id = rng.Next();
				if (!(inputPins.Any(pin => pin.GetPin().ID == id) || outputPins.Any(pin => pin.GetPin().ID == id)))
				{
					break;
				}
			}

			return id;
		}
	}
}