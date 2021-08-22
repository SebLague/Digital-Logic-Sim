using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wire : MonoBehaviour {

	public Material simpleMat;
	LineRenderer lineRenderer;
	public Color editCol;
	public Palette palette;
	public Color placedCol;
	public float curveSize = 0.5f;
	public int resolution = 10;
	public float thickness = 1;
	public float selectedThickness = 1.2f;
	bool selected;

	bool wireConnected;
	[HideInInspector] public Pin startPin;
	[HideInInspector] public Pin endPin;
	EdgeCollider2D wireCollider;
	public List<Vector2> anchorPoints { get; private set; }
	List<Vector2> drawPoints;
	const float thicknessMultiplier = 0.1f;
	float length;
	Material mat;
	float depth;

	void Awake () {
		lineRenderer = GetComponent<LineRenderer> ();
	}

	void Start () {
		lineRenderer.material = simpleMat;
		mat = lineRenderer.material;
	}

	public Pin ChipInputPin {
		get {
			return (startPin.pinType == Pin.PinType.ChipInput) ? startPin : endPin;
		}
	}

	public Pin ChipOutputPin {
		get {
			return (startPin.pinType == Pin.PinType.ChipOutput) ? startPin : endPin;
		}
	}

	public void SetAnchorPoints (Vector2[] newAnchorPoints) {
		anchorPoints = new List<Vector2> (newAnchorPoints);
		UpdateSmoothedLine ();
		UpdateCollider ();
	}

	public void SetDepth (int numWires) {
		depth = numWires * 0.01f;
		transform.localPosition = Vector3.forward * depth;
	}

	void LateUpdate () {
		SetWireCol ();
		if (wireConnected) {
			float depthOffset = 5;

			transform.localPosition = Vector3.forward * (depth + depthOffset);
			UpdateWirePos ();
			//transform.position = new Vector3 (transform.position.x, transform.position.y, inputPin.sequentialState * -0.01f);

		}
		lineRenderer.startWidth = ((selected) ? selectedThickness : thickness) * thicknessMultiplier;
		lineRenderer.endWidth = ((selected) ? selectedThickness : thickness) * thicknessMultiplier;

	}

	void UpdateWirePos () {
		const float maxSqrError = 0.00001f;
		// How far are start and end points from the pins they're connected to (chip has been moved)
		Vector2 startPointError = (Vector2) startPin.transform.position - anchorPoints[0];
		Vector2 endPointError = (Vector2) endPin.transform.position - anchorPoints[anchorPoints.Count - 1];

		if (startPointError.sqrMagnitude > maxSqrError || endPointError.sqrMagnitude > maxSqrError) {
			// If start and end points are both same offset from where they should be, can move all anchor points (entire wire)
			if ((startPointError - endPointError).sqrMagnitude < maxSqrError && startPointError.sqrMagnitude > maxSqrError) {
				for (int i = 0; i < anchorPoints.Count; i++) {
					anchorPoints[i] += startPointError;
				}
			}

			anchorPoints[0] = startPin.transform.position;
			anchorPoints[anchorPoints.Count - 1] = endPin.transform.position;
			UpdateSmoothedLine ();
			UpdateCollider ();
		}
	}

	void SetWireCol () {
		//Fix color for bus wires
		if(startPin.wireType != Pin.WireType.Simple)
		{
			mat.color = palette.busColor;
			return;
		}

		if (wireConnected) {
			Color onCol = palette.onCol;
			Color offCol = palette.offCol;

			// High Z
			if (ChipOutputPin.State == -1) {
				onCol = palette.highZCol;
				offCol = palette.highZCol;
			}
			mat.color = (ChipOutputPin.State == 0) ? offCol : onCol;
		} else {
			mat.color = Color.black;
		}
	}

	public void Connect (Pin inputPin, Pin outputPin) {
		ConnectToFirstPin (inputPin);
		Place (outputPin);
	}

	public void ConnectToFirstPin (Pin startPin) {
		this.startPin = startPin;
		lineRenderer = GetComponent<LineRenderer> ();
		mat = simpleMat;
		drawPoints = new List<Vector2> ();

		transform.localPosition = new Vector3 (0, 0, transform.localPosition.z);

		wireCollider = GetComponent<EdgeCollider2D> ();

		anchorPoints = new List<Vector2> ();
		anchorPoints.Add (startPin.transform.position);
		anchorPoints.Add (startPin.transform.position);
		UpdateSmoothedLine ();
		mat.color = editCol;
	}

	public void ConnectToFirstPinViaWire (Pin startPin, Wire parentWire, Vector2 inputPoint) {
		lineRenderer = GetComponent<LineRenderer> ();
		mat = simpleMat;
		drawPoints = new List<Vector2> ();
		this.startPin = startPin;
		transform.localPosition = new Vector3 (0, 0, transform.localPosition.z);

		wireCollider = GetComponent<EdgeCollider2D> ();

		anchorPoints = new List<Vector2> ();

		// Find point on wire nearest to input point
		Vector2 closestPoint = Vector2.zero;
		float smallestDst = float.MaxValue;
		int closestI = 0;
		for (int i = 0; i < parentWire.anchorPoints.Count - 1; i++) {
			var a = parentWire.anchorPoints[i];
			var b = parentWire.anchorPoints[i + 1];
			var pointOnWire = MathUtility.ClosestPointOnLineSegment (a, b, inputPoint);
			float sqrDst = (pointOnWire - inputPoint).sqrMagnitude;
			if (sqrDst < smallestDst) {
				smallestDst = sqrDst;
				closestPoint = pointOnWire;
				closestI = i;
			}
		}

		for (int i = 0; i <= closestI; i++) {
			anchorPoints.Add (parentWire.anchorPoints[i]);
		}
		anchorPoints.Add (closestPoint);
		if (Input.GetKey (KeyCode.LeftAlt)) {
			anchorPoints.Add (closestPoint);
		}
		anchorPoints.Add (inputPoint);

		UpdateSmoothedLine ();
		mat.color = editCol;
	}

	// Connect the input pin to the output pin
	public void Place (Pin endPin) {
		this.endPin = endPin;
		anchorPoints[anchorPoints.Count - 1] = endPin.transform.position;
		UpdateSmoothedLine ();

		wireConnected = true;
		UpdateCollider ();
	}

	// Update position of wire end point (for when initially placing the wire)
	public void UpdateWireEndPoint (Vector2 endPointWorldSpace) {
		anchorPoints[anchorPoints.Count - 1] = ProcessPoint (endPointWorldSpace);
		UpdateSmoothedLine ();
	}

	// Add anchor point (for when initially placing the wire)
	public void AddAnchorPoint (Vector2 pointWorldSpace) {
		anchorPoints[anchorPoints.Count - 1] = ProcessPoint (pointWorldSpace);
		anchorPoints.Add (ProcessPoint (pointWorldSpace));
	}

	void UpdateCollider () {
		wireCollider.points = drawPoints.ToArray ();
		wireCollider.edgeRadius = thickness * thicknessMultiplier;
	}

	void UpdateSmoothedLine () {
		length = 0;
		GenerateDrawPoints ();

		lineRenderer.positionCount = drawPoints.Count;
		Vector2 lastLocalPos = Vector2.zero;
		for (int i = 0; i < lineRenderer.positionCount; i++) {
			Vector2 localPos = transform.parent.InverseTransformPoint (drawPoints[i]);
			lineRenderer.SetPosition (i, new Vector3 (localPos.x, localPos.y, -0.01f));

			if (i > 0) {
				length += (lastLocalPos - localPos).magnitude;
			}
			lastLocalPos = localPos;
		}
	}

	public void SetSelectionState (bool selected) {
		this.selected = selected;
	}

	Vector2 ProcessPoint (Vector2 endPointWorldSpace) {
		if (Input.GetKey (KeyCode.LeftShift)) {
			Vector2 a = anchorPoints[anchorPoints.Count - 2];
			Vector2 b = endPointWorldSpace;
			Vector2 mid = (a + b) / 2;

			bool xAxisLonger = (Mathf.Abs (a.x - b.x) > Mathf.Abs (a.y - b.y));
			if (xAxisLonger) {
				return new Vector2 (b.x, a.y);
			} else {
				return new Vector2 (a.x, b.y);
			}
		}
		return endPointWorldSpace;
	}

	void GenerateDrawPoints () {
		drawPoints.Clear ();
		drawPoints.Add (anchorPoints[0]);

		for (int i = 1; i < anchorPoints.Count - 1; i++) {
			Vector2 targetPoint = anchorPoints[i];
			Vector2 targetDir = (anchorPoints[i] - anchorPoints[i - 1]).normalized;
			float dstToTarget = (anchorPoints[i] - anchorPoints[i - 1]).magnitude;
			float dstToCurveStart = Mathf.Max (dstToTarget - curveSize, dstToTarget / 2);

			Vector2 nextTarget = anchorPoints[i + 1];
			Vector2 nextTargetDir = (anchorPoints[i + 1] - anchorPoints[i]).normalized;
			float nextLineLength = (anchorPoints[i + 1] - anchorPoints[i]).magnitude;

			Vector2 curveStartPoint = anchorPoints[i - 1] + targetDir * dstToCurveStart;
			Vector2 curveEndPoint = targetPoint + nextTargetDir * Mathf.Min (curveSize, nextLineLength / 2);

			// Bezier
			for (int j = 0; j < resolution; j++) {
				float t = j / (resolution - 1f);
				Vector2 a = Vector2.Lerp (curveStartPoint, targetPoint, t);
				Vector2 b = Vector2.Lerp (targetPoint, curveEndPoint, t);
				Vector2 p = Vector2.Lerp (a, b, t);

				if ((p - drawPoints[drawPoints.Count - 1]).sqrMagnitude > 0.001f) {
					drawPoints.Add (p);
				}
			}
		}
		drawPoints.Add (anchorPoints[anchorPoints.Count - 1]);
	}

}