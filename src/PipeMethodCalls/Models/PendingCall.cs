using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// Represents a pending call executing on the remote endpoint.
	/// </summary>
	internal class PendingCall
	{
		/// <summary>
		/// The task completion source for the call.
		/// </summary>
		public TaskCompletionSource<SerializedPipeResponse> TaskCompletionSource { get; } = new TaskCompletionSource<SerializedPipeResponse>();
	}
}
