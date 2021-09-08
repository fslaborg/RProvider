using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Represents the state of the pipe connection.
	/// </summary>
	public enum PipeState
	{
		/// <summary>
		/// The pipe has not yet connected.
		/// </summary>
		NotOpened,

		/// <summary>
		/// The pipe is connected, methods can be invoked and we are listening for message from the other side of the pipe.
		/// </summary>
		Connected,

		/// <summary>
		/// The pipe has closed gracefully.
		/// </summary>
		Closed,

		/// <summary>
		/// An unexpected error caused the pipe to close.
		/// </summary>
		Faulted
	}
}
