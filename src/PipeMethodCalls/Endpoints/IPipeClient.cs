using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// A named pipe client.
	/// </summary>
	/// <typeparam name="TRequesting">The interface that the client will be invoking on the server.</typeparam>
	public interface IPipeClient<TRequesting> : IPipeInvokerHost<TRequesting>
		where TRequesting : class
	{
		/// <summary>
		/// Connects to the server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		Task ConnectAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Sets up the given action as a logger for the module.
		/// </summary>
		/// <param name="logger">The logger action.</param>
		void SetLogger(Action<string> logger);

		/// <summary>
		/// Waits for the server to close the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default);
	}
}