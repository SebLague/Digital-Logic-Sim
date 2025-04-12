using System;
using System.Collections.Generic;

namespace Seb.Vis.Text.FontLoading
{
	// An (incomplete) Parser for TrueType font files
	public static class FontParser
	{
		// Parse font from byte array (ttf file contents)
		public static FontData Parse(byte[] bytes)
		{
			using FontReader reader = new(bytes);
			return Parse(reader);
		}

		// Parse font from path to ttf file
		public static FontData Parse(string pathToFont)
		{
			using FontReader reader = new(pathToFont);
			return Parse(reader);
		}

		// Parse font
		static FontData Parse(FontReader reader)
		{
			TableMap tableMap = ReadTableLocations(reader);

			int unitsPerEm = GetUnitsPerEm(reader, tableMap);
			int numGlyphs = GetGlyphCount(reader, tableMap);

			GlyphRaw[] glyphs = ReadAllGlyphs(reader, tableMap, numGlyphs);
			ApplyLayoutInfo(reader, tableMap, glyphs, numGlyphs);
			bool isMono = CheckIfMonospaced(reader, tableMap);

			return new FontData(glyphs, unitsPerEm, isMono);
		}

		static bool CheckIfMonospaced(FontReader reader, TableMap tableMap)
		{
			reader.GoTo(tableMap.Post + 12);
			uint mono = reader.ReadUInt32();
			return mono != 0;
		}

		static int GetBytesPerLocationEntry(FontReader reader, TableMap tableMap)
		{
			reader.GoTo(tableMap.Head + 50);
			return reader.ReadInt16() == 0 ? 2 : 4;
		}

		static int GetUnitsPerEm(FontReader reader, TableMap tableMap)
		{
			reader.GoTo(tableMap.Head + 18);
			return reader.ReadUInt16(); // Num 'Design units' per 'Em' (range from 64 to 16384)
		}

		static int GetGlyphCount(FontReader reader, TableMap tableMap)
		{
			reader.GoTo(tableMap.Maxp);
			reader.SkipBytes(4);
			return reader.ReadUInt16();
		}

		static void ApplyLayoutInfo(FontReader reader, TableMap tableMap, GlyphRaw[] glyphs, int numGlyphs)
		{
			(int advance, int left)[] layoutData = new (int, int)[numGlyphs];

			// Get number of metrics from the 'hhea' table
			reader.GoTo(tableMap.Hhea);

			reader.SkipBytes(8); // unused: version, ascent, descent
			int lineGap = reader.ReadInt16();
			int advanceWidthMax = reader.ReadInt16();
			reader.SkipBytes(22); // unused: minLeftSideBearing, minRightSideBearing, xMaxExtent, caretSlope/Offset, reserved, metricDataFormat
			int numAdvanceWidthMetrics = reader.ReadInt16();

			// Get the advance width and leftsideBearing metrics from the 'hmtx' table
			reader.GoTo(tableMap.Hmtx);
			int lastAdvanceWidth = 0;

			for (int i = 0; i < numAdvanceWidthMetrics; i++)
			{
				int advanceWidth = reader.ReadUInt16();
				int leftSideBearing = reader.ReadInt16();
				lastAdvanceWidth = advanceWidth;

				layoutData[i] = (advanceWidth, leftSideBearing);
			}

			// Some fonts have a run of monospace characters at the end
			int numRem = numGlyphs - numAdvanceWidthMetrics;

			for (int i = 0; i < numRem; i++)
			{
				int leftSideBearing = reader.ReadInt16();
				int glyphIndex = numAdvanceWidthMetrics + i;

				layoutData[glyphIndex] = (lastAdvanceWidth, leftSideBearing);
			}

			// Apply
			foreach (GlyphRaw c in glyphs)
			{
				c.AdvanceWidth = layoutData[c.GlyphIndex].advance;
				c.LeftSideBearing = layoutData[c.GlyphIndex].left;
			}
		}

		// -- Read Font Directory to create a lookup of table locations by their 4-character nametag --
		static TableMap ReadTableLocations(FontReader reader)
		{
			Dictionary<string, uint> tableLocationLookup = new();

			// -- offset subtable --
			reader.SkipBytes(4); // unused: scalerType
			int numTables = reader.ReadUInt16();
			reader.SkipBytes(6); // unused: searchRange, entrySelector, rangeShift

			// -- table directory --
			for (int i = 0; i < numTables; i++)
			{
				string tag = reader.ReadString(4);
				uint checksum = reader.ReadUInt32();
				uint offset = reader.ReadUInt32();
				uint length = reader.ReadUInt32();

				tableLocationLookup.Add(tag, offset);
			}

			return new TableMap(tableLocationLookup);
		}

