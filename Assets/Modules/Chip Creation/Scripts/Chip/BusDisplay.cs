using System.Collections;
using System.Collections.Generic;
using DLS.ChipData;
using UnityEngine;

namespace DLS.ChipCreation
{
	public class BusDisplay : ChipBase
	{
		public enum PlacementState { PlacingFirstPin, PlacingWire, Finished }
		public PlacementState CurrentPlacementState { get; private set; }

		[Header("References")]
		[SerializeField] Pin standalonePinPrefab;
		[SerializeField] Wire wirePrefab;
		[SerializeField] Palette palette;
		[SerializeField] MeshRenderer highlight;

		Wire wire;
		Pin pinA;
		Pin pinB;

		public override void Load(ChipDescription description, ChipInstanceData instanceData)
		{
			base.Load(description, instanceData);
			CurrentPlacementState = PlacementState.Finished;
		}

		public override void StartPlacing(ChipDescription description, int id)
		{
			base.StartPlacing(description, id);
			pinA = CreatePin(transform.position, true);
			CurrentPlacementState = PlacementState.PlacingFirstPin;
			highlight.transform.localScale = (Vector2.one * DisplaySettings.PinSize + Vector2.one * DisplaySettings.HighlightPadding).WithZ(1);
		}

		public void PlacePin(Vector3 position)
		{
			if (CurrentPlacementState == PlacementState.PlacingFirstPin)
			{
				CurrentPlacementState = PlacementState.PlacingWire;
				wire = CreateWire();
				wire.AddAnchorPoint(position);
				pinB = CreatePin(position, false);
			}
			else if (CurrentPlacementState == PlacementState.PlacingWire)
			{
				CurrentPlacementState = PlacementState.Finished;
				wire.AddAnchorPoint(position);
				wire.ConnectWireToPins(pinA, pinB);
				SetPins(new Pin[] { pinA }, new Pin[] { pinB });
			}
		}

		public void UpdateWirePlacementPreview(Vector3 position)
		{
			wire.DrawToPoint(position);
			pinB.transform.position = position;
			highlight.transform.position = position.WithZ(highlight.transform.position.z);
		}

		public void AddWirePoint(Vector3 position)
		{
			wire.AddAnchorPoint(position);
		}

		public override ChipInstanceData GetInstanceData()
		{
			return new ChipInstanceData()
			{
				Name = Name,
				ID = ID
			};
		}

		public override void SetHighlightState(bool isHighlighted)
		{
			highlight.gameObject.SetActive(isHighlighted);
		}

		public override Bounds GetBounds()
		{
			if (CurrentPlacementState == PlacementState.PlacingFirstPin)
			{
				return new Bounds(pinA.transform.position, pinA.transform.localScale);
			}
			else if (CurrentPlacementState == PlacementState.PlacingWire)
			{
				return new Bounds(pinB.transform.position, pinA.transform.localScale);
			}
			return new Bounds(Vector3.zero, Vector3.zero);
		}

		Pin CreatePin(Vector3 position, bool firstPin)
		{
			Pin pin = Instantiate(standalonePinPrefab, position, Quaternion.identity, parent: transform);
			pin.transform.localScale = Vector3.one * DisplaySettings.PinSize;
			PinDescription pinDescription = new PinDescription()
			{
				Name = "Pin",
				ID = firstPin ? 0 : 1
			};
			pin.SetUp(this, pinDescription, firstPin ? PinType.SubChipInputPin : PinType.SubChipOutputPin, palette.GetDefaultColours());
			return pin;
		}

		Wire CreateWire()
		{
			return Instantiate(wirePrefab, transform.position, Quaternion.identity, parent: transform);
		}
	}
}