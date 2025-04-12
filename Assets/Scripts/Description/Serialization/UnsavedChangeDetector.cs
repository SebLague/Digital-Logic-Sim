using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DLS.Description
{
	public static class UnsavedChangeDetector
	{
		const float Epsilon = 0.0001f;

		// Test if two json strings are equivalent.
		// Unlike directly comparing the strings, this ignores things like formatting and comments.
		// It also uses an approximate comparison for float values.
		public static bool IsEquivalentJson(string jsonA, string jsonB)
		{
			try
			{
				JToken tokenA = JsonConvert.DeserializeObject<JToken>(jsonA);
				JToken tokenB = JsonConvert.DeserializeObject<JToken>(jsonB);
				return IsEquivalentToken(tokenA, tokenB);
			}
			catch (Exception)
			{
				return false;
			}
		}

		static bool IsEquivalentToken(JToken tokenA, JToken tokenB)
		{
			if (tokenA == null && tokenB == null) return true;
			if (tokenA == null || tokenB == null) return false;
			if (tokenA.Type != tokenB.Type) return false;

			return tokenA.Type switch
			{
				JTokenType.Object => IsEquivalentObject((JObject)tokenA, (JObject)tokenB),
				JTokenType.Array => IsEquivalentArray((JArray)tokenA, (JArray)tokenB),
				JTokenType.Float => Math.Abs((float)tokenA - (float)tokenB) < Epsilon,
				JTokenType.Comment => true,
				_ => JToken.DeepEquals(tokenA, tokenB)
			};
		}

		static bool IsEquivalentArray(JArray arrayA, JArray arrayB)
		{
			if (arrayA.Count != arrayB.Count) return false;

			for (int i = 0; i < arrayA.Count; i++)
			{
				if (!IsEquivalentToken(arrayA[i], arrayB[i])) return false;
			}

			return true;
		}

		static bool IsEquivalentObject(JObject objA, JObject objB)
		{
			if (objA.Count != objB.Count) return false;

			foreach (JProperty propertyA in objA.Properties())
			{
				if (!objB.TryGetValue(propertyA.Name, out JToken valueB)) return false;
				if (!IsEquivalentToken(propertyA.Value, valueB)) return false;
			}

			return true;
		}
	}
}