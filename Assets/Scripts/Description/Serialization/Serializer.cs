using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DLS.Description
{
	public static class Serializer
	{
		public static string SerializeAppSettings(AppSettings settings) => Serialize(settings);
		public static string SerializeChipDescription(ChipDescription description) => Serialize(description);
		public static string SerializeProjectDescription(ProjectDescription description) => Serialize(description);

		public static AppSettings DeserializeAppSettings(string settingsString) => Deserialize<AppSettings>(settingsString);
		public static ChipDescription DeserializeChipDescription(string serializedDescription) => Deserialize<ChipDescription>(serializedDescription);
		public static ProjectDescription DeserializeProjectDescription(string serializedDescription) => Deserialize<ProjectDescription>(serializedDescription);

		static JsonSerializerSettings CreateSerializationSettings()
		{
			JsonSerializerSettings settings = new();
			settings.Converters.Add(new Vector2Converter());
			settings.Converters.Add(new ColorConverter());
			settings.Converters.Add(new DateTimeConverter());
			return settings;
		}

		static string Serialize(object obj)
		{
			using StringWriter stringWriter = new();
			using CustomJsonTextWriter writer = new(stringWriter);
			writer.Formatting = Formatting.Indented;
			writer.Indentation = 2;
			writer.IndentChar = ' ';

			JsonSerializer serializer = JsonSerializer.Create(CreateSerializationSettings());
			serializer.Serialize(writer, obj);

			return stringWriter.ToString();
		}

		static T Deserialize<T>(string s)
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(s, CreateSerializationSettings());
			}
			catch (JsonException e)
			{
				Debug.LogError(e);
				return default;
			}
		}

		class Vector2Converter : JsonConverter<Vector2>
		{
			public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
			{
				const int digitCount = 5;
				writer.WriteStartObject();

				writer.WritePropertyName("x");
				writer.WriteValue(Math.Round(value.x, digitCount));

				writer.WritePropertyName("y");
				writer.WriteValue(Math.Round(value.y, digitCount));

				writer.WriteEndObject();
			}

			public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				JObject obj = JObject.Load(reader);
				return new Vector2((float)obj["x"], (float)obj["y"]);
			}
		}

		class ColorConverter : JsonConverter<Color>
		{
			public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
			{
				JObject obj = new()
				{
					{ "r", value.r },
					{ "g", value.g },
					{ "b", value.b },
					{ "a", 1 }
				};
				obj.WriteTo(writer);
			}

			public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				JObject obj = JObject.Load(reader);
				Color col = new((float)obj["r"], (float)obj["g"], (float)obj["b"], (float)obj["a"]);
				return col;
			}
		}

		public class DateTimeConverter : JsonConverter<DateTime>
		{
			const string DateFormat = "yyyy-MM-ddTHH:mm:ss.fffK"; // ISO 8601

			public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
			{
				string formattedDate = value.ToString(DateFormat, CultureInfo.InvariantCulture);
				writer.WriteValue(formattedDate);
			}

			public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer) => (DateTime)reader.Value;
		}

		class CustomJsonTextWriter : JsonTextWriter
		{
			int arrayDepth;

			public CustomJsonTextWriter(TextWriter textWriter) : base(textWriter)
			{
			}

			public override void WriteStartArray()
			{
				arrayDepth++;
				base.WriteStartArray();
			}

			public override void WriteEndArray()
			{
				base.WriteEndArray();
				arrayDepth--;
			}

			protected override void WriteIndent()
			{
				// Skip indenting within nested arrays
				if (arrayDepth > 1) return;

				base.WriteIndent();
			}

			protected override void WriteIndentSpace()
			{
				// Don't add extra spaces in arrays
				if (arrayDepth > 0) return;

				base.WriteIndentSpace();
			}
		}
	}
}