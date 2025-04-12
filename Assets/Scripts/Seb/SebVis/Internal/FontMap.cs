namespace Seb.Vis
{
	public enum FontType
	{
		// -- Monospaced --
		FiraCodeRegular,
		FiraCodeSemiBold,
		FiraCodeBold,
		MapleMonoBold,
		LiberationMonoRegular,
		LiberationMonoBold,
		JetbrainsMonoBold,
		JetbrainsMonoSemiBold,
		JetbrainsMonoExtraBold,
		JetbrainsMonoRegular,

		// -- Proportional --
		OpenSansBold,
		DepartureMono,

		// -- Pixel --
		Born2bSporty //
	}
}

namespace Seb.Vis.Internal
{
	public class FontMap
	{
		public const string FontFolderPath = "Fonts";

		public static readonly (FontType font, string path)[] map =
		{
			(FontType.FiraCodeRegular, "FiraCode/FiraCode-Regular"),
			(FontType.FiraCodeSemiBold, "FiraCode/FiraCode-SemiBold"),
			(FontType.FiraCodeBold, "FiraCode/FiraCode-Bold"),
			(FontType.MapleMonoBold, "MapleMono/MapleMono-Bold"),
			(FontType.OpenSansBold, "OpenSans/OpenSans-Bold"),
			(FontType.LiberationMonoRegular, "LiberationMono/LiberationMono-Regular"),
			(FontType.LiberationMonoBold, "LiberationMono/LiberationMono-Bold"),
			(FontType.JetbrainsMonoBold, "JetbrainsMono/JetBrainsMonoNL-Bold"),
			(FontType.JetbrainsMonoExtraBold, "JetbrainsMono/JetBrainsMonoNL-ExtraBold"),
			(FontType.JetbrainsMonoSemiBold, "JetbrainsMono/JetBrainsMonoNL-SemiBold"),
			(FontType.JetbrainsMonoRegular, "JetbrainsMono/JetBrainsMonoNL-Regular"),
			(FontType.DepartureMono, "DepartureMono/DepartureMono-Regular"),
			(FontType.Born2bSporty, "Born2bSporty/Born2bSportyV2") //
		};
	}
}