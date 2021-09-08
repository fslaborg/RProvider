using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// A method response to be sent over the pipe. Contains a typed response value.
	/// </summary>
	internal class TypedPipeResponse
	{
		/// <summary>
		/// The call ID.
		/// </summary>
		public long CallId { get; private set; }

		/// <summary>
		/// True if the call succeeded.
		/// </summary>
		public bool Succeeded { get; private set; }

		/// <summary>
		/// The response data. Valid if Succeeded is true.
		/// </summary>
		public object Data { get; private set; }

		/// <summary>
		/// The error details. Valid if Succeeded is false.
		/// </summary>
		public string Error { get; private set; }

		/// <summary>
		/// Creates a new success pipe response.
		/// </summary>
		/// <param name="callId">The ID of the call.</param>
		/// <param name="data">The returned data.</param>
		/// <returns>The success pipe response.</returns>
		public static TypedPipeResponse Success(long callId, object data)
		{
			return new TypedPipeResponse { Succeeded = true, CallId = callId, Data = data };
		}

		/// <summary>
		/// Creates a new failure pipe response.
		/// </summary>
		/// <param name="callId">The ID of the call.</param>
		/// <param name="message">The failure message.</param>
		/// <returns>The failure pipe response.</returns>
		public static TypedPipeResponse Failure(long callId, string message)
		{
			return new TypedPipeResponse { Succeeded = false, CallId = callId, Error = message };
		}

		/// <summary>
		/// Gets a string representation of this typed response.
		/// </summary>
		/// <returns>A string representation of this typed response.</returns>
		public override string ToString()
		{
			var builder = new StringBuilder("Response");
			builder.AppendLine();
			builder.Append("  CallId: ");
			builder.Append(this.CallId);
			builder.AppendLine();
			builder.Append("  Succeeded: ");
			builder.Append(this.Succeeded);
			builder.AppendLine();
			if (this.Succeeded)
			{
				builder.Append("  Response: ");
				builder.Append(this.Data?.ToString() ?? "<null>");
			}
			else
			{
				builder.Append("  Error: ");
				builder.Append(this.Error);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Serializes the response.
		/// </summary>
		/// <param name="serializer">The serializer to use.</param>
		/// <returns>The serialized response.</returns>
		public SerializedPipeResponse Serialize(IPipeSerializer serializer)
		{
			if (this.Succeeded)
			{
				return SerializedPipeResponse.Success(this.CallId, serializer.Serialize(this.Data));
			}
			else
			{
				return SerializedPipeResponse.Failure(this.CallId, this.Error);
			}
		}
	}
}
