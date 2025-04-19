using System.Linq;
using DLS.Description;

namespace DLS.Game
{
	public static class BuiltinCollectionCreator
	{
		public static StarredItem[] GetDefaultStarredList()
		{
			return new StarredItem[]
			{
				new("IN/OUT", true),
				new(ChipTypeHelper.GetName(ChipType.Nand), false)
			};
		}

		public static ChipCollection[] CreateDefaultChipCollections()
		{
			return new[]
			{
				CreateChipCollection("BASIC",
					ChipType.Nand,
					ChipType.Clock,
					ChipType.Key,
					ChipType.TriStateBuffer
				),
				CreateChipCollection("IN/OUT",
					ChipType.In_1Bit,
					ChipType.In_4Bit,
					ChipType.In_8Bit,
					ChipType.In_16Bit,
					ChipType.In_32Bit,
					ChipType.In_64Bit,
					ChipType.In_128Bit,
					ChipType.In_256Bit,
					ChipType.Out_1Bit,
					ChipType.Out_4Bit,
					ChipType.Out_8Bit,
					ChipType.Out_16Bit,
					ChipType.Out_32Bit,
					ChipType.Out_64Bit,
					ChipType.Out_128Bit,
					ChipType.Out_256Bit
				
				),
				CreateChipCollection("MERGE/SPLIT",
					ChipType.Split_4To1Bit,
					ChipType.Split_8To1Bit,
					ChipType.Split_16To1Bit,
					ChipType.Split_32To1Bit,
					ChipType.Split_64To1Bit,
					ChipType.Split_128To1Bit,
					ChipType.Split_256To1Bit,
					ChipType.Split_8To4Bit,
					ChipType.Split_16To4Bit,
					ChipType.Split_32To4Bit,
					ChipType.Split_64To4Bit,
					ChipType.Split_128To4Bit,
					ChipType.Split_256To4Bit,
					ChipType.Split_16To8Bit,
					ChipType.Split_32To8Bit,
					ChipType.Split_64To8Bit,
					ChipType.Split_128To8Bit,
					ChipType.Split_256To8Bit,
					ChipType.Split_32To16Bit,
					ChipType.Split_64To16Bit,
					ChipType.Split_128To16Bit,
					ChipType.Split_256To16Bit,
					ChipType.Split_64To32Bit,
					ChipType.Split_128To32Bit,
					ChipType.Split_256To32Bit,
					ChipType.Split_128To64Bit,
					ChipType.Split_256To64Bit,
					ChipType.Split_256To128Bit,
					ChipType.Merge_128To256Bit,
					ChipType.Merge_64To256Bit,
					ChipType.Merge_64To128Bit,
					ChipType.Merge_32To256Bit,
					ChipType.Merge_32To128Bit,
					ChipType.Merge_32To64Bit,
					ChipType.Merge_16To256Bit,
					ChipType.Merge_16To128Bit,
					ChipType.Merge_16To64Bit,
					ChipType.Merge_16To32Bit,
					ChipType.Merge_8To256Bit,
					ChipType.Merge_8To128Bit,
					ChipType.Merge_8To64Bit,
					ChipType.Merge_8To32Bit,
					ChipType.Merge_8To16Bit,
					ChipType.Merge_4To256Bit,
					ChipType.Merge_4To128Bit,
					ChipType.Merge_4To64Bit,
					ChipType.Merge_4To32Bit,
					ChipType.Merge_4To16Bit,
					ChipType.Merge_4To8Bit,
					ChipType.Merge_1To256Bit,
					ChipType.Merge_1To128Bit,
					ChipType.Merge_1To64Bit,
					ChipType.Merge_1To32Bit,
					ChipType.Merge_1To16Bit,
					ChipType.Merge_1To8Bit,
					ChipType.Merge_1To4Bit
				),
				CreateChipCollection("BUS",
					ChipType.Bus_1Bit,
					ChipType.Bus_4Bit,
					ChipType.Bus_8Bit,
					ChipType.Bus_16Bit,
					ChipType.Bus_32Bit,
					ChipType.Bus_64Bit,
					ChipType.Bus_128Bit,
					ChipType.Bus_256Bit
				),
				CreateChipCollection("DISPLAY",
					ChipType.SevenSegmentDisplay,
					ChipType.DisplayDot,
					ChipType.DisplayRGB,
					ChipType.DisplayLED
				),
				CreateChipCollection("MEMORY",
					ChipType.Rom_256x16
				)
			};
		}

		static ChipCollection CreateChipCollection(string name, params ChipType[] chipTypes)
		{
			return new ChipCollection(name, chipTypes.Select(t => ChipTypeHelper.GetName(t)).ToArray());
		}
	}
}
