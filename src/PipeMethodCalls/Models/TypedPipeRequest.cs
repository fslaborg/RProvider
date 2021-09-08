using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// A method request to be sent over the pipe. Contains typed parameters.
	/// </summary>
	internal class TypedPipeRequest
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
		public object[] Parameters { get; set; }

		/// <summary>
		/// The types for the generic arguments.
		/// </summary>
		public Type[] GenericArguments { get; set; }

		/// <summary>
		/// Gets a string representation of this typed request.
		/// </summary>
		/// <returns>The string representation of this typed request.</returns>
		public override string ToString()
		{
			var builder = new StringBuilder("Request");
			builder.AppendLine();
			builder.Append("  CallId: ");
			builder.Append(this.CallId);
			builder.AppendLine();
			builder.Append("  MethodName: ");
			builder.Append(this.MethodName);
			if (this.Parameters.Length > 0)
			{
				builder.AppendLine();
				builder.Append("  Parameters:");
				foreach (object parameter in this.Parameters)
				{
					builder.AppendLine();
					builder.Append("    ");
					builder.Append(parameter?.ToString() ?? "<null>");
				}
			}

			if (this.GenericArguments.Length > 0)
			{
				builder.AppendLine();
				builder.Append("  Generic arguments:");
				foreach (Type genericArgument in this.GenericArguments)
				{
					builder.AppendLine();
					builder.Append("    ");
					builder.Append(genericArgument.ToString());
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Serializes the request.
		/// </summary>
		/// <param name="serializer">The serializer to use.</param>
		/// <returns>The serialized request.</returns>
		public SerializedPipeRequest Serialize(IPipeSerializer serializer)
		{
			return new SerializedPipeRequest
			{
				CallId = this.CallId,
				MethodName = this.MethodName,
				Parameters = this.Parameters.Select(r => serializer.Serialize(r)).ToArray(),
				GenericArguments = this.GenericArguments
			};
		}
	}
}
