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
	/// Extension methods for IPipeInvokerHost
	/// </summary>
	public static class PipeInvokerHostExtensions
	{
		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <typeparam name="TRequesting">The interface to invoke.</typeparam>
		/// <param name="invokerHost">The invoker host to run the command on.</param>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		public static Task InvokeAsync<TRequesting>(this IPipeInvokerHost<TRequesting> invokerHost, Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default)
			where TRequesting : class
		{
			EnsureInvokerNonNull(invokerHost.Invoker);
			return invokerHost.Invoker.InvokeAsync(expression, cancellationToken);
		}

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <typeparam name="TRequesting">The interface to invoke.</typeparam>
		/// <param name="invokerHost">The invoker host to run the command on.</param>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		public static Task InvokeAsync<TRequesting>(this IPipeInvokerHost<TRequesting> invokerHost, Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default)
			where TRequesting : class
		{
			EnsureInvokerNonNull(invokerHost.Invoker);
			return invokerHost.Invoker.InvokeAsync(expression, cancellationToken);
		}

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <typeparam name="TRequesting">The interface to invoke.</typeparam>
		/// <typeparam name="TResult">The type of result from the method.</typeparam>
		/// <param name="invokerHost">The invoker host to run the command on.</param>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The method result.</returns>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		public static Task<TResult> InvokeAsync<TRequesting, TResult>(this IPipeInvokerHost<TRequesting> invokerHost, Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default)
			where TRequesting : class
		{
			EnsureInvokerNonNull(invokerHost.Invoker);
			return invokerHost.Invoker.InvokeAsync(expression, cancellationToken);
		}

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <typeparam name="TRequesting">The interface to invoke.</typeparam>
		/// <typeparam name="TResult">The type of result from the method.</typeparam>
		/// <param name="invokerHost">The invoker host to run the command on.</param>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The method result.</returns>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		public static Task<TResult> InvokeAsync<TRequesting, TResult>(this IPipeInvokerHost<TRequesting> invokerHost, Expression<Func<TRequesting, Task<TResult>>> expression, CancellationToken cancellationToken = default)
			where TRequesting : class
		{
			EnsureInvokerNonNull(invokerHost.Invoker);
			return invokerHost.Invoker.InvokeAsync(expression, cancellationToken);
		}

		/// <summary>
		/// Ensures the given invoker is not null.
		/// </summary>
		/// <typeparam name="TRequesting">The interface to invoke.</typeparam>
		/// <param name="invoker">The invoker to check.</param>
		private static void EnsureInvokerNonNull<TRequesting>(IPipeInvoker<TRequesting> invoker)
			where TRequesting : class
		{
			if (invoker == null)
			{
				throw new PipeInvokeFailedException("Can only invoke methods after connecting the pipe.");
			}
		}
	}
}
