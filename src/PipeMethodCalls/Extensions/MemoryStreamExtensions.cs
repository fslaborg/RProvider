using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace PipeMethodCalls
{
	internal static class MemoryStreamExtensions
	{
		public static void WriteInt(this MemoryStream memoryStream, int val)
		{
			memoryStream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(val)), 0, 4);
		}

		public static int ReadInt(this MemoryStream memoryStream)
		{
			byte[] intBytes = new byte[4];
			memoryStream.Read(intBytes, 0, 4);
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(intBytes, 0));
		}

		public static void WriteLong(this MemoryStream memoryStream, long val)
		{
			memoryStream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(val)), 0, 8);
		}

		public static long ReadLong(this MemoryStream memoryStream)
		{
			byte[] callIdBytes = new byte[8];
			memoryStream.Read(callIdBytes, 0, 8);

			return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(callIdBytes, 0));
		}

		public static void WriteArray(this MemoryStream memoryStream, byte[][] arr)
		{
			memoryStream.WriteInt(arr.Length);
			foreach (byte[] itemBytes in arr)
			{
				memoryStream.WriteInt(itemBytes.Length);
				memoryStream.Write(itemBytes, 0, itemBytes.Length);
			}
		}

		public static byte[][] ReadArray(this MemoryStream memoryStream)
		{
			int arrayLength = memoryStream.ReadInt();
			byte[][] result = new byte[arrayLength][];
			for (int i = 0; i < arrayLength; i++)
			{
				int payloadLength = memoryStream.ReadInt();
				byte[] payloadBytes = new byte[payloadLength];
				memoryStream.Read(payloadBytes, 0, payloadLength);
				result[i] = payloadBytes;
			}

			return result;
		}

		public static void WriteUtf8String(this MemoryStream memoryStream, string str)
		{
			byte[] strBytes = Encoding.UTF8.GetBytes(str);
			memoryStream.Write(strBytes, 0, strBytes.Length);

			// Null-terminate
			memoryStream.WriteByte(0);
		}

		public static string ReadUtf8String(this MemoryStream memoryStream)
		{
			long originalPosition = memoryStream.Position;
			while (memoryStream.ReadByte() != 0)
			{
			}

			long positionAfterReadingZero = memoryStream.Position;

			// Go back to where we started and read as a string
			memoryStream.Seek(originalPosition, SeekOrigin.Begin);

			byte[] utf8Bytes = new byte[positionAfterReadingZero - originalPosition - 1];

			memoryStream.Read(utf8Bytes, 0, utf8Bytes.Length);

			// Read the null to get us past it
			memoryStream.ReadByte();

			return Encoding.UTF8.GetString(utf8Bytes);
		}
	}
}
