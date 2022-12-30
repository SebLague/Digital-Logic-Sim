using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SebInput;
using UnityEngine.InputSystem;

namespace DLS.ChipCreation
{
	public class EditablePinHandle : MonoBehaviour
	{

		public event System.Action<EditablePin> HandleSelected;
		public event System.Action<EditablePin> HandleDeselected;
		public event System.Action<EditablePin> HandleMoved;
		public bool IsDragging => isDragging;
		public MouseInteraction<EditablePinHandle> MouseInteraction { get; private set; }

		[SerializeField] EditablePin editablePin;
		[SerializeField] Color normalCol;
		[SerializeField] Color highlightedCol;
		[SerializeField] Color selectedCol;
		[SerializeField] MeshRenderer graphic;

		Material material;
		bool isSelected;
		bool isDragging;
		Vector2 dragStartMousePos;
		Vector2 dragStartPos;

		public void SetUp()
		{
			MouseInteraction = new MouseInteraction<EditablePinHandle>(gameObject, this);

			MouseInteraction.MouseEntered += OnMouseEnter;
			MouseInteraction.MouseExitted += OnMouseExit;

			material = Material.Instantiate(graphic.sharedMaterial);
			graphic.sharedMaterial = material;
			SetColour(normalCol);
		}

		void LateUpdate()
		{

			if (MouseHelper.LeftMouseReleasedThisFrame() && isDragging)
			{
				isDragging = false;
				float z = editablePin.State == Simulation.PinState.HIGH ? RenderOrder.EditablePinHigh : RenderOrder.EditablePin;
				editablePin.transform.position = editablePin.transform.position.WithZ(z);
			}

			if (Keyboard.current.enterKey.wasPressedThisFrame)
			{
				OnDeselect();
			}

			if (isDragging)
			{
				float mouseY = MouseHelper.GetMouseWorldPosition().y;
				float posY = dragStartPos.y + (mouseY - dragStartMousePos.y);
				if (Mathf.Abs(posY - editablePin.transform.position.y) > 0.0001f)
				{
					editablePin.transform.position = new Vector3(dragStartPos.x, posY, RenderOrder.EditablePinPreview);
					HandleMoved?.Invoke(editablePin);
					editablePin.GetPin().NotifyMoved();
				}
			}
		}

		void OnMouseEnter(EditablePinHandle handle)
		{
			if (!isSelected)
			{
				SetColour(highlightedCol);
			}
		}

		void OnMouseExit(EditablePinHandle handle)
		{
			if (!isSelected)
			{
				SetColour(normalCol);
			}
		}

		public void Select()
		{
			OnSelect();
		}

		public void Deselect()
		{
			OnDeselect();
		}

		void OnSelect()
		{
			isDragging = true;
			dragStartMousePos = MouseHelper.GetMouseWorldPosition();
			dragStartPos = editablePin.transform.position;

			if (!isSelected)
			{
				isSelected = true;
				SetColour(selectedCol);
				HandleSelected?.Invoke(editablePin);
			}
		}

		void OnDeselect()
		{
			if (isSelected)
			{
				isSelected = false;
				SetColour(normalCol);
				HandleDeselected?.Invoke(editablePin);
			}
		}

		public void SetColour(Color col)
		{
			material.color = col;
		}


	}
}