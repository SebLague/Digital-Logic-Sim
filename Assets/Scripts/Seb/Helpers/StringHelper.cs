using System;
using System.Collections.Generic;
using System.Linq;

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

		public static string CreateBinaryString(uint value, bool removeLeadingZeroes = false)
		{
			string binary = Convert.ToString(value, 2);
			if (!removeLeadingZeroes)
			{
				binary = binary.PadLeft(32, '0');
			}
			else
			{
				int paddedLength = (binary.Length + 7) / 8 * 8;
				binary = binary.PadLeft(paddedLength, '0');
			}

			IEnumerable<string> grouped = Enumerable.Range(0, binary.Length / 8)
				.Select(i => binary.Substring(i * 8, 8));

			return string.Join(" ", grouped);
		}

		public static int CreateIntegerStringNonAlloc(char[] charArray, int value)
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

		public static int CreateHexStringNonAlloc(char[] charArray, int value, bool upperCase = true)
		{
			const string hexDigits = "0123456789ABCDEF";
			const string hexDigitsLower = "0123456789abcdef";

			int charCount = 0;
			uint uValue = (uint)value;
			do
			{
				charArray[charCount++] = (upperCase ? hexDigits : hexDigitsLower)[(int)(uValue & 0xF)];
				uValue >>= 4;
			} while (uValue > 0);

			for (int i = 0; i < charCount / 2; i++)
			{
				int swapIndex = charCount - i - 1;
				(charArray[i], charArray[swapIndex]) = (charArray[swapIndex], charArray[i]);
			}

			return charCount;
		}
	}
}