		static GlyphRaw[] ReadAllGlyphs(FontReader reader, TableMap tableMap, int numGlyphs)
		{
			uint[] glyphLocations = GetAllGlyphLocations(reader, numGlyphs, tableMap);
			GlyphMap[] mappings = GetUnicodeToGlyphIndexMappings(reader, tableMap.Cmap);

			GlyphRaw[] glyphs = new GlyphRaw[mappings.Length];

			for (int i = 0; i < mappings.Length; i++)
			{
				GlyphMap mapping = mappings[i];

				GlyphRaw glyph = ReadGlyph(reader, glyphLocations, mapping.GlyphIndex);
				glyph.UnicodeValue = mapping.Unicode;
				glyphs[i] = glyph;
			}

			return glyphs;
		}

		static GlyphRaw ReadGlyph(FontReader reader, uint[] glyphLocations, uint glyphIndex)
		{
			uint glyphLocation = glyphLocations[glyphIndex];

			reader.GoTo(glyphLocation);
			int contourCount = reader.ReadInt16();

			// Glyph is either simple or compound
			// * Simple: outline data is stored here directly
			// * Compound: two or more simple glyphs need to be looked up, transformed, and combined
			bool isSimpleGlyph = contourCount >= 0;

			if (isSimpleGlyph) return ReadSimpleGlyph(reader, glyphLocations, glyphIndex);
			return ReadCompoundGlyph(reader, glyphLocations, glyphIndex);
		}

		// Read a simple glyph from the 'glyf' table
		static GlyphRaw ReadSimpleGlyph(FontReader reader, uint[] glyphLocations, uint glyphIndex)
		{
			// Flag masks
			const int OnCurve = 0;
			const int IsSingleByteX = 1;
			const int IsSingleByteY = 2;
			const int Repeat = 3;
			const int InstructionX = 4;
			const int InstructionY = 5;

			reader.GoTo(glyphLocations[glyphIndex]);

			GlyphRaw glyph = new();
			glyph.GlyphIndex = glyphIndex;

			int contourCount = reader.ReadInt16();
			if (contourCount < 0) throw new Exception("Expected simple glyph, but found compound glyph instead");

			glyph.MinX = reader.ReadInt16();
			glyph.MinY = reader.ReadInt16();
			glyph.MaxX = reader.ReadInt16();
			glyph.MaxY = reader.ReadInt16();

			// Read contour ends
			int numPoints = 0;
			int[] contourEndIndices = new int[contourCount];

			for (int i = 0; i < contourCount; i++)
			{
				int contourEndIndex = reader.ReadUInt16();
				numPoints = Math.Max(numPoints, contourEndIndex + 1);
				contourEndIndices[i] = contourEndIndex;
			}

			int instructionsLength = reader.ReadInt16();
			reader.SkipBytes(instructionsLength); // skip instructions (hinting stuff)

			byte[] allFlags = new byte[numPoints];
			Point[] points = new Point[numPoints];

			for (int i = 0; i < numPoints; i++)
			{
				byte flag = reader.ReadByte();
				allFlags[i] = flag;

				if (FlagBitIsSet(flag, Repeat))
				{
					int repeatCount = reader.ReadByte();

					for (int r = 0; r < repeatCount; r++)
					{
						i++;
						allFlags[i] = flag;
					}
				}
			}

			ReadCoords(true);
			ReadCoords(false);
			glyph.Points = points;
			glyph.ContourEndIndices = contourEndIndices;
			return glyph;

			void ReadCoords(bool readingX)
			{
				int min = int.MaxValue;
				int max = int.MinValue;

				int singleByteFlagBit = readingX ? IsSingleByteX : IsSingleByteY;
				int instructionFlagMask = readingX ? InstructionX : InstructionY;

				int coordVal = 0;

				for (int i = 0; i < numPoints; i++)
				{
					byte currFlag = allFlags[i];

					// Offset value is represented with 1 byte (unsigned)
					// Here the instruction flag tells us whether to add or subtract the offset
					if (FlagBitIsSet(currFlag, singleByteFlagBit))
					{
						int coordOffset = reader.ReadByte();
						bool positiveOffset = FlagBitIsSet(currFlag, instructionFlagMask);
						coordVal += positiveOffset ? coordOffset : -coordOffset;
					}
					// Offset value is represented with 2 bytes (signed)
					// Here the instruction flag tells us whether an offset value exists or not
					else if (!FlagBitIsSet(currFlag, instructionFlagMask))
					{
						coordVal += reader.ReadInt16();
					}

					if (readingX) points[i].X = coordVal;
					else points[i].Y = coordVal;
					points[i].OnCurve = FlagBitIsSet(currFlag, OnCurve);

					min = Math.Min(min, coordVal);
					max = Math.Max(max, coordVal);
				}
			}
		}

