using DLS.Graphics;
using Seb.Vis;
using Seb.Vis.UI;
using UnityEngine;

namespace DLS.Dev.VidTools
{
	public class VidName : MonoBehaviour
	{
		public Vector2 size;
		public float pad;
		public Color bgCol;
		public float fontSize;
		readonly UIHandle id = new("In");

		void Start()
		{
			UI.GetInputFieldState(id).ClearText();
		}

		// Update is called once per frame
		void Update()
		{
			UI.CreateFixedAspectUIScope();
			InputFieldTheme t = DrawSettings.ActiveUITheme.ChipNameInputField;
			t.focusBorderCol = Color.clear;
			t.fontSize = fontSize;
			t.bgCol = Color.clear;
			UI.InputField(id, t, UI.Centre, size, "", Anchor.Centre, pad, forceFocus: true);
		}
	}
}