using System.IO;
using UnityEngine;

namespace Seb.Vis.Text.Demo
{
	public static class FontExampleLibrary
	{
		public enum TypeFace
		{
			JetBrainsMono,
			MapleMono,
			FiraCode,
			OpenSans,
			LibreBaskerville,
			RobotoSlab,
			Shrikhand,
			Pacifico,
			Cakecafe,
			Pixelify,
			PermanentMarker,
			Nicolast
		}

		public enum Variant
		{
			Regular,
			Bold
		}

		static readonly Entry[] entries =
		{
			new(TypeFace.JetBrainsMono, "JetBrains-Mono", "JetBrainsMonoNL-Regular", "JetBrainsMono-Bold"),
			new(TypeFace.OpenSans, "OpenSans", "OpenSans-Regular", "OpenSans-Bold"),
			new(TypeFace.FiraCode, "FiraCode", "FiraCode-Regular", "FiraCode-Bold"),
			new(TypeFace.MapleMono, "MapleMono", "MapleMono-Regular", "MapleMono-Bold"),
			new(TypeFace.LibreBaskerville, "LibreBaskerville", "LibreBaskerville-Regular", "LibreBaskerville-Bold"),
			new(TypeFace.RobotoSlab, "RobotoSlab", "RobotoSlab-Regular", "RobotoSlab-Bold"),
			new(TypeFace.Shrikhand, "Shrikhand", "Shrikhand-Regular", "Shrikhand-Regular"),
			new(TypeFace.Pacifico, "Pacifico", "Pacifico-Regular", "Pacifico-Regular"),
			new(TypeFace.Cakecafe, "Cakecafe", "Cakecafe", "Cakecafe"),
			new(TypeFace.Pixelify, "Pixelify", "PixelifySans-Regular", "PixelifySans-Bold"),
			new(TypeFace.PermanentMarker, "PermanentMarker", "PermanentMarker-Regular", "PermanentMarker-Regular"),
			new(TypeFace.Nicolast, "Nicolast", "Nicolast", "Nicolast")
		};

		public static string GetFontPath(TypeFace typeface, Variant variant)
		{
			foreach (Entry entry in entries)
			{
				if (entry.TypeFace == typeface)
				{
					return variant == Variant.Regular ? entry.PathRegular : entry.PathBold;
				}
			}

			return string.Empty;
		}


		public struct Entry
		{
			public TypeFace TypeFace;
			public string PathRegular;
			public string PathBold;

			public Entry(TypeFace typeFace, string directory, string nameRegular, string nameBold)
			{
				TypeFace = typeFace;
				PathRegular = MakePath(nameRegular);
				PathBold = MakePath(nameBold);

				string MakePath(string name)
				{
					return Path.Combine(Application.dataPath, "Fonts", directory, name + ".ttf");
				}
			}
		}
	}
}