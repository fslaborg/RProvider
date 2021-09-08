using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Represents when an invoke was successfully executed on the remote endpoint, but the method threw an exception.
	/// </summary>
	public class PipeInvokeFailedException : IOException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PipeInvokeFailedException"/> class.
		/// </summary>
		/// <param name="message">The exception message.</param>
		public PipeInvokeFailedException(string message) 
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeInvokeFailedException"/> class.
		/// </summary>
		/// <param name="message">The exception message.</param>
		/// <param name="innerException">The inner exception.</param>
		public PipeInvokeFailedException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
