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
					ChipType.Out_1Bit,
					ChipType.Out_4Bit,
					ChipType.Out_8Bit,
					ChipType.Out_16Bit,
					ChipType.Out_32Bit
				
				),
				CreateChipCollection("MERGE/SPLIT",
					ChipType.Merge_1To4Bit,
					ChipType.Merge_1To8Bit,
					ChipType.Merge_4To8Bit,
					ChipType.Split_4To1Bit,
					ChipType.Split_8To4Bit,
					ChipType.Split_8To1Bit,


					ChipType.Merge_1To16Bit,
					ChipType.Merge_4To16Bit,
					ChipType.Merge_8To16Bit,
					ChipType.Merge_1To32Bit,
					ChipType.Merge_4To32Bit,
					ChipType.Merge_8To32Bit,
					ChipType.Merge_16To32Bit,
					ChipType.Split_32To1Bit,
					ChipType.Split_32To4Bit,
					ChipType.Split_32To8Bit,
					ChipType.Split_32To16Bit,
					ChipType.Split_16To1Bit,
					ChipType.Split_16To4Bit,
					ChipType.Split_16To8Bit
					
				),
				CreateChipCollection("BUS",
					ChipType.Bus_1Bit,
					ChipType.Bus_4Bit,
					ChipType.Bus_8Bit
				),
				CreateChipCollection("DISPLAY",
					ChipType.SevenSegmentDisplay,
					ChipType.DisplayDot,
					ChipType.DisplayRGB
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