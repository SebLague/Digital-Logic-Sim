using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seb.Helpers;
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace DLS.Dev.VidTools
{
	public class InputRecorder : MonoBehaviour
	{
		public string saveFileName;

		KeyCode[] allKeys;

		readonly List<RecordedInputSource.InputFrame> frames = new();
		readonly List<KeyCode> heldKeys = new();
		int markerFrameIndex;
		float recordTime;
		public bool IsRecording { get; private set; }


		void LateUpdate()
		{
			if (IsRecording)
			{
				UpdateRecording();
			}
		}

		public void StartRecording()
		{
			IsRecording = true;
			allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
			recordTime = 0;
			frames.Clear();
			heldKeys.Clear();
			markerFrameIndex = -1;

			Debug.Log("Recording started");
		}

		public void StopRecording()
		{
			IsRecording = false;
			Debug.Log("Recording stopped");

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.Converters.Add(new Vector2Converter());

			string json = JsonConvert.SerializeObject(frames, settings);
			string path = Path.Combine(Application.dataPath, "Dev", "VidTools", "InputRecording", saveFileName + ".json");
			using StreamWriter writer = new(path);
			writer.Write(json);

			Debug.Log("Input recording saved to: " + path);
		}

		public void SetMarker()
		{
			markerFrameIndex = frames.Count - 1;
		}

		public Vector2 RestoreFromMarker()
		{
			if (markerFrameIndex >= 0 && markerFrameIndex + 1 < frames.Count)
			{
				frames.RemoveRange(markerFrameIndex + 1, frames.Count - markerFrameIndex - 1);
			}

			recordTime = frames[^1].Time;
			heldKeys.Clear();

			return frames[^1].GetMouseScreenSpace();
		}

		public void Pause() => IsRecording = false;
		public void Resume() => IsRecording = true;

		void UpdateRecording()
		{
			recordTime += Time.deltaTime;
			heldKeys.Clear();
			if (InputHelper.AnyKeyOrMouseHeldThisFrame)
			{
				foreach (KeyCode key in allKeys)
				{
					if (InputHelper.IsKeyHeld(key))
					{
						heldKeys.Add(key);
					}
				}
			}

			RecordedInputSource.InputFrame frame = new()
			{
				Time = recordTime,
				MousePosUNorm = new Vector2(InputHelper.MousePos.x / Screen.width, InputHelper.MousePos.y / Screen.height),
				HeldKeys = heldKeys.ToArray(),
				InputString = InputHelper.InputStringThisFrame,
				MouseScrollDelta = InputHelper.MouseScrollDelta
			};

			frames.Add(frame);
		}
	}

	internal class Vector2Converter : JsonConverter<Vector2>
	{
		public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
		{
			writer.WriteStartObject();

			writer.WritePropertyName("x");
			writer.WriteValue(value.x);

			writer.WritePropertyName("y");
			writer.WriteValue(value.y);

			writer.WriteEndObject();
		}

		public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			JObject obj = JObject.Load(reader);
			return new Vector2((float)obj["x"], (float)obj["y"]);
		}
	}
}