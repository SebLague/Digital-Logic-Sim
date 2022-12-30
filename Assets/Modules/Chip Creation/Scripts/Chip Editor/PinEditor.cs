using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipCreation
{
	// Responsible for handling moving, renaming and deleting of editable pins.
	public class PinEditor : ControllerBase
	{
		PinUI pinUI;
		EditablePin selectedPin;
		EditablePinHandle handleUnderMouse;



		public override void SetUp(ChipEditor editor)
		{
			base.SetUp(editor);

			editor.PinPlacer.PinCreated += OnPinCreated;
			pinUI = FindObjectOfType<PinUI>();
			pinUI.NameChanged += OnNameChanged;
			pinUI.DeletePressed += OnDeletePin;
		}

		void Update()
		{
			if (MouseHelper.LeftMousePressedThisFrame())
			{
				if (selectedPin is not null)
				{
					if (selectedPin.Handle == handleUnderMouse)
					{
					}
					if (pinUI.MouseIsOverWindow())
					{
					}
					if (selectedPin.Handle != handleUnderMouse && !pinUI.MouseIsOverWindow())
					{
						selectedPin.Handle.Deselect();
					}
				}
				if (handleUnderMouse is not null)
				{
					handleUnderMouse.Select();
				}
			}

			if (MouseHelper.LeftMouseReleasedThisFrame() && chipEditor.CanEdit)
			{
				if (selectedPin is not null)
				{
					pinUI.Show(selectedPin.GetPin().transform.position, selectedPin.GetPin().IsInputType, selectedPin.PinName);
				}
			}
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
			pinUI.Hide();
		}

		void OnNameChanged(string newName)
		{
			selectedPin.SetName(newName);
		}

		void OnPinMoved(EditablePin pin)
		{
			pinUI.SetPosition(pin.GetPin().transform.position, pin.GetPin().IsInputType);
		}

		void OnPinCreated(EditablePin pin)
		{
			if (selectedPin is not null && pin != selectedPin)
			{
				selectedPin.Handle.Deselect();
			}
			pin.Handle.HandleSelected += OnPinSelected;
			pin.Handle.HandleDeselected += OnPinDeselected;
			pin.Handle.HandleMoved += OnPinMoved;
			pin.Handle.MouseInteraction.MouseEntered += OnMouseOverHandle;
			pin.Handle.MouseInteraction.MouseExitted += OnMouseExitHandle;
			//handleUnderMouse = pin.Handle;
			//pin.Handle.Select();
		}

		void OnMouseOverHandle(EditablePinHandle handle)
		{
			handleUnderMouse = handle;
		}

		void OnMouseExitHandle(EditablePinHandle handle)
		{
			handleUnderMouse = null;
		}

		void OnDeletePin()
		{
			selectedPin.Delete();
			selectedPin = null;
		}

		void OnDestroy()
		{
			pinUI = FindObjectOfType<PinUI>();
			if (pinUI is not null)
			{
				pinUI.NameChanged -= OnNameChanged;
				pinUI.DeletePressed -= OnDeletePin;
			}
		}
	}
}