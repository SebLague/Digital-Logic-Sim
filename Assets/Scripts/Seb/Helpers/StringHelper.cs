using System;
using UnityEngine;

namespace Seb.Helpers
{
	public static class StringHelper
	{
		static readonly string[] newLineStrings = { "\r\n", "\r", "\n" };

		public static string[] SplitByLine(string text, bool removeEmptyEntries = false)
		{
			StringSplitOptions options = removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;
			return text.Split(newLineStrings, options);
		}

		public static int CreateIntegerStringNonAlloc(char[] charArray, UInt64 value)
		{
			int digitCount = value == 0 ? 1 : (int)Math.Log10(value) + 1;
			int charCount = digitCount;
			int digitIndex = digitCount - 1;

			do
			{
				charArray[digitIndex--] = (char)('0' + value % 10);
				value /= 10;
			} while (value > 0);

			return charCount;
		}
		public static int CreateIntegerStringNonAlloc(char[] charArray, Int64 value)
		{
			bool isNegative = value < 0;

			value = Math.Abs(value);

			int digitCount = value == 0 ? 1 : (int)Math.Log10(value) + 1;
			int charCount = digitCount;
			int digitIndex = digitCount - 1;

			if (isNegative)
			{
				charArray[0] = '-';
				digitIndex++;
				charCount++;
			}

			do
			{
				Debug.Log($"Max Size: {charArray.Length} | Index: {digitIndex} | Value: {value}");
				charArray[digitIndex--] = (char)('0' + value % 10);
				value /= 10;
			} while (value > 0);

			return charCount;
		}
	}
}