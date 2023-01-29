using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using SebInput;

namespace DLS.ChipCreation
{
	public class Wire : MonoBehaviour
	{
		public event System.Action<Wire> WireDeleted;

		public Pin SourcePin { get; private set; }
		public Pin TargetPin { get; private set; }
		public bool IsConnected { get; private set; }
		public ReadOnlyCollection<Vector2> AnchorPoints => new(anchorPoints);
		public MouseInteraction<Wire> MouseInteraction { get; private set; }
		public bool IsBusWire { get; private set; }
		public Vector2 CurrentDrawToPoint { get; private set; }

		[SerializeField] WireRenderer wireRenderer;
		[SerializeField] EdgeCollider2D edgeCollider;
		[SerializeField] float wireThickness;
		[SerializeField] float selectedThicknessPadding;
		[SerializeField] float wireCurveAmount;
		[SerializeField] int wireCurveResolution;
		[SerializeField] MeshRenderer busConnectionDot;

		List<Vector2> anchorPoints;
		Vector3[] drawPoints;
		bool isDeleted;

		public Palette.VoltageColour ColourTheme { get; private set; }


		void Awake()
		{
			wireRenderer.SetThickness(GetThickness(false));
			transform.position = new Vector3(0, 0, RenderOrder.WireEdit);
			edgeCollider.enabled = false;
			MouseInteraction = new MouseInteraction<Wire>(gameObject, this);

			anchorPoints = new List<Vector2>();
			drawPoints = new Vector3[0];
			SetColour(Color.black);
		}

		public void ConnectWireToPins(Pin pinA, Pin pinB)
		{
			SourcePin = (pinA.IsSourcePin) ? pinA : pinB;
			TargetPin = (pinA.IsTargetPin) ? pinA : pinB;
			IsBusWire = pinA.IsBusPin && pinB.IsBusPin;
			IsConnected = true;

			if (SourcePin != pinA)
			{
				anchorPoints.Reverse();
			}

			// Delete wire if either pin is deleted
			pinA.PinDeleted += (pin) => DeleteWire();
			pinB.PinDeleted += (pin) => DeleteWire();
			// Update wire position if pins are moved
			pinA.PinMoved += OnPinMove;
			pinB.PinMoved += OnPinMove;

			EnableCollisions();

			// Update renderer
			UpdateLineRenderer();
			wireRenderer.SetThickness(GetThickness(false));
			SetColourTheme(SourcePin.ColourTheme);

			SourcePin.ColourThemeChanged += SetColourTheme;
			TargetPin.ColourThemeChanged += SetColourTheme;

			// If connecting to a bus line, then display a small dot at the connection point
			if ((SourcePin.IsBusPin || TargetPin.IsBusPin) && !IsBusWire)
			{
				busConnectionDot.sharedMaterial = new Material(busConnectionDot.sharedMaterial);
				Vector2 busConnectionPoint = SourcePin.IsBusPin ? anchorPoints[0] : anchorPoints[^1];
				busConnectionDot.gameObject.SetActive(true);
				busConnectionDot.transform.position = busConnectionPoint.WithZ(RenderOrder.busConnectionDot);
				busConnectionDot.transform.localScale = Vector3.one * DisplaySettings.PinSize * 0.6f;
			}
		}

		public void DeleteWire()
		{
			if (!isDeleted)
			{
				isDeleted = true;
				WireDeleted?.Invoke(this);
				if (SourcePin != null)
				{
					SourcePin.ColourThemeChanged -= SetColourTheme;
				}
				if (TargetPin != null)
				{
					TargetPin.ColourThemeChanged -= SetColourTheme;
				}
				Destroy(gameObject);
			}
		}

		public void UpdateDisplayState()
		{
			var col = ColourTheme.GetColour(SourcePin.State);
			float z;

			z = (SourcePin.State == Simulation.PinState.HIGH) ? RenderOrder.WireHigh : RenderOrder.WireLow;
			if (IsBusWire)
			{
				z = (SourcePin.State == Simulation.PinState.HIGH) ? RenderOrder.BusWireHigh : RenderOrder.BusWireLow;
			}

			z += RenderOrder.layerAbove / 10f * ColourTheme.displayPriority;
			transform.position = new Vector3(0, 0, z);
			SetColour(col);
		}