		static GlyphRaw ReadCompoundGlyph(FontReader reader, uint[] glyphLocations, uint glyphIndex)
		{
			GlyphRaw compoundGlyph = new();
			compoundGlyph.GlyphIndex = glyphIndex;

			uint glyphLocation = glyphLocations[glyphIndex];
			reader.GoTo(glyphLocation);
			reader.SkipBytes(2);

			compoundGlyph.MinX = reader.ReadInt16();
			compoundGlyph.MinY = reader.ReadInt16();
			compoundGlyph.MaxX = reader.ReadInt16();
			compoundGlyph.MaxY = reader.ReadInt16();

			List<Point> allPoints = new();
			List<int> allContourEndIndices = new();

			while (true)
			{
				(GlyphRaw componentGlyph, bool hasMoreGlyphs) = ReadNextComponentGlyph(reader, glyphLocations, glyphLocation);

				// Add all contour end indices from the simple glyph component to the compound glyph's data
				// Note: indices must be offset to account for previously-added component glyphs
				foreach (int endIndex in componentGlyph.ContourEndIndices)
				{
					allContourEndIndices.Add(endIndex + allPoints.Count);
				}

				allPoints.AddRange(componentGlyph.Points);

				if (!hasMoreGlyphs) break;
			}

			compoundGlyph.Points = allPoints.ToArray();
			compoundGlyph.ContourEndIndices = allContourEndIndices.ToArray();
			return compoundGlyph;
		}

		static (GlyphRaw glyph, bool hasMoreGlyphs) ReadNextComponentGlyph(FontReader reader, uint[] glyphLocations, uint glyphLocation)
		{
			uint flag = reader.ReadUInt16();
			uint glyphIndex = reader.ReadUInt16();

			uint componentGlyphLocation = glyphLocations[glyphIndex];
			// If compound glyph refers to itself, return empty glyph to avoid infinite loop.
			// Had an issue with this on the 'carriage return' character in robotoslab.
			// There's likely a bug in my parsing somewhere, but this is my work-around for now...
			if (componentGlyphLocation == glyphLocation)
			{
				return (new GlyphRaw { Points = Array.Empty<Point>(), ContourEndIndices = Array.Empty<int>() }, false);
			}

			// Decode flags
			bool argsAre2Bytes = FlagBitIsSet(flag, 0);
			bool argsAreXYValues = FlagBitIsSet(flag, 1);
			bool roundXYToGrid = FlagBitIsSet(flag, 2);
			bool isSingleScaleValue = FlagBitIsSet(flag, 3);
			bool isMoreComponentsAfterThis = FlagBitIsSet(flag, 5);
			bool isXAndYScale = FlagBitIsSet(flag, 6);
			bool is2x2Matrix = FlagBitIsSet(flag, 7);
			bool hasInstructions = FlagBitIsSet(flag, 8);
			bool useThisComponentMetrics = FlagBitIsSet(flag, 9);
			bool componentsOverlap = FlagBitIsSet(flag, 10);

			// Read args (these are either x/y offsets, or point number)
			int arg1 = argsAre2Bytes ? reader.ReadInt16() : reader.ReadSByte();
			int arg2 = argsAre2Bytes ? reader.ReadInt16() : reader.ReadSByte();

			if (!argsAreXYValues) throw new Exception("TODO: Args1&2 are point indices to be matched, rather than offsets");

			double offsetX = arg1;
			double offsetY = arg2;

			double iHat_x = 1;
			double iHat_y = 0;
			double jHat_x = 0;
			double jHat_y = 1;

			if (isSingleScaleValue)
			{
				iHat_x = reader.ReadFixedPoint2Dot14();
				jHat_y = iHat_x;
			}
			else if (isXAndYScale)
			{
				iHat_x = reader.ReadFixedPoint2Dot14();
				jHat_y = reader.ReadFixedPoint2Dot14();
			}
			// Todo: incomplete implemntation
			else if (is2x2Matrix)
			{
				throw new Exception("TOOD: Implement 2x2 matrix mode in font parser");
			}

			uint currentCompoundGlyphReadLocation = reader.GetLocation();
			GlyphRaw simpleGlyph = ReadGlyph(reader, glyphLocations, glyphIndex);
			reader.GoTo(currentCompoundGlyphReadLocation);

			for (int i = 0; i < simpleGlyph.Points.Length; i++)
			{
				(double xPrime, double yPrime) = TransformPoint(simpleGlyph.Points[i].X, simpleGlyph.Points[i].Y);
				simpleGlyph.Points[i].X = (int)xPrime;
				simpleGlyph.Points[i].Y = (int)yPrime;
			}

			return (simpleGlyph, isMoreComponentsAfterThis);

			(double xPrime, double yPrime) TransformPoint(double x, double y)
			{
				double xPrime = iHat_x * x + jHat_x * y + offsetX;
				double yPrime = iHat_y * x + jHat_y * y + offsetY;
				return (xPrime, yPrime);
			}
		}


