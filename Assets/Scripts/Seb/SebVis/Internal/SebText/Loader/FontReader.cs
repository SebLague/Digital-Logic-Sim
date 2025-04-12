using System;
using System.IO;
using System.Text;

namespace Seb.Vis.Text.FontLoading
{
	// BinaryReader wrapper for more convient reading of ttf files.
	public class FontReader : IDisposable
	{
		const byte ByteMask = 0b11111111;
		readonly bool convertToLittleEndian;
		public readonly BinaryReader reader;
		public readonly Stream stream;

		readonly StringBuilder stringBuilder;
		bool _isDisposed;

		public FontReader(string pathToFont)
		{
			stream = File.Open(pathToFont, FileMode.Open);
			reader = new BinaryReader(stream);
			convertToLittleEndian = BitConverter.IsLittleEndian;
			stringBuilder = new StringBuilder();
		}

		public FontReader(byte[] bytes)
		{
			stream = new MemoryStream(bytes);
			reader = new BinaryReader(stream);
			convertToLittleEndian = BitConverter.IsLittleEndian;
			stringBuilder = new StringBuilder();
		}


		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		public void Skip16BitEntries(int num) => SkipBytes(num * 2);
		public void Skip32BitEntries(int num) => SkipBytes(num * 4);
		public void SkipBytes(int numBytes) => reader.BaseStream.Seek(numBytes, SeekOrigin.Current);
		public void SkipBytes(uint numBytes) => reader.BaseStream.Seek(numBytes, SeekOrigin.Current);

		public void GoTo(uint byteOffsetFromOrigin, out uint prev)
		{
			prev = GetLocation();
			reader.BaseStream.Seek(byteOffsetFromOrigin, SeekOrigin.Begin);
		}

		public void GoTo(uint byteOffsetFromOrigin) => reader.BaseStream.Seek(byteOffsetFromOrigin, SeekOrigin.Begin);
		public void GoTo(int byteOffsetFromOrigin) => reader.BaseStream.Seek(byteOffsetFromOrigin, SeekOrigin.Begin);
		public void GoTo(long byteOffsetFromOrigin) => reader.BaseStream.Seek(byteOffsetFromOrigin, SeekOrigin.Begin);
		public uint GetLocation() => (uint)reader.BaseStream.Position;

		public string ReadString(int numBytes)
		{
			Span<char> tag = stackalloc char[numBytes];

			for (int i = 0; i < tag.Length; i++)
				tag[i] = (char)reader.ReadByte();

			return tag.ToString();
		}

		public string ReadTag() => ReadString(4);

		public double ReadFixedPoint2Dot14() => UInt16ToFixedPoint2Dot14(ReadUInt16());

		public static double UInt16ToFixedPoint2Dot14(UInt16 raw) => (Int16)raw / (double)(1 << 14);

		public byte ReadByte() => reader.ReadByte();

		public sbyte ReadSByte() => reader.ReadSByte();

		public Int32 ReadInt32() => (Int32)ReadUInt32();

		public Int32 ReadInt16() => (Int16)ReadUInt16();

		public UInt16 ReadUInt16()
		{
			UInt16 value = reader.ReadUInt16();
			if (convertToLittleEndian)
			{
				value = ToLittleEndian(value);
			}

			return value;
		}

		public UInt32 ReadUInt32()
		{
			UInt32 value = reader.ReadUInt32();
			if (convertToLittleEndian)
			{
				value = ToLittleEndian(value);
			}

			return value;
		}

		static UInt32 ToLittleEndian(UInt32 bigEndianValue)
		{
			UInt32 a = (bigEndianValue >> 24) & ByteMask;
			UInt32 b = (bigEndianValue >> 16) & ByteMask;
			UInt32 c = (bigEndianValue >> 8) & ByteMask;
			UInt32 d = (bigEndianValue >> 0) & ByteMask;
			return a | (b << 8) | (c << 16) | (d << 24);
		}

		static UInt16 ToLittleEndian(UInt16 bigEndianValue) => (UInt16)((bigEndianValue >> 8) | (bigEndianValue << 8));

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					stream.Dispose();
					reader.Dispose();
				}

				_isDisposed = true;
			}
		}
	}
}