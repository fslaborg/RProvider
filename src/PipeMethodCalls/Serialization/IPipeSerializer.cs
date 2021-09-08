using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Defines how to serialize and deserialize objects to pass through the pipe.
	/// </summary>
	public interface IPipeSerializer
	{
		/// <summary>
		/// Serializes the given object into bytes.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <returns>The bytes to represent the serialized object.</returns>
		byte[] Serialize(object o);

		/// <summary>
		/// Deserializes the given bytes into an object.
		/// </summary>
		/// <param name="data">The data to deserialize.</param>
		/// <param name="type">The type to deserialize to.</param>
		/// <returns>The deserialized object.</returns>
		object Deserialize(byte[] data, Type type);
	}
}
