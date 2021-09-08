using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// Invokes methods on a named pipe.
	/// </summary>
	/// <typeparam name="TRequesting">The interface that we will be invoking.</typeparam>
	public interface IPipeInvoker<TRequesting>
		where TRequesting : class
	{
		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default);

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		Task InvokeAsync(Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default);

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <typeparam name="TResult">The type of result from the method.</typeparam>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The method result.</returns>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, Task<TResult>>> expression, CancellationToken cancellationToken = default);

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <typeparam name="TResult">The type of result from the method.</typeparam>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The method result.</returns>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default);
	}
}
