using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DLS.ChipCreation
{
	public static class NameValidationHelper
	{
		// Don't allow these characters in project/chip names as they are illegal (or behave strangely) in some operating systems.
		public static readonly HashSet<char> ForbiddenChars = new(new char[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '.' });
		// Reserved file names on windows, or in the save-system
		public static readonly HashSet<string> ReservedNames = new(new string[]
		{
			"CON",
			"PRN",
			"AUX",
			"NUL",
			"COM1",
			"COM2",
			"COM3",
			"COM4",
			"COM5",
			"COM6",
			"COM7",
			"COM8",
			"COM9",
			"LPT1",
			"LPT2",
			"LPT3",
			"LPT4",
			"LPT5",
			"LPT6",
			"LPT7",
			"LPT8",
			"LPT9"
		}, System.StringComparer.OrdinalIgnoreCase);

		// Test if file name is valid on all operating systems
		public static bool ValidFileName(string name)
		{
			bool hasIllegalChar = name.Any(c => ForbiddenChars.Contains(c));
			bool reservedFileName = ReservedNames.Contains(name.Trim());
			return !hasIllegalChar && !reservedFileName;
		}
	}
}