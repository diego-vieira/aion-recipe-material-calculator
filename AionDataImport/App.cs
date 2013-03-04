﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Ionic.Zlib;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;
using Program.Properties;
using Ionic.Zip;

namespace Program
{
	#region class Utils

	public static class Utils
	{
		public static int ReadValuePackedS32 (this BinaryReader stream)
		{
			byte current = stream.ReadByte ();
			int value = 0;
			int shift = 0;

			while (current >= 0x80)
			{
				value |= (current & 0x7F) << shift;
				shift += 7;
				current = stream.ReadByte ();
			}

			return value | (current << shift);
		}

		public static int CalculateCrc32 (this byte[] data)
		{
			CRC32 crc = new CRC32 ();
			crc.SlurpBlock (data, 0, data.Length);

			return crc.Crc32Result;
		}

		public static string GetElementValue (this XElement node, string name)
		{
			XElement e = node.Element (name);
			if (e == null)
				return null;

			return e.Value;
		}

		public static T GetElementValue<T> (this XElement node, string name)
		{
			XElement e = node.Element (name);
			if (e == null)
				return default (T);

			return (T) Convert.ChangeType (e.Value, typeof (T), CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// (extension methos) Compares two strings with no case sensivity and invariant culture.
		/// </summary>
		/// <param name="a">String a.</param>
		/// <param name="b">String b.</param>
		/// <returns>True if strings are equal or false otherwise.</returns>
		public static bool CompareIgnoreCase (this string a, string b)
		{
			return string.Compare (a, b, StringComparison.InvariantCultureIgnoreCase) == 0;
		}

		/// <summary>
		/// Extends <see cref="IDictionary.TryGetValue"/> with default return value.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static TValue TryGetValue<TKey, TValue> (this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
		{
			TValue res;

			if (!dictionary.TryGetValue (key, out res))
				return defaultValue;

			return res;
		}
	}

	#endregion

	internal class App
	{
		#region struct PakFileHeader

		private struct PakFileHeader
		{
			public byte Version { get; set; }
			public byte System { get; set; }
			public ushort Flags { get; set; }
			public ushort CompressionMethod { get; set; }
			public ushort LastModificationTime { get; set; }
			public ushort LastModificationDate { get; set; }
			public int Crc { get; set; }
			public uint CompressedSize { get; set; }
			public uint UncompressedSize { get; set; }
			public ushort FilenameLength { get; set; }
			public ushort ExtraFieldLength { get; set; }

			public static readonly int Size = Marshal.SizeOf (typeof (PakFileHeader));

			public PakFileHeader (BinaryReader f)
				: this ()
			{
				this.Version = f.ReadByte ();
				this.System = f.ReadByte ();
				this.Flags = f.ReadUInt16 ();
				this.CompressionMethod = f.ReadUInt16 ();
				this.LastModificationTime = f.ReadUInt16 ();
				this.LastModificationDate = f.ReadUInt16 ();
				this.Crc = f.ReadInt32 ();
				this.CompressedSize = f.ReadUInt32 ();
				this.UncompressedSize = f.ReadUInt32 ();
				this.FilenameLength = f.ReadUInt16 ();
				this.ExtraFieldLength = f.ReadUInt16 ();
			}
		}

		#endregion

		#region struct PakFileCentralDirectory

		private struct PakFileCentralDirectory
		{
			public byte Version { get; set; }
			public byte System { get; set; }
			public byte ExtractVersion { get; set; }
			public byte ExtractSystem { get; set; }
			public ushort Flags { get; set; }
			public ushort CompressionMethod { get; set; }
			public ushort LastModificationTime { get; set; }
			public ushort LastModificationDate { get; set; }
			public int Crc { get; set; }
			public uint CompressedSize { get; set; }
			public uint UncompressedSize { get; set; }
			public ushort FilenameLength { get; set; }
			public ushort ExtraFieldLength { get; set; }
			public ushort CommentLength { get; set; }
			public ushort DiskNumberStart { get; set; }
			public ushort InternalFileAttributes { get; set; }
			public uint ExternalFileAttributes { get; set; }
			public int LocalHeaderOffset { get; set; }

			public static readonly int Size = Marshal.SizeOf (typeof (PakFileCentralDirectory));

			public PakFileCentralDirectory (BinaryReader f)
				: this ()
			{
				this.Version = f.ReadByte ();
				this.System = f.ReadByte ();
				this.ExtractVersion = f.ReadByte ();
				this.ExtractSystem = f.ReadByte ();
				this.Flags = f.ReadUInt16 ();
				this.CompressionMethod = f.ReadUInt16 ();
				this.LastModificationTime = f.ReadUInt16 ();
				this.LastModificationDate = f.ReadUInt16 ();
				this.Crc = f.ReadInt32 ();
				this.CompressedSize = f.ReadUInt32 ();
				this.UncompressedSize = f.ReadUInt32 ();
				this.FilenameLength = f.ReadUInt16 ();
				this.ExtraFieldLength = f.ReadUInt16 ();
				this.CommentLength = f.ReadUInt16 ();
				this.DiskNumberStart = f.ReadUInt16 ();
				this.InternalFileAttributes = f.ReadUInt16 ();
				this.ExternalFileAttributes = f.ReadUInt32 ();
				this.LocalHeaderOffset = f.ReadInt32 ();
			}
		}

		#endregion

		#region Keys

		private class KeyInfo
		{
			public int Version { get; set; }
			public int XorModificator { get; set; }
			public int XorMultiplier { get; set; }
			public byte[] Key { get; set; }
		}

		private readonly KeyInfo[] Keys = new KeyInfo[]
		{
			new KeyInfo
			{
				Version = 1,
				XorModificator = 0x1F,
				XorMultiplier = 32,
				Key = new byte[]
				{
					0x2f, 0x5d, 0x51, 0xf7, 0x01, 0xe9, 0xb4, 0x93, 0x4e, 0x51, 0x81, 0x3e, 0xaf, 0x3f, 0xdf, 0x99,
					0x80, 0x5e, 0x13, 0x83, 0x9b, 0x46, 0x57, 0xb5, 0x1b, 0x5c, 0xec, 0xb1, 0x29, 0x7c, 0xa9, 0x31,
					0x68, 0xe5, 0xda, 0xa7, 0xf6, 0x4f, 0xae, 0x16, 0x9a, 0x7f, 0x03, 0xcf, 0x1d, 0x5e, 0xd0, 0x51,
					0x5a, 0xe5, 0x02, 0xd9, 0x11, 0xd0, 0xfb, 0xf4, 0xf8, 0x7c, 0xa2, 0x88, 0x26, 0xd8, 0x1f, 0xa2,
					0x43, 0xda, 0x33, 0xa9, 0xac, 0x4e, 0x5a, 0x0d, 0xed, 0x78, 0x86, 0x2d, 0xb2, 0x6a, 0xc4, 0x9b,
					0xaa, 0x77, 0x85, 0x57, 0x6a, 0xa6, 0xd8, 0x35, 0xd8, 0x97, 0x6b, 0x17, 0x24, 0xb7, 0x7a, 0x1d,
					0xd3, 0x3b, 0x9e, 0x79, 0xf2, 0xae, 0x9f, 0x01, 0xe6, 0x9d, 0x29, 0x40, 0xed, 0x2f, 0x9c, 0x16,
					0xda, 0x18, 0xd1, 0x99, 0x0e, 0xd4, 0x0a, 0x63, 0x2d, 0x92, 0xd7, 0xeb, 0xb4, 0xa7, 0x50, 0x21,
					0xd8, 0x0f, 0x45, 0xd6, 0xc6, 0xbf, 0xcc, 0x47, 0xcc, 0x59, 0xed, 0x3e, 0x71, 0xfe, 0xa0, 0x26,
					0xfc, 0xd1, 0x07, 0x85, 0x8a, 0xee, 0x12, 0x36, 0x11, 0x5a, 0x60, 0xe1, 0x8f, 0xbd, 0x9e, 0xf7,
					0xb6, 0x64, 0x39, 0xcd, 0x49, 0x5a, 0x9a, 0xf7, 0x90, 0x1c, 0xc1, 0xa2, 0x0b, 0xb3, 0x81, 0xf7,
					0xca, 0xb8, 0x2a, 0x4b, 0x95, 0x13, 0xdc, 0x2e, 0x4a, 0xe5, 0x64, 0x16, 0x94, 0x99, 0xc9, 0xb1,
					0x7b, 0x53, 0x76, 0xae, 0xc4, 0xdf, 0x26, 0xf7, 0xc8, 0x5f, 0x78, 0x31, 0xae, 0xaf, 0x5a, 0x7f,
					0xa4, 0xe7, 0x29, 0x5e, 0x0e, 0xe2, 0xbb, 0x91, 0x41, 0x32, 0x2c, 0xf0, 0xce, 0x60, 0x9e, 0x27,
					0xdc, 0xfa, 0xdc, 0x13, 0xac, 0x37, 0xf7, 0xf1, 0xb4, 0xa4, 0xcd, 0xf4, 0x7a, 0xdc, 0xa9, 0x7b,
					0x95, 0x82, 0xda, 0x7d, 0xfb, 0x8d, 0x6b, 0x6e, 0x0c, 0x43, 0xe7, 0x23, 0x6c, 0xc0, 0x53, 0xf9,
					0x39, 0x82, 0x38, 0xde, 0x9b, 0xd0, 0xfe, 0x57, 0x3d, 0x75, 0x65, 0x43, 0xb0, 0xae, 0x5a, 0x6e,
					0x4e, 0xb3, 0xfb, 0xae, 0x8c, 0xc4, 0x0f, 0x9b, 0x65, 0x27, 0xaf, 0xa2, 0xc6, 0xf1, 0x84, 0x91,
					0x94, 0x1a, 0x39, 0x39, 0x53, 0xa5, 0x90, 0x64, 0xf0, 0x62, 0xcc, 0xb5, 0xbf, 0x1e, 0xbc, 0xa7,
					0x28, 0xae, 0x33, 0x3f, 0x16, 0xc6, 0x30, 0xb7, 0xb1, 0xf2, 0x83, 0xb1, 0x5e, 0xb0, 0x37, 0x20,
					0x9d, 0xf7, 0x7b, 0x95, 0xbe, 0x35, 0x6e, 0x1b, 0x07, 0x05, 0x77, 0x32, 0x3a, 0xae, 0x8a, 0x39,
					0x25, 0xaf, 0x10, 0xc5, 0x18, 0x56, 0xc2, 0x2b, 0xf9, 0xc4, 0x4b, 0xd6, 0xdc, 0x44, 0xd7, 0x9d,
					0xa8, 0x5c, 0x7f, 0xad, 0xef, 0x88, 0xbc, 0x46, 0x5f, 0xfe, 0xc0, 0xe3, 0xde, 0x69, 0xe3, 0x03,
					0xed, 0xf8, 0x06, 0x1f, 0x38, 0xc1, 0x22, 0x23, 0xf4, 0xc1, 0xd7, 0xe1, 0x11, 0x7b, 0x3c, 0xcb,
					0xb4, 0x8d, 0xaf, 0x82, 0x23, 0x30, 0x0d, 0x78, 0x82, 0xf9, 0xed, 0x3e, 0x91, 0xe1, 0x52, 0xa7,
					0xd5, 0xd5, 0x75, 0x71, 0x46, 0xda, 0x11, 0x97, 0xfb, 0x16, 0xdf, 0xea, 0xf3, 0xab, 0xa0, 0x32,
					0x66, 0xdb, 0x5e, 0x5e, 0xb9, 0x43, 0x55, 0x0e, 0x9e, 0xa5, 0x2a, 0xfd, 0x5e, 0x31, 0xc6, 0x93,
					0xd4, 0x9a, 0xa2, 0x2b, 0x37, 0x00, 0xb9, 0x46, 0x13, 0xf7, 0x05, 0x51, 0xa7, 0xb2, 0xaa, 0x22,
					0x0c, 0x9d, 0xc5, 0xd2, 0x3d, 0x62, 0xf4, 0x28, 0x8c, 0xbc, 0x89, 0x25, 0x79, 0xfa, 0x9a, 0xfd,
					0x8d, 0xa1, 0xbc, 0x02, 0x2b, 0x15, 0xb0, 0xb6, 0xe6, 0xa4, 0xcd, 0xbc, 0x72, 0xf8, 0x68, 0xb4,
					0x9a, 0x33, 0x08, 0xba, 0x62, 0xb7, 0xb1, 0xb1, 0xca, 0x00, 0x08, 0x01, 0x40, 0x68, 0x8e, 0xe1,
					0x49, 0x4f, 0xd8, 0xf2, 0x67, 0x85, 0xf0, 0x37, 0xc9, 0x61, 0xab, 0x1e, 0xc6, 0x6a, 0x4d, 0xca,
					0xaf, 0x03, 0x2f, 0x36, 0x02, 0xf0, 0xbc, 0x5e, 0x81, 0x39, 0x8a, 0x25, 0x38, 0x2c, 0xca, 0x04,
					0xf9, 0x0d, 0xf6, 0x44, 0x5b, 0x46, 0xdb, 0xde, 0xb7, 0x7b, 0xf4, 0xac, 0x3b, 0x7f, 0x36, 0x0d,
					0x90, 0x7c, 0x2c, 0xb0, 0x20, 0x48, 0xab, 0xa9, 0x7f, 0x39, 0xdb, 0x6d, 0x0b, 0x80, 0xe2, 0xf1,
					0x37, 0x50, 0xfa, 0x83, 0x9d, 0xd3, 0x3e, 0x8c, 0x54, 0x48, 0xeb, 0xe7, 0x92, 0x34, 0x6a, 0xeb,
					0x2b, 0x18, 0xda, 0xda, 0xe5, 0x7c, 0x7e, 0xd3, 0x3d, 0xd9, 0xb1, 0xfd, 0x90, 0x28, 0xcd, 0x00,
					0x45, 0x93, 0xb3, 0x86, 0xeb, 0x32, 0x4b, 0xe6, 0xea, 0x24, 0xb6, 0x97, 0xb4, 0x11, 0x94, 0xa0,
					0x16, 0x53, 0xfb, 0xae, 0xa6, 0xd7, 0x9a, 0xe9, 0xd9, 0xfb, 0xa6, 0x41, 0xc2, 0x6d, 0xec, 0x4b,
					0x0b, 0x59, 0xd7, 0x6c, 0x2e, 0xec, 0x9b, 0x5d, 0x6f, 0x76, 0x66, 0xcb, 0xb0, 0x23, 0xca, 0x2c,
					0x8d, 0xb6, 0x3a, 0x6e, 0xdc, 0x29, 0xd1, 0xbd, 0x1d, 0x89, 0x3f, 0xeb, 0xc7, 0x22, 0x09, 0xb8,
					0x1d, 0x2e, 0x04, 0x98, 0x71, 0x1a, 0x35, 0x26, 0x7d, 0xaa, 0xf2, 0xdb, 0xc0, 0x01, 0x8a, 0x56,
					0x76, 0xd1, 0x27, 0xa3, 0x2b, 0xc8, 0x58, 0xea, 0x76, 0x72, 0xe6, 0xf9, 0xea, 0xa0, 0x54, 0xf4,
					0xb2, 0xa4, 0xc0, 0xbb, 0xec, 0x54, 0x81, 0x3f, 0x58, 0x37, 0x3c, 0x69, 0x45, 0xc8, 0xb7, 0xb1,
					0x60, 0x3b, 0x3d, 0x20, 0x5b, 0x97, 0xce, 0xd2, 0xfc, 0xb1, 0xf2, 0xaf, 0xa2, 0xcb, 0x67, 0x74,
					0xad, 0x58, 0x79, 0xc8, 0xfe, 0xc1, 0x54, 0x71, 0xea, 0x98, 0x0b, 0x59, 0xc6, 0x21, 0xa0, 0x94,
					0x7f, 0x91, 0xde, 0xfd, 0x61, 0xfc, 0x3c, 0xa1, 0x71, 0x47, 0x9f, 0x97, 0x89, 0x0d, 0x43, 0x74,
					0x97, 0xec, 0x85, 0xfe, 0x2e, 0x0d, 0xe7, 0x49, 0xca, 0x55, 0x0e, 0xdd, 0xf4, 0x38, 0xf8, 0x22,
					0xb1, 0x7e, 0x55, 0x9e, 0x56, 0xea, 0x0f, 0x4a, 0x3a, 0x3d, 0x0f, 0x86, 0x64, 0x57, 0x51, 0xf9,
					0xa3, 0x0c, 0x23, 0xe4, 0x2a, 0x6a, 0xdf, 0x20, 0x31, 0xf8, 0xdd, 0x6d, 0xa8, 0xc4, 0xdf, 0x42,
					0x7d, 0xae, 0xd2, 0xac, 0x7d, 0xd7, 0x1f, 0x85, 0x67, 0xa4, 0x4f, 0x97, 0x21, 0x25, 0x61, 0xd0,
					0xa9, 0x6b, 0x77, 0x47, 0xc7, 0x97, 0x47, 0x13, 0x03, 0x1a, 0xfa, 0xc8, 0xe2, 0x05, 0xd7, 0xa6,
					0x0e, 0xda, 0x71, 0x18, 0x42, 0xc5, 0xaa, 0xd8, 0xb0, 0x96, 0x53, 0x2f, 0xd3, 0x78, 0xad, 0x8f,
					0x2b, 0xc4, 0x91, 0x3b, 0x07, 0xd7, 0x90, 0x09, 0xcb, 0x55, 0xcc, 0xf7, 0xcc, 0xbd, 0xcf, 0xc5,
					0x3b, 0xc1, 0x34, 0x1d, 0x35, 0x3c, 0x59, 0x8d, 0x75, 0x35, 0xf7, 0xf7, 0xb7, 0xdb, 0xd6, 0x90,
					0x53, 0xdb, 0x66, 0x20, 0x0e, 0xf7, 0x98, 0xb0, 0xbd, 0x51, 0xa4, 0x49, 0xb4, 0x3f, 0x1d, 0xe2,
					0x82, 0x2b, 0x04, 0x3c, 0x13, 0x4b, 0x39, 0xb6, 0xbd, 0xa8, 0x00, 0xe7, 0x33, 0x60, 0xe5, 0xfa,
					0xf1, 0x7b, 0xd5, 0x9b, 0x2b, 0x4c, 0x9f, 0x81, 0xb6, 0xb9, 0xb8, 0x55, 0x16, 0x5f, 0x7a, 0x05,
					0x07, 0xe6, 0xb3, 0x3e, 0xbc, 0x8b, 0xc3, 0x2f, 0x37, 0x23, 0x19, 0x39, 0xd1, 0xa2, 0x4c, 0xba,
					0x81, 0x78, 0xa3, 0x99, 0xd3, 0xb0, 0x53, 0xb9, 0x38, 0x44, 0x2b, 0xfc, 0x8f, 0x7b, 0x0f, 0xfe,
					0x99, 0xca, 0xfb, 0x37, 0x3e, 0x1d, 0xd4, 0x99, 0x3c, 0xdd, 0xd5, 0x6f, 0x48, 0xc2, 0xe1, 0x83,
					0x23, 0xab, 0x7f, 0x52, 0xa9, 0x89, 0xc4, 0x61, 0x6f, 0xae, 0x02, 0x66, 0xe9, 0x7a, 0x67, 0x67,
					0xad, 0xb7, 0x80, 0x7f, 0xc8, 0xa8, 0xb5, 0x61, 0xc9, 0x1a, 0xb3, 0x57, 0x73, 0x6c, 0xe9, 0xd3,
					0xa0, 0xfa, 0xfe, 0x43, 0x70, 0xc3, 0x71, 0x46, 0x2e, 0xbe, 0x2e, 0x02, 0x17, 0xca, 0x78, 0xa0
				}
			},
			new KeyInfo
			{
				Version = 2,
				XorModificator = 0x3FF,
				XorMultiplier = 1,
				Key = new byte[]
				{
					0x86, 0xFA, 0x1A, 0x1C, 0x07, 0xBD, 0xD8, 0x64, 0xCE, 0xEE, 0x59, 0x88, 0xCD, 0xA9, 0x1D, 0x06,
					0xF7, 0x3D, 0x31, 0x58, 0x83, 0xA1, 0x5C, 0x7E, 0xDF, 0xA6, 0x50, 0x9E, 0x89, 0xA8, 0x12, 0xD2,
					0x25, 0x49, 0x75, 0xE2, 0x07, 0x0F, 0xEB, 0x01, 0x97, 0x4A, 0x66, 0x35, 0xAB, 0x32, 0x9D, 0xA7,
					0x4E, 0xA2, 0x89, 0x62, 0x0F, 0x55, 0x41, 0xC5, 0x52, 0x10, 0x1F, 0x47, 0xB0, 0xA0, 0x63, 0xA6,
					0xF0, 0x1C, 0x1C, 0x4C, 0x9B, 0x3C, 0xAC, 0xE2, 0xB3, 0x4E, 0x9F, 0xF1, 0xA4, 0x91, 0x29, 0x82,
					0xE4, 0x76, 0x0D, 0x8D, 0x4F, 0xA3, 0x34, 0x4A, 0xCC, 0x1C, 0xC7, 0x18, 0x48, 0x8E, 0xFE, 0x18,
					0x79, 0x08, 0x87, 0x28, 0x8E, 0x24, 0xB7, 0x6B, 0x38, 0xF2, 0x58, 0x01, 0x2D, 0xA8, 0x58, 0x0E,
					0x9C, 0x54, 0x29, 0xCF, 0xA1, 0xAE, 0x0A, 0xD2, 0x3B, 0x4A, 0x10, 0xF8, 0xD8, 0x19, 0x31, 0x7D,
					0xF3, 0xAE, 0x1B, 0x90, 0xD2, 0x2F, 0x16, 0xC7, 0xE5, 0x3B, 0xCC, 0xEF, 0xE1, 0xE1, 0x2C, 0x86,
					0x00, 0xDD, 0x35, 0x67, 0x8D, 0x25, 0xFC, 0xED, 0x32, 0x1F, 0xA9, 0x1A, 0x12, 0x6D, 0xB0, 0xF7,
					0x3D, 0xB6, 0x1F, 0xE8, 0x81, 0x4D, 0x36, 0xE7, 0x25, 0x30, 0x21, 0x90, 0x86, 0x30, 0x0E, 0xEE,
					0x40, 0xBE, 0x6E, 0xDA, 0xC1, 0x3A, 0xAF, 0xF2, 0xEC, 0x28, 0x2C, 0xF1, 0xCD, 0x44, 0x98, 0x72,
					0xDA, 0xCD, 0xC6, 0xD9, 0xDF, 0xF7, 0xEE, 0x88, 0x04, 0xE1, 0x62, 0x00, 0x08, 0x0E, 0xCD, 0x16,
					0x37, 0xAB, 0xF9, 0xF5, 0x14, 0xAA, 0x2E, 0x00, 0x4E, 0xF8, 0x18, 0x41, 0x0B, 0xD9, 0x6F, 0x9B,
					0xFA, 0xAD, 0x2B, 0x54, 0x56, 0x2E, 0x7F, 0x2C, 0x3B, 0x6A, 0x82, 0xA1, 0x7C, 0x7C, 0xA6, 0x8F,
					0x66, 0x5E, 0xE7, 0xCF, 0x83, 0xB9, 0xEA, 0xFC, 0xE2, 0x31, 0xD4, 0x10, 0xF3, 0xF4, 0x22, 0xEC,
					0x73, 0x14, 0x4F, 0x94, 0x78, 0x79, 0x8F, 0x1E, 0x29, 0xEA, 0x5F, 0x21, 0x1E, 0x08, 0x37, 0xB8,
					0xF6, 0x9A, 0x2D, 0xC5, 0x36, 0x34, 0xC1, 0x97, 0xDC, 0x75, 0xB2, 0xAD, 0xD7, 0xE3, 0x04, 0xA7,
					0xC0, 0xC9, 0x1C, 0x1A, 0x00, 0xE9, 0x2D, 0x6F, 0xD6, 0x8D, 0xBC, 0x73, 0x52, 0xC0, 0x8A, 0xB6,
					0xBA, 0x2C, 0xA6, 0x7D, 0x7B, 0x6F, 0xF4, 0x47, 0x1A, 0x72, 0xE9, 0xB2, 0x30, 0x7D, 0xD4, 0xD3,
					0x09, 0x9C, 0x65, 0xB0, 0xD0, 0x17, 0xCF, 0xFC, 0xF2, 0xFF, 0x46, 0xD2, 0xA6, 0x43, 0x11, 0x76,
					0x2B, 0xE5, 0x1D, 0xE5, 0xC9, 0x47, 0x2F, 0x4B, 0x1B, 0xDD, 0x9A, 0xFD, 0x9D, 0x20, 0xB6, 0x43,
					0x1A, 0x64, 0xE3, 0x68, 0xF3, 0x21, 0x57, 0x68, 0xD4, 0x04, 0x8F, 0xC3, 0xCE, 0xAF, 0xA3, 0xAB,
					0x69, 0xA3, 0x3C, 0x34, 0xBE, 0x1F, 0x84, 0xA8, 0x0E, 0x74, 0xCB, 0xB7, 0xE6, 0xB1, 0x39, 0x8D,
					0x68, 0x00, 0x3A, 0x9B, 0x9C, 0xB1, 0x09, 0x1C, 0x7D, 0x52, 0x15, 0x12, 0xA6, 0xB0, 0x83, 0xD3,
					0x40, 0x47, 0x9B, 0xE4, 0x22, 0xE3, 0x6E, 0x30, 0xC4, 0xFC, 0x6F, 0x4F, 0xFE, 0x9F, 0x51, 0x14,
					0x13, 0x57, 0xF1, 0xEB, 0x25, 0xF7, 0x95, 0x4C, 0x92, 0xB6, 0x3C, 0xD0, 0x34, 0x79, 0x59, 0x33,
					0x20, 0xBE, 0xB8, 0xBF, 0xE0, 0x0A, 0xD2, 0x77, 0xBC, 0x43, 0x5C, 0x7D, 0xFC, 0xE1, 0x59, 0x00,
					0xDE, 0x5A, 0x7D, 0x44, 0x11, 0xAC, 0x13, 0xF2, 0x64, 0x84, 0x4F, 0x5D, 0xA2, 0xC4, 0x36, 0xD7,
					0x23, 0xFA, 0xF8, 0xD1, 0x14, 0x8D, 0xF9, 0xDD, 0x17, 0x1D, 0x52, 0x41, 0x22, 0xF5, 0x1A, 0x42,
					0x39, 0xFE, 0x36, 0xD5, 0x0A, 0x10, 0x01, 0xD2, 0xEA, 0x12, 0x82, 0x5A, 0x48, 0xD2, 0x94, 0x95,
					0x0A, 0xF7, 0xAB, 0x70, 0xF7, 0xF2, 0x98, 0x89, 0xA1, 0x68, 0xF9, 0xE1, 0xD6, 0xE1, 0xBD, 0x92,
					0x38, 0x45, 0x5F, 0x19, 0xE2, 0xEA, 0x46, 0x76, 0xC5, 0xC3, 0xF2, 0xB4, 0x9F, 0x70, 0x53, 0x09,
					0x3F, 0xB8, 0x06, 0x3A, 0xF3, 0x46, 0xC8, 0x6A, 0xCD, 0x0A, 0xE3, 0xF0, 0xAA, 0x34, 0xD9, 0x72,
					0x98, 0x34, 0x23, 0xD1, 0x96, 0x8C, 0x32, 0x32, 0x3B, 0x00, 0xA3, 0x9E, 0x4F, 0xED, 0xBC, 0x97,
					0xD4, 0x4A, 0x26, 0x15, 0x96, 0x1D, 0x0E, 0x36, 0xB8, 0xEE, 0x86, 0x45, 0x57, 0x04, 0x6D, 0x2B,
					0xC0, 0xDB, 0x91, 0x0A, 0x46, 0xCE, 0x7C, 0x1F, 0x3C, 0x3A, 0x81, 0x94, 0x22, 0x26, 0x82, 0x6D,
					0x83, 0xBD, 0x13, 0x2D, 0x96, 0x91, 0x53, 0x6C, 0x26, 0x0C, 0x44, 0xFE, 0xBD, 0xEE, 0xDA, 0xCC,
					0xBD, 0x52, 0xA6, 0x11, 0x3E, 0x10, 0x42, 0x20, 0x60, 0xEB, 0x5F, 0x5B, 0x0D, 0x7C, 0xBB, 0x80,
					0xAC, 0x2F, 0xB9, 0xF9, 0xD2, 0x4A, 0xEB, 0x54, 0x80, 0x60, 0x62, 0x85, 0xE5, 0x1A, 0xF0, 0x30,
					0x45, 0xB7, 0x44, 0x82, 0xEF, 0x3A, 0x0C, 0xE0, 0xE5, 0x94, 0xFA, 0xFD, 0x2E, 0xD9, 0xEB, 0x8D,
					0x5A, 0xC2, 0xEF, 0x39, 0x51, 0x71, 0x92, 0xFA, 0xDB, 0xEF, 0x14, 0x88, 0x00, 0xFF, 0xE3, 0xF6,
					0xB5, 0x34, 0x34, 0x40, 0xF5, 0xBB, 0xC8, 0xD3, 0xB5, 0xBD, 0xF6, 0xCF, 0xC7, 0xB1, 0xF9, 0x18,
					0x3D, 0xA2, 0x74, 0xEF, 0x40, 0xBC, 0x6B, 0x39, 0xF2, 0xC8, 0x6E, 0x00, 0x64, 0x78, 0x52, 0x88,
					0x13, 0xF4, 0x27, 0x74, 0x14, 0x8F, 0xCE, 0x34, 0x5E, 0xF9, 0xE0, 0x6D, 0x47, 0xFC, 0x38, 0x6D,
					0xB0, 0x03, 0xED, 0x6C, 0xF6, 0x68, 0x00, 0xAC, 0x2B, 0xFE, 0x73, 0x2C, 0x94, 0x9E, 0x3F, 0x17,
					0x0C, 0x33, 0xB9, 0x8F, 0x33, 0x34, 0xDE, 0x05, 0x18, 0xE1, 0x2B, 0xB9, 0x42, 0x3F, 0x5F, 0xA2,
					0xB4, 0x1E, 0xE9, 0x45, 0xF3, 0x38, 0x43, 0xBB, 0x8E, 0xB0, 0x0A, 0x94, 0x39, 0xEE, 0xFF, 0x9A,
					0xF4, 0x2D, 0x6C, 0x4B, 0x66, 0xB1, 0x1E, 0x0F, 0xC2, 0x18, 0x32, 0xE1, 0x74, 0xFF, 0x90, 0x94,
					0xF2, 0x38, 0xDD, 0x56, 0xDC, 0x78, 0x91, 0x96, 0xD1, 0x04, 0x03, 0x09, 0x21, 0x39, 0xB2, 0xD4,
					0xCC, 0x2A, 0xA8, 0xAB, 0xE8, 0x99, 0x1C, 0xE7, 0xE4, 0x43, 0x3B, 0x58, 0xC1, 0x59, 0x54, 0xE8,
					0xBD, 0x9C, 0x28, 0xC6, 0x81, 0xFC, 0xAD, 0x33, 0x4F, 0x24, 0x16, 0xA0, 0x47, 0xD1, 0x4C, 0x4D,
					0x39, 0x7A, 0xC1, 0xF7, 0x1D, 0x04, 0xCF, 0xE7, 0xAE, 0x17, 0x71, 0xD9, 0x37, 0xDC, 0x9C, 0x0A,
					0x0E, 0x9D, 0x0E, 0x04, 0xD7, 0x24, 0xC1, 0x50, 0x0C, 0x49, 0xE3, 0xBC, 0xCA, 0x98, 0x89, 0x55,
					0x86, 0x73, 0xF1, 0xC3, 0x8D, 0x8F, 0x99, 0x34, 0xF7, 0x4B, 0xE7, 0x69, 0x0A, 0xB0, 0xC1, 0x2F,
					0x85, 0x97, 0xBF, 0xC3, 0xFD, 0xD0, 0x62, 0x75, 0xB1, 0xAD, 0xF3, 0x04, 0xF3, 0xF3, 0x77, 0x06,
					0xAA, 0x77, 0x5A, 0xE7, 0xEB, 0x67, 0x3F, 0xB5, 0x40, 0xA1, 0x9C, 0x53, 0x96, 0xFD, 0x85, 0x53,
					0x6E, 0xED, 0x52, 0x05, 0x3B, 0x6E, 0x89, 0xEF, 0x95, 0x98, 0xB6, 0x66, 0x34, 0xD0, 0x8A, 0x3F,
					0x44, 0xEA, 0x06, 0x86, 0x13, 0x39, 0xEF, 0x20, 0xAD, 0xE4, 0x73, 0x2C, 0x61, 0x77, 0x10, 0x3D,
					0xB9, 0x0B, 0xC2, 0x0C, 0xFD, 0xF2, 0x99, 0xD8, 0xB1, 0x57, 0x83, 0x1B, 0x24, 0xA6, 0xA0, 0xAB,
					0x97, 0x3E, 0xE5, 0x09, 0x07, 0x3F, 0x43, 0xED, 0x12, 0xE3, 0x36, 0xCE, 0x16, 0x58, 0xF2, 0x78,
					0x00, 0x63, 0xF7, 0x67, 0xDC, 0xD9, 0x5F, 0x0D, 0xAA, 0x3E, 0x9A, 0xA3, 0x83, 0x72, 0xFE, 0xBA,
					0x92, 0xE9, 0xD4, 0x22, 0xF0, 0x38, 0x38, 0x61, 0xE2, 0x79, 0x9B, 0x5E, 0x8A, 0x62, 0x27, 0x59,
					0x84, 0x71, 0xC0, 0xEB, 0x95, 0x28, 0x0D, 0x34, 0xCB, 0xAB, 0x25, 0xC6, 0x3B, 0xBC, 0x52, 0xA5,
					0xCA, 0x6B, 0x93, 0xCA, 0x23, 0x6D, 0x35, 0x87, 0x41, 0x87, 0x3E, 0x48, 0xB9, 0xDF, 0x0E, 0xFD,
					0x30, 0xB8, 0xD1, 0xB8, 0x10, 0x68, 0x3D, 0xBC, 0x09, 0x04, 0x31, 0x94, 0x5C, 0x91, 0xAF, 0x6C
				}
			}
		};

		#endregion

		#region class BinaryXml

		private class BinaryXml
		{
			private byte[] stringTableData;
			private Dictionary<int, string> stringTableCache;

			public XDocument ReadXml (BinaryReader f)
			{
				if (f.ReadByte () != 0x80)
					throw new InvalidOperationException ("Invalid binary XML file");

				// read string table
				int size = f.ReadValuePackedS32 ();
				this.stringTableData = f.ReadBytes (size);
				this.stringTableCache = new Dictionary<int, string> ();

				return new XDocument (this.ReadElement (f));
			}

			private string GetString (int index)
			{
				if (index == 0)
					return string.Empty;

				string res;
				if (!this.stringTableCache.TryGetValue (index, out res))
				{
					int offset = index * 2;
					int k = offset;
					while (true)
					{
						if (this.stringTableData[k] == 0 && this.stringTableData[k + 1] == 0)
							break;

						k += 2;
					}

					res = Encoding.Unicode.GetString (this.stringTableData, offset, k - offset);
					this.stringTableCache.Add (index, res);
				}

				return res;
			}

			private XElement ReadElement (BinaryReader f)
			{
				XElement res = new XElement (this.GetString (f.ReadValuePackedS32 ()));

				byte flags = f.ReadByte ();
				string value = string.Empty;

				// value
				if ((flags & 0x01) == 1)
				{
					res.Value = this.GetString (f.ReadValuePackedS32 ());
				}

				// attributes
				if ((flags & 0x02) == 2)
				{
					int count = f.ReadValuePackedS32 ();
					for (int k = 0; k < count; k++)
						res.Add (new XAttribute (this.GetString (f.ReadValuePackedS32 ()), this.GetString (f.ReadValuePackedS32 ())));
				}

				// child nodes
				if ((flags & 0x04) == 4)
				{
					int count = f.ReadValuePackedS32 ();
					for (int k = 0; k < count; k++)
						res.Add (this.ReadElement (f));
				}

				return res;
			}
		}

		#endregion

		#region Helpers

		public byte[] ExtractFile (string pakFileName, string fileName)
		{
			fileName = fileName.ToLowerInvariant ();

			using (FileStream file_stream = new FileStream (pakFileName, FileMode.Open, FileAccess.Read))
			using (BinaryReader f = new BinaryReader (file_stream))
			{
				KeyInfo current_key = null;

				while (f.BaseStream.Position < f.BaseStream.Length)
				{
					uint block_id = f.ReadUInt32 ();

					switch (block_id)
					{
						// file
						case 0xFBFCB4AF:
							{
								if (f.BaseStream.Position + PakFileHeader.Size > f.BaseStream.Length)
									break;

								PakFileHeader header = new PakFileHeader (f);
								string file_name = Encoding.Default.GetString (f.ReadBytes (header.FilenameLength));

								file_name = file_name.Replace ("/", "\\").ToLowerInvariant ();

								//Program.Log (file_name);

								if (header.ExtraFieldLength > 0)
									f.BaseStream.Seek (header.ExtraFieldLength, SeekOrigin.Current);

								if (header.CompressedSize == 0)
									continue;

								if (file_name != fileName)
								{
									f.BaseStream.Seek (header.CompressedSize, SeekOrigin.Current);
									continue;
								}

								byte[] uncompressed_data = null;

								if (header.UncompressedSize > int.MaxValue)
									throw new NotSupportedException ("Data size more than " + int.MaxValue + " not supported.");

								byte[] data = f.ReadBytes ((int) header.CompressedSize);

								if (current_key == null)
								{
									// detect version
									foreach (KeyInfo key in this.Keys)
									{
										int xor_size = 32;
										if (xor_size > data.Length)
											xor_size = data.Length;

										int table_offset = (data.Length & key.XorModificator) * key.XorMultiplier;
										for (int k = 0; k < xor_size; k++)
											data[k] ^= key.Key[table_offset + k];

										if (header.CompressionMethod == 8)
										{
											try
											{
												uncompressed_data = DeflateStream.UncompressBuffer (data);
											}
											catch
											{
											}
										}
										else
											uncompressed_data = data;

										if (uncompressed_data != null && uncompressed_data.Length == header.UncompressedSize && uncompressed_data.CalculateCrc32 () == header.Crc)
										{
											current_key = key;
											break;
										}
										else
										{
											uncompressed_data = null;

											for (int k = 0; k < xor_size; k++)
												data[k] ^= key.Key[table_offset + k];
										}
									}

									if (current_key == null)
										throw new InvalidOperationException ("Unknown Aion version");
								}
								else if (current_key != null && header.CompressionMethod == 8)
								{
									int table_offset = (data.Length & current_key.XorModificator) * current_key.XorMultiplier;
									int xor_size = Math.Min (data.Length, 32);
									for (int k = 0; k < xor_size; k++)
										data[k] ^= current_key.Key[table_offset + k];

									try
									{
										uncompressed_data = DeflateStream.UncompressBuffer (data);
									}
									catch
									{
									}
								}
								else
									uncompressed_data = data;

								return uncompressed_data;
							}

						// central directory
						case 0xFDFEB4AF:
							{
								if (f.BaseStream.Position + PakFileCentralDirectory.Size > f.BaseStream.Length)
									break;

								PakFileCentralDirectory header = new PakFileCentralDirectory (f);
								string file_name = Encoding.Default.GetString (f.ReadBytes (header.FilenameLength));

								f.BaseStream.Seek (header.ExtraFieldLength + header.CommentLength, SeekOrigin.Current);

								break;
							}

						// end of archive
						case 0xF9FAB4AF:
							{
								f.BaseStream.Seek (22 - 4, SeekOrigin.Current);
								break;
							}

						default:
							throw new InvalidOperationException (string.Format (CultureInfo.InvariantCulture, "Unknown block id 0x{0:X}", block_id));
					}
				}
			}

			throw new InvalidOperationException ("File '" + fileName + "' not found inside '" + pakFileName + "'.");
		}

		public XDocument ExtractXml (string pakFileName, string fileName)
		{
			byte[] f = this.ExtractFile (pakFileName, fileName);

			try
			{
				using (MemoryStream stream = new MemoryStream (f))
				using (BinaryReader stream2 = new BinaryReader (stream))
				{
					BinaryXml xml = new BinaryXml ();
					return xml.ReadXml (stream2);
				}
			}
			catch
			{
				return XDocument.Parse (Encoding.UTF8.GetString (f));
			}
		}

		#endregion

		#region Dictionaries

		private readonly Dictionary<string, byte> qualityTable = new Dictionary<string, byte> ();
		private readonly Dictionary<string, byte> craftskillTable = new Dictionary<string, byte> ();
		private readonly Dictionary<string, byte> raceTable = new Dictionary<string, byte> ();
		private readonly Dictionary<string, string> strings = new Dictionary<string, string> ();

		private readonly Dictionary<string, byte> weaponTypeTable = new Dictionary<string, byte> ();
		private readonly Dictionary<string, byte> armorTypeTable = new Dictionary<string, byte> ();
		private readonly Dictionary<string, byte> jewelryTypeTable = new Dictionary<string, byte> ();

		private readonly byte shieldType = 100;
		private readonly byte hatType = 110;

		#endregion

		#region Run

		private class LangPakInfo
		{
			public string Pak { get; set; }
			public string Lang { get; set; }
		}

		public void Run (string[] args)
		{
			this.qualityTable.Add ("junk", 0);
			this.qualityTable.Add ("common", 10);
			this.qualityTable.Add ("rare", 20);
			this.qualityTable.Add ("legend", 30);
			this.qualityTable.Add ("unique", 40);
			this.qualityTable.Add ("epic", 50);
			this.qualityTable.Add ("mythic", 60);

			this.craftskillTable.Add ("alchemy", 0);
			this.craftskillTable.Add ("armorsmith", 1);
			this.craftskillTable.Add ("convert", 2);
			this.craftskillTable.Add ("cooking", 3);
			this.craftskillTable.Add ("handiwork", 4);
			this.craftskillTable.Add ("tailoring", 5);
			this.craftskillTable.Add ("weaponsmith", 6);
            this.craftskillTable.Add ("menuisier", 7);

            this.raceTable.Add("pc_light", 0);
            this.raceTable.Add("pc_dark", 1);
            this.raceTable.Add("all", 2);

			this.weaponTypeTable.Add ("1h_dagger", 1);
			this.weaponTypeTable.Add ("1h_mace", 2);
			this.weaponTypeTable.Add ("1h_sword", 3);
			this.weaponTypeTable.Add ("2h_book", 4);
			this.weaponTypeTable.Add ("2h_orb", 5);
			this.weaponTypeTable.Add ("2h_polearm", 6);
			this.weaponTypeTable.Add ("2h_staff", 7);
			this.weaponTypeTable.Add ("2h_sword", 8);
			this.weaponTypeTable.Add ("bow", 9);

			this.armorTypeTable.Add ("robe", 20);
			this.armorTypeTable.Add ("leather", 30);
			this.armorTypeTable.Add ("chain", 40);
			this.armorTypeTable.Add ("plate", 50);

			this.jewelryTypeTable.Add ("waist", 60);
			this.jewelryTypeTable.Add ("neck", 70);
			this.jewelryTypeTable.Add ("right_or_left_ear", 80);
			this.jewelryTypeTable.Add ("right_or_left_finger", 90);

			Program.Log ("Started");

			List<LangPakInfo> lang_paks = new List<LangPakInfo> ();
			foreach (string p in Settings.Default.Paks.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var pp = p.Split (new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				lang_paks.Add (new LangPakInfo ()
				{
					Pak = pp[0],
					Lang = pp[1]
				});
			}


			if (Settings.Default.ImportItems || Settings.Default.ImportRecipes)
			{
				foreach (LangPakInfo p in lang_paks)
				{
					Program.Log ("Perform import operation for language '{0}'", p.Lang);

                    Program.Log("Loading strings");
                    XDocument xml_strings = this.ExtractXml(Path.Combine(Settings.Default.AionDirectory, p.Pak), @"strings\client_strings_item.xml");

                    this.strings.Clear();
                    foreach (XElement string_node in xml_strings.Root.Elements("string"))
                    {
                        string name = string_node.GetElementValue("name");
                        string value = string_node.GetElementValue("body");

                        this.strings.Add(name.ToUpper(), value);
                    }

                    xml_strings = this.ExtractXml(Path.Combine(Settings.Default.AionDirectory, p.Pak), @"strings\client_strings_item2.xml");

                    foreach (XElement string_node in xml_strings.Root.Elements("string"))
                    {
                        string name = string_node.GetElementValue("name");
                        string value = string_node.GetElementValue("body");

                        this.strings.Add(name.ToUpper(), value);
                    }

                    xml_strings = this.ExtractXml(Path.Combine(Settings.Default.AionDirectory, p.Pak), @"strings\client_strings_quest.xml");

                    foreach (XElement string_node in xml_strings.Root.Elements("string"))
                    {
                        string name = string_node.GetElementValue("name");
                        string value = string_node.GetElementValue("body");

                        this.strings.Add(name.ToUpper(), value);
                    }

                    xml_strings = this.ExtractXml(Path.Combine(Settings.Default.AionDirectory, p.Pak), @"strings\client_strings_ui.xml");

                    foreach (XElement string_node in xml_strings.Root.Elements("string"))
                    {
                        string name = string_node.GetElementValue("name");
                        string value = string_node.GetElementValue("body");

                        this.strings.Add(name.ToUpper(), value);
                    }

					this.ImportToJs (p.Lang);
				}
			}

			if (Settings.Default.PrepareZips)
			{
				foreach (LangPakInfo p in lang_paks)
				{
					Program.Log ("Prepare zips for language '{0}'", p.Lang);
					this.PrepareZips (p.Lang);
				}
			}

			Program.Log ("All done");
		}

		#endregion

		#region Import to js

		#region Helper classes

		private class ItemData
		{
			public int Id { get; set; }
			public int? IdProc { get; set; }
			public string Description { get; set; }
			public string Icon { get; set; }
            public string Imageid { get; set; }
			public byte Quality { get; set; }
			public short Level { get; set; }
			public byte Type { get; set; }
			public bool IsQuest { get; set; }
			public int Price { get; set; }

			public string[] GetJsFields ()
			{
				List<string> fields = new List<string> ();
				fields.Add ("id:" + this.Id);
				fields.Add ("d:\"" + (this.Description ?? string.Empty).Replace ("\"", "\\\"") + "\"");
                fields.Add ("i:\"" + this.Icon + "\"");
                fields.Add ("im:\"" + this.Imageid + "\"");
				fields.Add ("q:" + this.Quality);
				fields.Add ("l:" + this.Level);
				fields.Add ("t:" + this.Type);
				fields.Add ("p:" + this.Price);

				if (this.IdProc.HasValue)
					fields.Add ("idp:" + this.IdProc.Value);

				return fields.ToArray ();
			}
		}

		private enum RecipeItemType : byte
		{
			Product = 0,
			Combo = 1,
			Component = 2
		}

		private class RecipeItemData
		{
			public RecipeItemType Type { get; set; }
			public ItemData Item { get; set; }
			public short Quantity { get; set; }

			public string[] GetJsFields ()
			{
				return
					new string[]
					{
						"rt:" + (byte) this.Type,
						"qn:" + this.Quantity
					}
					.Union (this.Item.GetJsFields ())
					.ToArray ();
			}

			public string[] GetJsFields (RecipeData recipe)
			{
				RecipeItemData c = (from x in recipe.Items
									where
										(x.Type == RecipeItemType.Product || x.Type == RecipeItemType.Combo)
										&&
										x.Item == this.Item
									select x).SingleOrDefault ();

				if (c == null)
					throw new InvalidOperationException ("Invalid component recipe specified.");

				return
					new string[]
					{
						"rt:" + (byte) c.Type,
						"qn:" + c.Quantity
					}
					.Union (this.Item.GetJsFields ())
					.ToArray ();
			}
		}

		private class RecipeData
		{
            public int Id { get; set; }
            public byte Race { get; set; }
			public byte Skill { get; set; }
			public short SkillLevel { get; set; }
			public List<RecipeItemData> Items { get; set; }

			public string[] GetJsFields ()
			{
				return new string[]
				{
					"rid:" + this.Id,
					"r:" + this.Race,
					"s:" + this.Skill,
					"sl:" + this.SkillLevel
				};
			}
		}

		#endregion

		private List<ItemData> items = new List<ItemData> ();
		private Dictionary<int, ItemData> itemsById = new Dictionary<int, ItemData> ();
		private Dictionary<string, ItemData> itemsByName = new Dictionary<string, ItemData> ();
		private List<RecipeData> recipes = new List<RecipeData> ();

		private void ImportToJs (string lang)
		{
			#region Load items

			Program.Log ("Loading items");

			this.items.Clear ();
			this.itemsById.Clear ();
			this.itemsByName.Clear ();

            Dictionary<string, string> filelist = new Dictionary<string, string>();
            for (int i = 0; i <= 9; i++)
            {
                string[] files = Directory.GetFiles(String.Format("icons/{0}", i));
                foreach (var file in files)
                {
                    string imageid = File.ReadAllText(file);
                    string id = file.Replace(String.Format("icons/{0}\\", i), "");
                    filelist.Add(id, imageid);
                }
            }

			XDocument xml_items = this.ExtractXml (Path.Combine (Settings.Default.AionDirectory, Settings.Default.ItemsPakPath), "client_items.xml");

			foreach (XElement item_node in xml_items.Root.Elements ("client_item"))
			{
                if (item_node.GetElementValue("desc") != null)
                {
                    ItemData item = new ItemData();

                    item.Id = item_node.GetElementValue<int>("id");
                    string Imageid = (filelist.ContainsKey(item.Id.ToString()) ? filelist[item.Id.ToString()] : "");
                    if (string.IsNullOrEmpty(Imageid))
                        Program.Log("WARNING: Item icon not found {0}", item.Id);

                    item.Description = strings.TryGetValue(item_node.GetElementValue("desc").ToUpper() ?? string.Empty, null);
                    item.Icon = item_node.GetElementValue("icon_name");
                    item.Imageid = Imageid;
                    item.Quality = this.qualityTable[item_node.GetElementValue("quality").ToLowerInvariant()];
                    item.Level = item_node.GetElementValue<short>("level");
                    item.IsQuest = (item_node.Element("quest") != null);
                    item.Price = item_node.GetElementValue<int>("price");

                    if (item.Description == null)
                    {
                        Program.Log("WARNING: Missing description for {0}", item.Id);
                    }

                    // determine item type
                    {
                        byte type = 0;

                        string wt = item_node.GetElementValue("weapon_type");
                        if (string.IsNullOrEmpty(wt) || !this.weaponTypeTable.TryGetValue(wt, out type))
                        {
                            string at = item_node.GetElementValue("armor_type");
                            if (string.IsNullOrEmpty(at) || !this.armorTypeTable.TryGetValue(at, out type))
                            {
                                string es = item_node.GetElementValue("equipment_slots");
                                if (string.IsNullOrEmpty(es) || !this.jewelryTypeTable.TryGetValue(es, out type))
                                {
                                    if (at == "no_armor")
                                    {
                                        if (es == "sub")
                                            type = this.shieldType;
                                        else if (es == "head")
                                            type = this.hatType;
                                    }
                                }
                            }
                        }

                        item.Type = type;
                    }

                    this.items.Add(item);
                    this.itemsById.Add(item.Id, item);
                    this.itemsByName.Add(item_node.GetElementValue("name").ToLowerInvariant(), item);
                }
			}

			#endregion

			#region Load recipes

			Program.Log ("Loading recipes");

			this.recipes.Clear ();

			XDocument xml_recipes = this.ExtractXml (Path.Combine (Settings.Default.AionDirectory, Settings.Default.ItemsPakPath), "client_combine_recipe.xml");

            foreach (XElement recipe_node in xml_recipes.Root.Elements("client_combine_recipe"))
            {
                RecipeData recipe = new RecipeData();

                recipe.Id = recipe_node.GetElementValue<int>("id");
                recipe.Skill = this.craftskillTable[recipe_node.GetElementValue("combineskill")];
                recipe.Race = this.raceTable[recipe_node.GetElementValue("qualification_race")];
                recipe.SkillLevel = recipe_node.GetElementValue<short>("required_skillpoint");
                recipe.Items = new List<RecipeItemData>();

                ItemData item = this.itemsByName[recipe_node.GetElementValue("product").ToLowerInvariant()];
                if (item.IsQuest)
                    continue;

                recipe.Items.Add(new RecipeItemData
                {
                    Type = RecipeItemType.Product,
                    Item = item,
                    Quantity = recipe_node.GetElementValue<byte>("product_quantity")
                });

                // combo
                for (int k = 1; ; k++)
                {
                    string combo = recipe_node.GetElementValue("combo" + k + "_product");
                    if (string.IsNullOrEmpty(combo))
                        break;

                    ItemData combo_item = this.itemsByName[combo.ToLowerInvariant()];
                    recipe.Items.Add(new RecipeItemData
                    {
                        Type = RecipeItemType.Combo,
                        Item = combo_item,
                        Quantity = recipe_node.GetElementValue<byte>("product_quantity")
                    });

                    recipe.Items[0].Item.IdProc = combo_item.Id;
                    combo_item.IdProc = recipe.Items[0].Item.Id;
                }

                // components
                int max_components = recipe_node.GetElementValue<int>("component_quantity");
                for (int k = 1; k <= max_components; k++)
                {
                    recipe.Items.Add(new RecipeItemData
                    {
                        Type = RecipeItemType.Component,
                        Item = this.itemsByName[recipe_node.GetElementValue("component" + k).ToLowerInvariant()],
                        Quantity = recipe_node.GetElementValue<short>("compo" + k + "_quantity")
                    });
                }

                this.recipes.Add(recipe);
            }

			#endregion

			Program.Log ("Loaded {0} items and {1} recipes", this.items.Count, this.recipes.Count);

			#region Export items

			var js_directory = Path.Combine (Settings.Default.JsDirectory, lang);
			if (!Directory.Exists (js_directory))
				Directory.CreateDirectory (js_directory);

			if (Settings.Default.ImportItems)
			{
				Program.Log ("Export craftable items");

				byte? last_race = null;
				byte? last_skill = null;
				List<string> data = new List<string> ();

				// for each recipe
				foreach (RecipeData recipe in from x in this.recipes
											  orderby x.Race, x.Skill, x.SkillLevel
											  select x)
				{
					if (!last_race.HasValue || last_race != recipe.Race || !last_skill.HasValue || last_skill != recipe.Skill)
					{
						if (data.Count > 0)
						{
							File.WriteAllText
							(
								Path.Combine (js_directory, last_race + "_" + last_skill + ".js"),
								"[" + string.Join (",", data.ToArray ()) + "]"
							);
						}

						last_race = recipe.Race;
						last_skill = recipe.Skill;
						data.Clear ();
					}

					// for each product or combo item result in recipe
					foreach (RecipeItemData recipe_item in from x in recipe.Items
														   where x.Type == RecipeItemType.Product || x.Type == RecipeItemType.Combo
														   select x)
					{
						data.Add ("{" + string.Join (",", recipe.GetJsFields ()) + "," + string.Join (",", recipe_item.GetJsFields ()) + "}");
					}
				}

				if (data.Count > 0)
				{
					File.WriteAllText
					(
						Path.Combine (js_directory, last_race + "_" + last_skill + ".js"),
						"[" + string.Join (",", data.ToArray ()) + "]"
					);
				}
			}

			#endregion

			#region Export recipes

			if (Settings.Default.ImportRecipes)
			{
				Program.Log ("Export recipes");

				if (!Directory.Exists (Settings.Default.JsDirectory))
					Directory.CreateDirectory (Settings.Default.JsDirectory);

                int recipeCount = 1;

				// for each recipe
				foreach (RecipeData recipe in this.recipes)
				{
					// for each product or combo item result in recipe
					foreach (RecipeItemData recipe_item in from x in recipe.Items
														   where x.Type == RecipeItemType.Product || x.Type == RecipeItemType.Combo
														   select x)
					{
						ItemData item = recipe_item.Item;

                        Program.Log(String.Format("{0}_{1}.js - {2} of {3}", recipe.Id, item.Id, recipeCount, this.recipes.Count));

						File.WriteAllText
						(
							Path.Combine (js_directory, recipe.Id + "_" + item.Id + ".js"),
							"{" + this.GetRecipeItemJs (recipe, recipe_item) + "}"
						);
					}

                    recipeCount++;
				}
			}

			#endregion

			Program.Log ("Finished");
		}

		private string GetRecipeItemJs (RecipeData recipe, RecipeItemData recipeItem)
		{
			string js = string.Join (",", recipeItem.GetJsFields (recipe));
			js += "," + string.Join (",", recipe.GetJsFields ());

			List<string> component_js = new List<string> ();

			// recipe components
			foreach (RecipeItemData component in from x in recipe.Items
												 where x.Type == RecipeItemType.Component
												 select x)
			{
				string cjs = "{nq:" + component.Quantity + ",";

				// find recipe for component
				RecipeData[] rr = (from x in this.recipes
								   where x.Items.Any
									   (y =>
										   (y.Type == RecipeItemType.Product || y.Type == RecipeItemType.Combo)
										   &&
										   y.Item == component.Item
									   )
								   select x).ToArray ();

				if (rr.Length > 0)
				{
					RecipeData component_recipe = rr[0];
					foreach (RecipeData r in rr)
					{
						if (r.Skill == recipe.Skill)
						{
							component_recipe = r;
							break;
						}
					}

					cjs += this.GetRecipeItemJs (component_recipe, component);
				}
				else
					cjs += string.Join (",", component.GetJsFields ());

				cjs += "}";
				component_js.Add (cjs);
			}

			if (component_js.Count > 0)
				js += ",c:[" + string.Join (",", component_js.ToArray ()) + "]";

			return js;
		}

		#endregion

		#region Prepare zips

		private void PrepareZips (string lang)
		{
			Directory.SetCurrentDirectory (Settings.Default.JsDirectory);

			string[] files = Directory.GetFiles (lang);
			int max_zips = files.Length / Settings.Default.ZipMaxFiles + 1;

			for (int k = 0; k < max_zips; k++)
			{
				using (ZipFile zip = new ZipFile ())
				{
					zip.AddFiles (files.Skip (k * Settings.Default.ZipMaxFiles).Take (Settings.Default.ZipMaxFiles));
					zip.Save (Path.Combine (Settings.Default.ZipDirectory, lang + k + ".zip"));
				}
			}

			Program.Log ("Finished");
		}

		#endregion
	}
}
