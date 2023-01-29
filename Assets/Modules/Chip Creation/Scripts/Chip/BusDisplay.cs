using System.Collections;
using System.Collections.Generic;
using DLS.ChipData;
using UnityEngine;
using System.Linq;

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

		public Wire Wire { get; private set; }
		Pin pinA;
		Pin pinB;

		public override void Load(ChipDescription description, ChipInstanceData instanceData)
		{
			//base.Load(description, instanceData);
			transform.position = ToVector(instanceData.Points[0]);
			Vector3[] points = instanceData.Points.Select(p => ToVector(p).WithZ(RenderOrder.ChipMoving)).Reverse().ToArray();
			StartPlacing(description, instanceData.ID);
			pinA.transform.position = points[0];
			PlacePin();
			for (int i = 1; i < points.Length - 1; i++)
			{
				Wire.AddAnchorPoint(points[i]);
			}
			pinB.transform.position = points[^1];
			PlacePin();
			FinishPlacing();
		}

		public override void StartPlacing(ChipDescription description, int id)
		{
			base.StartPlacing(description, id);
			pinA = CreatePin(transform.position, true);
			CurrentPlacementState = PlacementState.PlacingFirstPin;
			highlight.transform.localScale = (Vector2.one * DisplaySettings.PinSize).WithZ(1);
		}

		public void PlacePin()
		{
			if (CurrentPlacementState == PlacementState.PlacingFirstPin)
			{

				CurrentPlacementState = PlacementState.PlacingWire;
				Wire = CreateWire();
				Wire.AddAnchorPoint(pinA.transform.position);
				pinB = CreatePin(pinA.transform.position, false);
			}
			else if (CurrentPlacementState == PlacementState.PlacingWire)
			{
				CurrentPlacementState = PlacementState.Finished;
				Wire.AddAnchorPoint(pinB.transform.position);
				Wire.ConnectWireToPins(pinA, pinB);
				Wire.WireDeleted += (w) => Delete();
				SetPins(new Pin[] { pinB }, new Pin[] { pinA });
			}
		}

		public void UpdateWirePlacementPreview(Vector3 position)
		{
			Wire.DrawToPoint(position.WithZ(RenderOrder.WireEdit));
			pinB.transform.position = position.WithZ(RenderOrder.ChipMoving);
			highlight.transform.position = position.WithZ(highlight.transform.position.z);
		}

		public void UpdatePrevBusPoint(Vector2 p)
		{
			int index = Wire.AnchorPoints.Count - 1;
			Wire.UpdateAnchorPoint(index, p);
			if (index == 0)
			{
				pinA.transform.position = p.WithZ(RenderOrder.ChipMoving);
			}
		}

		public void AddWireAnchor()
		{
			Wire.AddAnchorPoint(pinB.transform.position);
		}

		public override ChipInstanceData GetInstanceData()
		{

			return new ChipInstanceData()
			{
				Name = Name,
				ID = ID,
				Points = Wire.AnchorPoints.Select(p => ToPoint(p)).ToArray()
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
				return new Bounds(pinA.transform.position, Vector3.one * 0.1f);
			}
			else if (CurrentPlacementState == PlacementState.PlacingWire)
			{
				return new Bounds(pinB.transform.position, Vector3.one * 0.1f);
			}
			return new Bounds(Vector3.zero, Vector3.zero);
		}

		public override void FinishPlacing()
		{
			base.FinishPlacing();
			Wire.transform.position = Wire.transform.position.WithZ(RenderOrder.WireLow);
			pinA.transform.position = pinA.transform.position.WithZ(RenderOrder.ChipPin);
			pinB.transform.position = pinB.transform.position.WithZ(RenderOrder.ChipPin);
		}

		Pin CreatePin(Vector3 position, bool inputPin)
		{
			Pin pin = Instantiate(standalonePinPrefab, position, Quaternion.identity, parent: transform);
			pin.IsBusPin = true;
			pin.transform.localScale = Vector3.one * DisplaySettings.PinSize;
			PinDescription pinDescription = new PinDescription()
			{
				Name = "Pin",
				ID = inputPin ? 0 : 1
			};
			pin.SetUp(this, pinDescription, inputPin ? PinType.SubChipInputPin : PinType.SubChipOutputPin, palette.GetDefaultColours());
			return pin;
		}

		Wire CreateWire()
		{
			return Instantiate(wirePrefab, transform.position, Quaternion.identity, parent: transform);
		}
	}
}