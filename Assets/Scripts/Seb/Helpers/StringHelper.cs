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
				charArray[digitIndex--] = (char)('0' + value % 10);
				value /= 10;
			} while (value > 0);

			return charCount;
		}

		public static int CreateHexStringNonAlloc(char[] charArray, UInt64 value, bool upperCase = true)
		{
			const string hexDigits = "0123456789ABCDEF";
			const string hexDigitsLower = "0123456789abcdef";

			int charCount = 0;
			do
			{
				charArray[charCount++] = (upperCase ? hexDigits : hexDigitsLower)[(int)(value & 0xF)];
				value >>= 4;
			} while (value > 0);

			for (int i = 0; i < charCount / 2; ++i)
			{
				char tmp = charArray[i];
				charArray[i] = charArray[charCount - i - 1];
				charArray[charCount - i - 1] = tmp;
			}

			return charCount;
		}
	}
}