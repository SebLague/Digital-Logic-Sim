using System.Linq;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;
using Random = System.Random;

[ExecuteAlways]
public class MyVisTest : MonoBehaviour
{
	public enum DrawMode
	{
		None,
		SceneObjects,
		LotsOfPoints
	}

	public Vector2 offsetTest;
	public float scaleTest = 1;

	public DrawMode mode;

	public int pointTestCount;

	public Anchor buttonAnchor;
	public Vector2 buttonPos;
	public Vector2 buttonSize;

	public Anchor textAnchor;
	public Vector2 textPos;
	public float textSize;
	public string text;

	public Vector2 inputFieldPos;
	public Vector2 inputFieldSize;

	public Vector2 sliderPos;
	public Vector2 sliderSize;

	public float verticalGroupStart;
	public float verticalGroupSpacing;
	public int vGroupNum;
	public float horizontalGroupStart;
	public float horizontalGroupSpacing;
	public int hGroupNum;
	InputFieldState inputFieldState;
	SliderState sliderState;
	float sliderT;

	// Start is called before the first frame update
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		if (mode == DrawMode.SceneObjects)
		{
			DrawSceneObjects();
		}
		else if (mode == DrawMode.LotsOfPoints)
		{
			DrawLotsOfPoints();
		}

		DrawUI();
	}


	void DrawUI()
	{
		//return;
		//ui.Begin(Vector2.zero, new Vector2(Screen.width, Screen.height));
		using (UI.CreateFixedAspectUIScope())
		{
			//  UI.DrawInputField(inputFieldPos, inputFieldSize, Anchor.Centre, ref inputFieldState);
			UI.DrawSlider(sliderPos, sliderSize, Anchor.BottomLeft, ref sliderState);

			/*
			using (UI.CreateLayoutScope(UI.LayoutScope.Kind.Vertical, verticalGroupStart, verticalGroupSpacing))
			{
			    for (int i = 0; i < vGroupNum; i++)
			    {
			        using (UI.CreateLayoutScope(UI.LayoutScope.Kind.Horizontal, horizontalGroupStart, horizontalGroupSpacing))
			        {
			            for (int j = 0; j < hGroupNum; j++)
			            {
			                if (UI.DrawButton(text, UI.TestButtonTheme, buttonPos, buttonSize, true, buttonAnchor))
			                {
			                    Debug.Log("Pressed");
			                }
			            }
			        }
			    }
			}
			*/
			//ui.DrawText(text, textSize, textPos, textAnchor);
		}
	}

	void DrawLotsOfPoints()
	{
		Random rng = new(5);

		for (int i = 0; i < pointTestCount; i++)
		{
			float x = (float)(rng.NextDouble() - 0.5) * 17;
			float y = (float)(rng.NextDouble() - 0.5) * 10;
			Vector2 pos = new(x, y);
			float rad = (float)rng.NextDouble() * 0.3f;
			Color col = i % 2 == 0 ? Color.red : Color.blue;
			Draw.Point(pos, rad, col);
		}
	}

	void DrawSceneObjects()
	{
		Draw.StartLayer(Vector2.zero, 1, true);
		// Seb.Vis.Draw.BeginTransformState(Vector2.zero, 1, useScreenSpace:true);
		Vector2 screenSize = new(Screen.width, Screen.height);
		Draw.Quad(screenSize / 2, screenSize - Vector2.one * 10, new Color(0.1f, 0.1f, 0.1f, 1));
		// Draw.EndTransformState();

		Draw.StartLayer(offsetTest, scaleTest, false);
		//Seb.Vis.Draw.BeginTransformState(offsetTest, scaleTest);
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform t = transform.GetChild(i);

			if (t.TryGetComponent(out PointVisTest p))
			{
				// Seb.Vis.DrawManager.Point(p.transform.position, p.transform.localScale.x, p.Col);
				Draw.Ellipse(p.transform.position, p.transform.localScale, p.Col);
			}
			else if (t.TryGetComponent(out QuadVisTest q))
			{
				if (q.outline) Draw.QuadOutline(q.transform.position, q.transform.localScale, q.thickness, q.col);
				else Draw.Quad(q.transform.position, q.transform.localScale, q.col);
			}
			else if (t.TryGetComponent(out LineVisTest l))
			{
				Draw.Line(l.transform.position, l.transform.position + (Vector3)l.offset, l.thickness, l.col, l.t);
			}
			else if (t.TryGetComponent(out QuadraticBezier b))
			{
				b.Draw();
			}
			else if (t.TryGetComponent(out TextVisTest tv))
			{
				Draw.Text(tv.fontType, tv.text, tv.fontSize, tv.transform.position, tv.textAlign, tv.col);
			}
			else if (t.TryGetComponent(out PathVisTest path))
			{
				Draw.LinePath(path.GetComponentsInChildren<Transform>().Select(t => (Vector2)t.position).ToArray(), path.thickness, path.col, path.t);
			}
			else if (t.TryGetComponent(out ArrowVisTest a))
			{
				if (a.useHeadDefaults)
				{
					Draw.Arrow(a.transform.position, (Vector2)a.transform.position + a.offset, a.thickness, a.col, a.t);
				}
				else
				{
					Draw.Arrow(a.transform.position, (Vector2)a.transform.position + a.offset, a.thickness, a.headLength, a.headAngle, a.col, a.t);
				}
			}
			else if (t.TryGetComponent(out TriangleVisTest tri))
			{
				Vector2 c = tri.transform.position;
				Draw.Triangle(c + tri.offsetA, c + tri.offsetB, c + tri.offsetC, tri.col);
			}
		}

		// Seb.Vis.Draw.EndTransformState();
	}
}