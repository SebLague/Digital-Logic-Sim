using System.Collections.Generic;
using System.Text;
using Seb.Helpers.InputHandling;
using UnityEngine;

namespace DLS.Dev.VidTools
{
	public class RecordedInputSource : IInputSource
	{
		readonly Queue<InputFrame> frameQueue;
		readonly HashSet<KeyCode> heldKeysPrevFrame = new();
		readonly HashSet<KeyCode> heldKeysThisFrame = new();
		readonly StringBuilder stringBuilder = new();
		Vector2 MousePositionTarget;
		public float mouseSmoothTime;
		Vector2 mouseSmoothVel;
		float prevInputFrameTime;

		public RecordedInputSource(InputFrame[] frames)
		{
			frameQueue = new Queue<InputFrame>(frames);
			if (frameQueue.Count > 0)
			{
				MousePositionTarget = frameQueue.Peek().GetMouseScreenSpace();
				MousePosition = MousePositionTarget;
			}
		}

		public bool PlaybackComplete { get; private set; }
		public float playbackTime { get; private set; }
		public Vector2 MousePosition { get; private set; }

		public bool IsKeyDownThisFrame(KeyCode key) => heldKeysThisFrame.Contains(key) && !heldKeysPrevFrame.Contains(key);

		public bool IsKeyUpThisFrame(KeyCode key) => heldKeysPrevFrame.Contains(key) && !heldKeysThisFrame.Contains(key);

		public bool IsKeyHeld(KeyCode key) => heldKeysThisFrame.Contains(key);

		public bool AnyKeyOrMouseDownThisFrame { get; private set; }
		public bool AnyKeyOrMouseHeldThisFrame => heldKeysThisFrame.Count > 0;
		public string InputString { get; private set; }
		public Vector2 MouseScrollDelta { get; private set; }

		public void UpdatePlayback(float deltaTime)
		{
			if (frameQueue.Count == 0)
			{
				PlaybackComplete = true;
				return;
			}

			playbackTime += deltaTime;

			float currentTime = playbackTime;

			// Update prev frame held keys
			heldKeysPrevFrame.Clear();
			foreach (KeyCode key in heldKeysThisFrame)
			{
				heldKeysPrevFrame.Add(key);
			}

			bool hasNewFrame = false;

			while (frameQueue.Count > 0)
			{
				InputFrame frame = frameQueue.Peek();

				if (frame.Time < currentTime)
				{
					if (!hasNewFrame)
					{
						hasNewFrame = true;
						heldKeysThisFrame.Clear();
						stringBuilder.Clear();
						MouseScrollDelta = Vector2.zero;
					}

					frameQueue.Dequeue();
					prevInputFrameTime = frame.Time;
					MousePositionTarget = frame.GetMouseScreenSpace();
					MouseScrollDelta += frame.MouseScrollDelta;
					stringBuilder.Append(frame.InputString);

					foreach (KeyCode key in frame.HeldKeys)
					{
						heldKeysThisFrame.Add(key);
						AnyKeyOrMouseDownThisFrame |= !heldKeysPrevFrame.Contains(key);
					}
				}
				else
				{
					float t = Mathf.InverseLerp(prevInputFrameTime, frame.Time, currentTime);
					MousePositionTarget = Vector2.Lerp(MousePositionTarget, frame.GetMouseScreenSpace(), t);
					break;
				}
			}

			InputString = stringBuilder.ToString();

			//MousePosition = MousePositionTarget;
			MousePosition = Vector2.SmoothDamp(MousePosition, MousePositionTarget, ref mouseSmoothVel, mouseSmoothTime);

			/*
			if (IsKeyDownThisFrame(KeyCode.C))
			{
				Debug.Log("C: " + IsKeyDownThisFrame(KeyCode.C));
				Debug.Log(frameQueue.Count);
			}*/
		}

		public struct InputFrame
		{
			public float Time;
			public Vector2 MousePosUNorm;
			public KeyCode[] HeldKeys;
			public string InputString;
			public Vector2 MouseScrollDelta;

			public Vector2 GetMouseScreenSpace() => new(MousePosUNorm.x * Screen.width, MousePosUNorm.y * Screen.height);
		}
	}
}