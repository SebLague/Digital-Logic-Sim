using DLS.Game;
using Seb.Helpers;
using UnityEngine;

namespace DLS.Dev.VidTools
{
	public class VidTools : MonoBehaviour
	{
		public bool camOverrideAtStartupOnly;
		public bool camOrthoOverride;
		public float camOrtho;
		public bool camPosOverride;
		public Vector2 camPos;
		public bool printCamView;
		bool clearingPinNames;
		SubChipInstance labelTarget;
		int n;

		DevPinInstance nameTarget;

		void Update()
		{
			if (Time.frameCount > 10 && camOverrideAtStartupOnly)
			{
				camOrthoOverride = false;
				camPosOverride = false;
			}

			if (printCamView)
			{
				Debug.Log(CameraController.activeView.Pos.x + "  " + CameraController.activeView.Pos.y + "  ortho: " + CameraController.activeView.OrthoSize);
				printCamView = false;
			}

			if (camOrthoOverride)
			{
				//Debug.Log("Setting cam (vidtools)");
				CameraController.activeView.OrthoSize = camOrtho;
			}

			if (camPosOverride)
			{
				//Debug.Log("Setting cam (vidtools)");
				CameraController.activeView.Pos = camPos;
			}


			if (InputHelper.IsKeyDownThisFrame(KeyCode.V) && InputHelper.AltIsHeld)
			{
				Debug.Log("Clearing pin names (vidtools)");
				clearingPinNames = true;
			}

			if (InputHelper.IsKeyDownThisFrame(KeyCode.B) && InputHelper.AltIsHeld)
			{
				Debug.Log("Resetting index (vidtools)");
				n = 0;
			}

			if (clearingPinNames)
			{
				foreach (IMoveable x in Project.ActiveProject.ViewedChip.Elements)
				{
					if (x is DevPinInstance p)
					{
						p.Pin.Name = string.Empty;
					}
				}
			}

			if (InputHelper.IsKeyDownThisFrame(KeyCode.R) && InputHelper.AltIsHeld)
			{
				clearingPinNames = false;
				n++;
				if (Project.ActiveProject.ViewedChip.Elements[^n] is DevPinInstance devPin)
				{
					Debug.Log("Starting pin name (vidtools)");
					nameTarget = devPin;
					nameTarget.Pin.Name = string.Empty;
				}

				if (Project.ActiveProject.ViewedChip.Elements[^n] is SubChipInstance subchip)
				{
					Debug.Log("Starting chip label (vidtools)");
					labelTarget = subchip;
				}
			}

			if (nameTarget != null)
			{
				if (InputHelper.IsKeyDownThisFrame(KeyCode.Return))
				{
					nameTarget = null;
				}
				else nameTarget.Pin.Name += InputHelper.InputStringThisFrame;
			}

			if (labelTarget != null)
			{
				if (InputHelper.IsKeyDownThisFrame(KeyCode.Return))
				{
					labelTarget = null;
				}
				else labelTarget.Label += InputHelper.InputStringThisFrame;
			}
		}
	}
}