using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DLS.VideoTools
{
	public class InputRecorder : MonoBehaviour
	{
		public enum Mode { Off, Recording, Playback }
		public TextAsset recordingFile;
		public string savePath;
		Mode mode = Mode.Off;

		InputEventTrace trace;
		InputEventTrace.ReplayController replayController;
		public RectTransform softwareMouse;
		Mouse playbackMouse;
		VidActions vidActions;
		static InputRecorder instance;

		public static bool IsInPlayback => instance.mode == Mode.Playback;

		void Awake()
		{
			instance = this;
		}

		void Start()
		{
			vidActions = new VidActions();
			vidActions.Enable();
		}

		void Update()
		{
			if (mode == Mode.Playback)
			{
				Playback();
			}
			HandleInput();
		}

		void HandleInput()
		{
			if (vidActions.Map.Cancel.WasPerformedThisFrame())
			{
				Cancel();
			}

			if (mode != Mode.Playback)
			{
				if (vidActions.Map.ToggleRecording.WasPerformedThisFrame())
				{
					ToggleRecording();
				}
				else if (vidActions.Map.StartPlayback.WasPerformedThisFrame())
				{
					StartPlayback();
				}
				else if (vidActions.Map.Load.WasPerformedThisFrame())
				{
					LoadRecording();
				}
				else if (vidActions.Map.Save.WasPerformedThisFrame())
				{
					SaveRecording();
				}
			}
		}

		void Cancel()
		{
			if (mode == Mode.Playback)
			{
				StopPlayback();
			}
			else if (mode == Mode.Recording)
			{
				StopRecording();
			}
		}

		void ToggleRecording()
		{
			if (mode is Mode.Recording)
			{
				StopRecording();
			}
			else
			{
				StartRecording();
			}
		}


		void StartRecording()
		{
			mode = Mode.Recording;
			trace?.Dispose();
			trace = new InputEventTrace();
			trace.Enable();
			Debug.Log("Recording Started");
		}

		void StopRecording()
		{
			mode = Mode.Off;
			trace.Disable();
			Debug.Log("Recording Stopped");
		}

		void StopPlayback()
		{
			mode = Mode.Off;
			softwareMouse.gameObject.SetActive(false);
			replayController.paused = true;
			Debug.Log("Playback Stopped");
		}

		void StartPlayback()
		{
			Debug.Log("Playback Started");

			mode = Mode.Playback;
			if (replayController is not null)
			{
				replayController.Dispose();
			}
			replayController = trace.Replay();
			replayController.paused = false;
			replayController.Rewind();
			replayController.PlayAllFramesOneByOne();
			replayController.PlayAllEventsAccordingToTimestamps();
			softwareMouse.gameObject.SetActive(true);

			foreach (var x in replayController.trace.deviceInfos)
			{
				var m = InputSystem.GetDeviceById(x.deviceId);
				if (m is Mouse)
				{
					playbackMouse = m as Mouse;
				}
			}
		}

		void SaveRecording()
		{
			string fileName = "InputRec_" + (new System.Random().Next()).GetHashCode().ToString().Substring(0, 5) + ".bytes";
			string filePath = System.IO.Path.Combine(savePath, fileName);

			Debug.Log("Recording saved to: " + filePath);
			trace.WriteTo(filePath);
		}

		void LoadRecording()
		{
			Debug.Log("Recording loaded from: " + recordingFile.name);
			var stream = new System.IO.MemoryStream(recordingFile.bytes);
			trace = InputEventTrace.LoadFrom(stream);
			stream.Dispose();
		}


		void Playback()
		{
			Vector2 targetPos = playbackMouse.position.ReadValue();
			softwareMouse.position = targetPos;
			if (replayController.finished)
			{
				StopPlayback();
			}
		}

		void OnDestroy()
		{
			if (trace is not null)
			{
				trace.Dispose();
			}
			if (replayController is not null)
			{
				replayController.Dispose();
			}
		}
	}
}