		static uint[] GetAllGlyphLocations(FontReader reader, int numGlyphs, TableMap tableMap)
		{
			uint[] allGlyphLocations = new uint[numGlyphs];
			int bytesPerEntry = GetBytesPerLocationEntry(reader, tableMap);
			bool isTwoByteEntry = bytesPerEntry == 2;

			for (int glyphIndex = 0; glyphIndex < numGlyphs; glyphIndex++)
			{
				reader.GoTo(tableMap.Loca + glyphIndex * bytesPerEntry);
				// If 2-byte format is used, the stored location is half of actual location (so multiply by 2)
				uint glyphDataOffset = isTwoByteEntry ? reader.ReadUInt16() * 2u : reader.ReadUInt32();
				allGlyphLocations[glyphIndex] = tableMap.Glyf + glyphDataOffset;
			}

			return allGlyphLocations;
		}

		// Create a lookup from unicode to font's internal glyph index
		static GlyphMap[] GetUnicodeToGlyphIndexMappings(FontReader reader, uint cmapOffset)
		{
			List<GlyphMap> glyphPairs = new();
			reader.GoTo(cmapOffset);

			uint version = reader.ReadUInt16();
			uint numSubtables = reader.ReadUInt16(); // font can contain multiple character maps for different platforms

			// --- Read through metadata for each character map to find the one we want to use ---
			uint cmapSubtableOffset = 0;
			int selectedUnicodeVersionID = -1;

			for (int i = 0; i < numSubtables; i++)
			{
				int platformID = reader.ReadUInt16();
				int platformSpecificID = reader.ReadUInt16();
				uint offset = reader.ReadUInt32();

				// Unicode encoding
				if (platformID == 0)
				{
					// Use highest supported unicode version
					if (platformSpecificID is 0 or 1 or 3 or 4 && platformSpecificID > selectedUnicodeVersionID)
					{
						cmapSubtableOffset = offset;
						selectedUnicodeVersionID = platformSpecificID;
					}
				}
				// Microsoft Encoding
				else if (platformID == 3 && selectedUnicodeVersionID == -1)
				{
					if (platformSpecificID is 1 or 10)
					{
						cmapSubtableOffset = offset;
					}
				}
			}

			if (cmapSubtableOffset == 0)
			{
				throw new Exception("Font does not contain supported character map type (TODO)");
			}

			// Go to the character map
			reader.GoTo(cmapOffset + cmapSubtableOffset);
			int format = reader.ReadUInt16();
			bool hasReadMissingCharGlyph = false;

			if (format != 12 && format != 4)
			{
				throw new Exception("Font cmap format not supported (TODO): " + format);
			}

			// ---- Parse Format 4 ----
			if (format == 4)
			{
				int length = reader.ReadUInt16();
				int languageCode = reader.ReadUInt16();
				// Number of contiguous segments of character codes
				int segCount2X = reader.ReadUInt16();
				int segCount = segCount2X / 2;
				reader.SkipBytes(6); // Skip: searchRange, entrySelector, rangeShift

				// Ending character code for each segment (last = 2^16 - 1)
				int[] endCodes = new int[segCount];
				for (int i = 0; i < segCount; i++)
				{
					endCodes[i] = reader.ReadUInt16();
				}

				reader.Skip16BitEntries(1); // Reserved pad

				int[] startCodes = new int[segCount];
				for (int i = 0; i < segCount; i++)
				{
					startCodes[i] = reader.ReadUInt16();
				}

				int[] idDeltas = new int[segCount];
				for (int i = 0; i < segCount; i++)
				{
					idDeltas[i] = reader.ReadUInt16();
				}

				(int offset, int readLoc)[] idRangeOffsets = new (int, int)[segCount];
				for (int i = 0; i < segCount; i++)
				{
					int readLoc = (int)reader.GetLocation();
					int offset = reader.ReadUInt16();
					idRangeOffsets[i] = (offset, readLoc);
				}

				for (int i = 0; i < startCodes.Length; i++)
				{
					int endCode = endCodes[i];
					int currCode = startCodes[i];

					if (currCode == 65535) break; // not sure about this (hack to avoid out of bounds on a specific font)

					while (currCode <= endCode)
					{
						int glyphIndex;
						// If idRangeOffset is 0, the glyph index can be calculated directly
						if (idRangeOffsets[i].offset == 0)
						{
							glyphIndex = (currCode + idDeltas[i]) % 65536;
						}
						// Otherwise, glyph index needs to be looked up from an array
						else
						{
							uint readerLocationOld = reader.GetLocation();
							int rangeOffsetLocation = idRangeOffsets[i].readLoc + idRangeOffsets[i].offset;
							int glyphIndexArrayLocation = 2 * (currCode - startCodes[i]) + rangeOffsetLocation;

							reader.GoTo(glyphIndexArrayLocation);
							glyphIndex = reader.ReadUInt16();

							if (glyphIndex != 0)
							{
								glyphIndex = (glyphIndex + idDeltas[i]) % 65536;
							}

							reader.GoTo(readerLocationOld);
						}

						glyphPairs.Add(new GlyphMap((uint)glyphIndex, (uint)currCode));
						hasReadMissingCharGlyph |= glyphIndex == 0;
						currCode++;
					}
				}
			}
			// ---- Parse Format 12 ----
			else if (format == 12)
			{
				reader.SkipBytes(10); // Skip: reserved, subtableByteLengthInlcudingHeader, languageCode
				uint numGroups = reader.ReadUInt32();

				for (int i = 0; i < numGroups; i++)
				{
					uint startCharCode = reader.ReadUInt32();
					uint endCharCode = reader.ReadUInt32();
					uint startGlyphIndex = reader.ReadUInt32();

					uint numChars = endCharCode - startCharCode + 1;
					for (int charCodeOffset = 0; charCodeOffset < numChars; charCodeOffset++)
					{
						uint charCode = (uint)(startCharCode + charCodeOffset);
						uint glyphIndex = (uint)(startGlyphIndex + charCodeOffset);

						glyphPairs.Add(new GlyphMap(glyphIndex, charCode));
						hasReadMissingCharGlyph |= glyphIndex == 0;
					}
				}
			}

			if (!hasReadMissingCharGlyph)
			{
				glyphPairs.Add(new GlyphMap(0, 65535));
			}

			return glyphPairs.ToArray();
		}

