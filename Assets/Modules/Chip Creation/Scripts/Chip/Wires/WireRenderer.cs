using System.Collections.Generic;
using UnityEngine;
using SebUtils;

namespace DLS.ChipCreation
{
	[RequireComponent(typeof(LineRenderer))]
	public class WireRenderer : MonoBehaviour
	{

		LineRenderer lineRenderer;
		List<Vector2> drawPoints;
		Material material;

		bool isInitialized;


		bool animatingColour;
		Color prevCol;
		Color targetColour;
		float colourAnimateDuration;
		float colourAnimateT;


		void Init()
		{
			if (!isInitialized)
			{
				isInitialized = true;

				lineRenderer ??= GetComponent<LineRenderer>();
				drawPoints ??= new List<Vector2>();

				material = Material.Instantiate(lineRenderer.sharedMaterial);
				lineRenderer.sharedMaterial = material;
			}
		}

		void Update()
		{
			if (animatingColour)
			{
				colourAnimateT += Time.deltaTime / colourAnimateDuration;
				material.color = Color.Lerp(prevCol, targetColour, colourAnimateT);
				if (colourAnimateT >= 1)
				{
					animatingColour = false;
				}
			}
		}

		public void SetThickness(float width)
		{
			Init();
			lineRenderer.startWidth = width;
			lineRenderer.endWidth = width;
		}

		public void SetColour(Color col, float fadeDuration = 0)
		{
			Init();
			prevCol = material.color;
			if (fadeDuration > 0)
			{
				animatingColour = true;
				targetColour = col;
				colourAnimateDuration = fadeDuration;
				colourAnimateT = 0;
			}
			else
			{
				animatingColour = false;
				material.color = col;
			}
		}


		public void SetAnchorPoints(Vector2[] anchorPoints, float curveSize, int resolution, bool useWorldSpace = false)
		{
			Init();
			drawPoints.Clear();
			drawPoints.Add(anchorPoints[0]);

			for (int i = 1; i < anchorPoints.Length - 1; i++)
			{
				Vector2 targetPoint = anchorPoints[i];
				Vector2 targetDir = (anchorPoints[i] - anchorPoints[i - 1]).normalized;
				float dstToTarget = (anchorPoints[i] - anchorPoints[i - 1]).magnitude;
				float dstToCurveStart = Mathf.Max(dstToTarget - curveSize, dstToTarget / 2);

				Vector2 nextTarget = anchorPoints[i + 1];
				Vector2 nextTargetDir = (anchorPoints[i + 1] - anchorPoints[i]).normalized;
				float nextLineLength = (anchorPoints[i + 1] - anchorPoints[i]).magnitude;

				Vector2 curveStartPoint = anchorPoints[i - 1] + targetDir * dstToCurveStart;
				Vector2 curveEndPoint = targetPoint + nextTargetDir * Mathf.Min(curveSize, nextLineLength / 2);

				// Bezier
				for (int j = 0; j < resolution; j++)
				{
					float t = j / (resolution - 1f);
					Vector2 a = Vector2.Lerp(curveStartPoint, targetPoint, t);
					Vector2 b = Vector2.Lerp(targetPoint, curveEndPoint, t);
					Vector2 p = Vector2.Lerp(a, b, t);

					if ((p - (Vector2)drawPoints[drawPoints.Count - 1]).sqrMagnitude > 0.001f)
					{
						drawPoints.Add(p);
					}
				}
			}
			drawPoints.Add(anchorPoints[anchorPoints.Length - 1]);

			lineRenderer.positionCount = drawPoints.Count;

			lineRenderer.SetPositions(VectorHelper.Vector2sToVector3s(drawPoints));
			lineRenderer.useWorldSpace = useWorldSpace;
		}

		public Vector2 ClosestPointOnWire(Vector2 p)
		{
			return Maths.ClosestPointOnPath(p, drawPoints);
		}
	}

}