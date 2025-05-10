using Seb.Helpers;
using Seb.Helpers.InputHandling;
using UnityEngine;

namespace DLS.Dev.VidTools
{
	public class VidInputController : MonoBehaviour
	{
		public enum Mode
		{
			None,
			Record,
			Playback
		}

		public Mode mode;
		public InputRecorder recorder;
		public InputPlayback playback;
		readonly UnityInputSource unityInputSource = new();
		bool waitingForMarkerResumeInput;

		void Start()
		{
			if (mode == Mode.Record)
			{
				Debug.Log("Recording - Start/Stop: Ctrl + Shift + R");
				Debug.Log("Recording - Set Marker: Ctrl + 1");
				Debug.Log("Recording - Restore From Marker: Ctrl + Alt + Shift + 1");
			}
			else if (mode == Mode.Playback)
			{
				Debug.Log("Playback - Start/Stop: Ctrl + Shift + P");
			}
			else if (mode == Mode.None)
			{
				gameObject.SetActive(false);
			}
		}


		void Update()
		{
			if (mode == Mode.Record) HandleRecording();
			else if (mode == Mode.Playback) HandlePlayback();
		}

		void HandleRecording()
		{
			// Start/stop
			if (InputHelper.CtrlIsHeld && InputHelper.ShiftIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.R))
			{
				if (recorder.IsRecording) recorder.StopRecording();
				else recorder.StartRecording();

				waitingForMarkerResumeInput = false;
			}

			// Set marker at current frame (basically a savepoint that can be returned to)
			if (InputHelper.CtrlIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.Alpha1) && !(InputHelper.AltIsHeld || InputHelper.ShiftIsHeld))
			{
				recorder.SetMarker();
				Debug.Log("Marker Set");
			}

			// Return to last marker. This will display the mouse position at the marked frame, return the recording to that frame, and pause the recording
			if (InputHelper.CtrlIsHeld && InputHelper.ShiftIsHeld && InputHelper.AltIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.Alpha1))
			{
				waitingForMarkerResumeInput = true;
				recorder.Pause();
				Vector2 mousePos = recorder.RestoreFromMarker();
				playback.WorldMousePosToDraw = InputHelper.WorldCam.ScreenToWorldPoint(mousePos);
				playback.BeginDrawingMousePos();
				Debug.Log("Marker Restored and Recording Paused. Press Alt to Resume Recording");
			}

			if (waitingForMarkerResumeInput && InputHelper.IsKeyDownThisFrame(KeyCode.LeftAlt))
			{
				waitingForMarkerResumeInput = false;
				playback.StopDrawingMouse();
				recorder.Resume();
			}
		}

		void HandlePlayback()
		{
			IInputSource inputSrcOld = InputHelper.InputSource;
			InputHelper.InputSource = unityInputSource;

			bool togglePlayback = InputHelper.CtrlIsHeld && InputHelper.ShiftIsHeld && InputHelper.IsKeyDownThisFrame(KeyCode.P);
			InputHelper.InputSource = inputSrcOld; // restore old input source in case is playing back recording

			// Start/stop
			if (togglePlayback)
			{
				if (playback.IsPlayingBack) playback.StopPlayback();
				else playback.StartPlayback();
			}
		}
	}
}