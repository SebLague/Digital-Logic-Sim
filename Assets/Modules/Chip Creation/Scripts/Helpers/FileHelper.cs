using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace DLS.ChipCreation
{
	public static class FileHelper
	{
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

		// Ensure directory name is unique by appending a number to it if necessary
		public static string EnsureUniqueDirectoryName(string originalPath)
		{
			string uniquePath = originalPath;
			int duplicates = 0;

			while (Directory.Exists(uniquePath))
			{
				duplicates++;
				uniquePath = originalPath + "_" + duplicates;
			}

			return uniquePath;
		}
 
		// Thanks to https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
		public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{
			// Get information about the source directory
			var dir = new DirectoryInfo(sourceDir);

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