		public void DrawToPoint(Vector2 targetPoint)
		{
			// Only draw wire to target point if an anchor point exists, and target point is not on top of last anchor point
			if (anchorPoints.Count > 0 && (anchorPoints[^1] - targetPoint).sqrMagnitude > 0.001f)
			{
				CurrentDrawToPoint = targetPoint;
				List<Vector2> points = new List<Vector2>(anchorPoints);
				points.Add(targetPoint);
				UpdateLineRenderer(points.ToArray());
			}
		}

		public void SetAnchorPoints(IList<Vector2> points, bool updateGraphics)
		{
			anchorPoints = new List<Vector2>(points);
			if (updateGraphics)
			{
				UpdateLineRenderer();
			}
		}


		public void AddAnchorPoint(Vector2 point)
		{
			// Don't add point if too close to previous anchor point
			if (anchorPoints.Count == 0 || (anchorPoints[^1] - point).sqrMagnitude > 0.01f)
			{
				anchorPoints.Add(point);
			}
		}

		public void UpdateAnchorPoint(int i, Vector2 point)
		{
			anchorPoints[i] = point;
			UpdateLineRenderer();
		}

		public void RemoveLastAnchorPoint()
		{
			if (anchorPoints.Count > 1)
			{
				anchorPoints.RemoveAt(anchorPoints.Count - 1);
			}
		}

		public void SetHighlightState(bool highlighted)
		{
			wireRenderer.SetThickness(GetThickness(highlighted));
		}

		void OnPinMove(Pin pin)
		{
			UpdatePosition();
		}

		void UpdatePosition()
		{
			Vector2 deltaA = (Vector2)SourcePin.transform.position - anchorPoints[0];
			Vector2 deltaB = (Vector2)TargetPin.transform.position - anchorPoints[^1];
			bool moveA = deltaA.magnitude > 0.001f && !SourcePin.IsBusPin;
			bool moveB = deltaB.magnitude > 0.001f && !TargetPin.IsBusPin;

			if (moveA && moveB)
			{
				for (int i = 0; i < anchorPoints.Count; i++)
				{
					anchorPoints[i] += deltaA;
				}
			}
			else if (moveA)
			{
				anchorPoints[0] += deltaA;
			}
			else if (moveB)
			{
				anchorPoints[^1] += deltaB;
			}

			if (moveA || moveB)
			{
				UpdateLineRenderer();
				UpdateCollider();
			}
		}

		public void SetColourTheme(Palette.VoltageColour colours)
		{
			this.ColourTheme = colours;
			UpdateDisplayState();
		}

		void SetColour(Color col, float fadeDuration = 0)
		{
			busConnectionDot.sharedMaterial.color = col;
			wireRenderer.SetColour(col, fadeDuration);
		}

		public void UpdateLineRenderer()
		{
			UpdateLineRenderer(anchorPoints.ToArray());
		}

		void UpdateLineRenderer(Vector2[] points)
		{
			wireRenderer.SetAnchorPoints(points, wireCurveAmount, wireCurveResolution);
		}

		public void EnableCollisions()
		{
			UpdateCollider();
			edgeCollider.edgeRadius = GetThickness(true);
			edgeCollider.enabled = true;
		}

		public Vector2 ClosestPoint(Vector2 p) => wireRenderer.ClosestPointOnWire(p);

		void UpdateCollider()
		{
			edgeCollider.points = anchorPoints.ToArray();
		}

		float GetThickness(bool isSelected)
		{
			return wireThickness + (isSelected ? selectedThicknessPadding : 0);
		}

		void OnDestroy()
		{
			if (SourcePin is not null)
			{
				SourcePin.PinMoved -= OnPinMove;
				TargetPin.PinMoved -= OnPinMove;
			}
		}
	}
}