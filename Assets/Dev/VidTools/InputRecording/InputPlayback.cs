using System.Collections.Generic;
using Newtonsoft.Json;
using Seb.Helpers;
using Seb.Helpers.InputHandling;
using Seb.Vis;
using Seb.Vis.Text.Rendering.Helpers;
using UnityEngine;
using UnityEngine.Rendering;

namespace DLS.Dev.VidTools
{
	public class InputPlayback : MonoBehaviour
	{
		public TextAsset fileToLoad;
		public bool pause;
		public float playbackTimeScale = 1;
		public float scale = 1;
		public Vector2 offsetT;
		public Material mat;
		public float mouseSmoothTimeMax;
		public float mouseSmoothClickTimeFade;
		public float currMouseSmoothTime;
		public Vector2 mouseUnormTweak;
		readonly List<float> clickTimes = new();
		Mesh quadMesh;

		RecordedInputSource recordedInput;

		public bool IsPlayingBack { get; private set; }
		public Vector2 WorldMousePosToDraw { get; set; }


		void Update()
		{
			if (IsPlayingBack && !pause)
			{
				currMouseSmoothTime = CalculateMouseSmoothTime();
				recordedInput.mouseSmoothTime = currMouseSmoothTime;
				recordedInput.UpdatePlayback(Time.deltaTime * playbackTimeScale);
				WorldMousePosToDraw = InputHelper.MousePosWorld;

				if (recordedInput.PlaybackComplete)
				{
					StopPlayback();
				}
			}
		}

		public void StartPlayback()
		{
			Debug.Log("Playing back input recording");
			IsPlayingBack = true;

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Converters.Add(new Vector2Converter());

			string json = fileToLoad.text;
			RecordedInputSource.InputFrame[] recording = JsonConvert.DeserializeObject<RecordedInputSource.InputFrame[]>(json, settings);
			for (int i = 0; i < recording.Length; i++)
			{
				recording[i].MousePosUNorm += mouseUnormTweak;
			}

			recordedInput = new RecordedInputSource(recording);
			InputHelper.InputSource = recordedInput;

			CreateClickTimeMap(recording);

			BeginDrawingMousePos();
		}

		public void StopPlayback()
		{
			Debug.Log("Playback Stopped");
			IsPlayingBack = false;
			StopDrawingMouse();
			InputHelper.InputSource = new UnityInputSource();
		}

		public void BeginDrawingMousePos()
		{
			quadMesh = QuadMeshGenerator.GenerateQuadMesh();

			Draw.OnDraw -= OnDraw;
			Draw.OnDraw += OnDraw;
		}

		public void StopDrawingMouse()
		{
			Draw.OnDraw -= OnDraw;
		}

		void OnDraw(CommandBuffer cmd)
		{
			Vector2 size = Vector2.one * scale * InputHelper.WorldCam.orthographicSize / Screen.width;
			Matrix4x4 matrix = Matrix4x4.TRS(WorldMousePosToDraw + new Vector2(size.x * offsetT.x, size.y * offsetT.y), Quaternion.identity, size);
			cmd.DrawMesh(quadMesh, matrix, mat);
		}

		float CalculateMouseSmoothTime()
		{
			float t = recordedInput.playbackTime;
			float minDst = float.MaxValue;

			foreach (float clickTime in clickTimes)
			{
				minDst = Mathf.Min(minDst, Mathf.Abs(t - clickTime));
			}

			float fadeT = Mathf.Clamp01(minDst / mouseSmoothClickTimeFade);

			return mouseSmoothTimeMax * fadeT;
		}

		void CreateClickTimeMap(RecordedInputSource.InputFrame[] recording)
		{
			bool mouse0Old = false;
			bool mouse1Old = false;
			foreach (RecordedInputSource.InputFrame f in recording)
			{
				bool hasMouse0 = false;
				bool hasMouse1 = false;
				foreach (KeyCode k in f.HeldKeys)
				{
					if (k == KeyCode.Mouse0)
					{
						hasMouse0 = true;
						if (!mouse0Old)
						{
							mouse0Old = true;
							clickTimes.Add(f.Time);
						}
					}

					if (k == KeyCode.Mouse1)
					{
						hasMouse1 = true;
						if (!mouse1Old)
						{
							mouse1Old = true;
							clickTimes.Add(f.Time);
						}
					}
				}

				if (!hasMouse0 && mouse0Old)
				{
					mouse0Old = false;
					clickTimes.Add(f.Time);
				}

				if (!hasMouse1 && mouse1Old)
				{
					mouse1Old = false;
					clickTimes.Add(f.Time);
				}
			}
		}
	}
}