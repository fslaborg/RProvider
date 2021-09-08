using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// A named pipe server.
	/// </summary>
	public interface IPipeServer : IDisposable
	{
		/// <summary>
		/// Sets up the given action as a logger for the module.
		/// </summary>
		/// <param name="logger">The logger action.</param>
		void SetLogger(Action<string> logger);

		/// <summary>
		/// Waits for a client to connect to the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		Task WaitForConnectionAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Waits for the client to close the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default);
	}
}