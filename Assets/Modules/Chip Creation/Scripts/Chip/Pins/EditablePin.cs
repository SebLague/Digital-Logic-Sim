using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DLS.Simulation;
using DLS.ChipData;

namespace DLS.ChipCreation
{
	public class EditablePin : MonoBehaviour
	{
		public event System.Action<EditablePin> EditablePinDeleted;
		public string PinName => pin.PinName;
		public PinState State => pin.State;
		public Pin GetPin() => pin;
		public EditablePinHandle Handle => handle;

		[SerializeField] EditablePinIndicator indicatorPin;
		[SerializeField] Pin pin;
		[SerializeField] Transform connectionGraphic;
		[SerializeField] EditablePinHandle handle;
		[SerializeField] Transform[] flip;

		bool isInputPin;

		public void SetUp(bool isInputPin, string name, Palette.VoltageColour theme, int id)
		{
			this.isInputPin = isInputPin;

			ConfigureGraphics(isInputPin);

			PinDescription description = new PinDescription()
			{
				Name = name,
				ID = id,
				PositionY = transform.position.y
			};

			pin.SetUp(null, description, (isInputPin) ? PinType.ChipInputPin : PinType.ChipOutputPin, theme);
			pin.ColourThemeChanged += (c)=> UpdateDisplayColour();
			UpdateDisplayColour();

			if (isInputPin)
			{
				indicatorPin.MouseInteraction.LeftMouseDown += (e) => TogglePinState();
			}


			pin.State = PinState.LOW;
			handle.SetUp();
		}

		void TogglePinState()
		{
			if (pin.State == PinState.LOW)
			{
				pin.State = PinState.HIGH;
			}
			else if (pin.State == PinState.HIGH)
			{
				pin.State = PinState.LOW;
			}
		}

		public void UpdateDisplayState()
		{
			transform.position = transform.position.WithZ(State == PinState.HIGH ? RenderOrder.EditablePinHigh : RenderOrder.EditablePin);
			UpdateDisplayColour();
		}

		void UpdateDisplayColour()
		{
			indicatorPin.SetColour(pin.ColourTheme.GetColour(State));
		}

		public void Delete()
		{
			pin.NotifyOfDeletion();
			EditablePinDeleted?.Invoke(this);
			Destroy(gameObject);
		}

		public void SetName(string name)
		{
			pin.SetName(name);
		}

		public void ConfigureGraphics(bool isInput)
		{
			if (!isInput)
			{
				foreach (Transform t in flip)
				{
					t.localPosition = new Vector3(-t.localPosition.x, t.localPosition.y, t.localPosition.z);
				}
			}
		}

	}
}