		static bool FlagBitIsSet(byte flag, int bitIndex) => ((flag >> bitIndex) & 1) == 1;
		static bool FlagBitIsSet(uint flag, int bitIndex) => ((flag >> bitIndex) & 1) == 1;

		public readonly struct GlyphMap
		{
			public readonly uint GlyphIndex;
			public readonly uint Unicode;

			public GlyphMap(uint index, uint unicode)
			{
				GlyphIndex = index;
				Unicode = unicode;
			}
		}

		public class GlyphRaw
		{
			public int AdvanceWidth;
			public int[] ContourEndIndices;
			public uint GlyphIndex;
			public int LeftSideBearing;
			public int MaxX;
			public int MaxY;

			public int MinX;
			public int MinY;
			public Point[] Points;
			public uint UnicodeValue;

			public int Width => MaxX - MinX;
			public int Height => MaxY - MinY;
		}

		public struct Point
		{
			public int X;
			public int Y;
			public bool OnCurve;

			public Point(int x, int y) : this()
			{
				X = x;
				Y = y;
			}

			public Point(int x, int y, bool onCurve)
			{
				X = x;
				Y = y;
				OnCurve = onCurve;
			}
		}

		public readonly struct TableMap
		{
			public readonly uint Cmap;
			public readonly uint Head;
			public readonly uint Maxp;
			public readonly uint Hhea;
			public readonly uint Hmtx;
			public readonly uint Loca;
			public readonly uint Glyf;
			public readonly uint Post;

			public TableMap(Dictionary<string, uint> map)
			{
				Cmap = map["cmap"];
				Head = map["head"];
				Maxp = map["maxp"];
				Hhea = map["hhea"];
				Hmtx = map["hmtx"];
				Loca = map["loca"];
				Glyf = map["glyf"];
				Post = map["post"];
			}
		}
	}
}