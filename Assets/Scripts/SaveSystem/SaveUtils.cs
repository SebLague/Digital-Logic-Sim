using System;
using System.Collections.Generic;
using System.IO;

namespace DLS.SaveSystem
{
	public static class SaveUtils
	{
		// Don't allow these characters in project/chip names as they are illegal (or behave strangely) in some operating systems.
		static readonly HashSet<char> ForbiddenChars = new(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '.' });

		// Reserved file names on windows
		static readonly string[] ReservedNames =
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
		};

		// Test if file name is valid on all operating systems
		public static bool ValidFileName(string name)
		{
			if (string.IsNullOrEmpty(name)) return false;
			bool hasIllegalChar = NameContainsForbiddenChar(name);
			bool reservedFileName = IsReservedFileName(name);

			return !hasIllegalChar && !reservedFileName;
		}

		static bool IsReservedFileName(string name)
		{
			ReadOnlySpan<char> nameWithoutWhitespace = name.AsSpan().Trim();

			for (int i = 0; i < ReservedNames.Length; i++)
			{
				ReadOnlySpan<char> reservedName = ReservedNames[i];
				if (nameWithoutWhitespace.Equals(reservedName, StringComparison.OrdinalIgnoreCase)) return true;
			}

			return false;
		}

		public static bool NameContainsForbiddenChar(string name)
		{
			if (string.IsNullOrEmpty(name)) return false;

			foreach (char c in name)
			{
				if (ForbiddenChars.Contains(c)) return true; //
			}

			return false;
		}

		// Ensure file name is unique by appending a number to it if necessary
		public static string EnsureUniqueFileName(string originalPath)
		{
			string originalFileName = Path.GetFileName(originalPath);
			string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
			string extension = Path.GetExtension(originalPath);
			string uniquePath = originalPath;

			int duplicates = 0;

			while (File.Exists(uniquePath))
			{
				duplicates++;
				string uniqueFileName = $"{originalFileNameWithoutExtension}_{duplicates}{extension}";
				uniquePath = originalPath.Replace(originalFileName, uniqueFileName);
			}

			return uniquePath;
		}

		// Ensure directory name is unique by appending a number the path if necessary
		public static string EnsureUniqueDirectoryName(string path)
		{
			string uniquePath = path;
			int numDuplicates = 0;

			while (Directory.Exists(uniquePath))
			{
				numDuplicates++;
				uniquePath = path + "_" + numDuplicates;
			}

			return uniquePath;
		}

		// Thanks to https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
		public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{
			// Get information about the source directory
			DirectoryInfo dir = new DirectoryInfo(sourceDir);

			// Check if the source directory exists
			if (!dir.Exists)
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			// Cache directories before we start copying
			DirectoryInfo[] dirs = dir.GetDirectories();

			// Create the destination directory
			Directory.CreateDirectory(destinationDir);

			// Get the files in the source directory and copy to the destination directory
			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath);
			}

			// If recursive and copying subdirectories, recursively call this method
			if (recursive)
			{
				foreach (DirectoryInfo subDir in dirs)
				{
					string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
					CopyDirectory(subDir.FullName, newDestinationDir, true);
				}
			}
		}
	}
}