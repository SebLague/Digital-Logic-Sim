using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DLS.ChipData
{
	public static class BuiltinChipNames
	{
		public const string AndChip = "AND";
		public const string NotChip = "NOT";
		public const string TriStateBufferName = "TRI-STATE BUFFER";
		public const string SevenSegmentDisplayName = "7-SEGMENT DISPLAY";
		public const string BusName = "BUS";
		public const string ClockName = "CLOCK";

		static readonly string[] allNames = new string[]
		{
			AndChip,
			NotChip,
			TriStateBufferName,
			SevenSegmentDisplayName,
			BusName,
			ClockName
		};

		public static bool IsBuiltinName(string chipName, bool ignoreCase = true)
		{

			for (int i = 0; i < allNames.Length; i++)
			{
				if (Compare(chipName, allNames[i], ignoreCase))
				{
					return true;
				}
			}

			return false;
		}

		public static bool Compare(string a, string b, bool ignoreCase = true)
		{
			System.StringComparison comparison = (ignoreCase) ? System.StringComparison.OrdinalIgnoreCase : System.StringComparison.Ordinal;
			return string.Equals(a, b, comparison);
		}
	}
}