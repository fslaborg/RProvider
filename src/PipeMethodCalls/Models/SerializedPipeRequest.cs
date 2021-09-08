using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// A request sent over the pipe that has been user serialized by an <see cref="IPipeSerializer"/>.
	/// </summary>
	/// <remarks>
	/// There are two stages to serialization:
	/// 1: User serialization, where typed values are serialized to a byte array.
	/// 2: Message serialization, where the message is sent with a custom binary serialization over the pipe.
	/// </remarks>
	internal class SerializedPipeRequest : IMessage
	{
		/// <summary>
		/// The call ID.
		/// </summary>
		public long CallId { get; set; }

		/// <summary>
		/// The name of the method to invoke.
		/// </summary>
		public string MethodName { get; set; }

		/// <summary>
		/// The list of parameters to pass to the method.
		/// </summary>
		/// <remarks>
		/// The method invoker will know what type each parameter needs to be converted to, and with that type the serializer will be able to convert it to and from a byte array.
		/// </remarks>
		public byte[][] Parameters { get; set; }

		/// <summary>
		/// The types for the generic arguments.
		/// </summary>
		public Type[] GenericArguments { get; set; }
	}
}
