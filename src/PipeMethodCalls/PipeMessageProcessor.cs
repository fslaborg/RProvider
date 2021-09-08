using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// Handles the pipe connection state and work processing.
	/// </summary>
	internal sealed class PipeMessageProcessor : IDisposable
	{
		private TaskCompletionSource<object> pipeCloseCompletionSource;
		private CancellationTokenSource workLoopCancellationTokenSource;

		/// <summary>
		/// Gets the state of the pipe.
		/// </summary>
		public PipeState State { get; private set; } = PipeState.NotOpened;

		/// <summary>
		/// Gets the pipe fault exception.
		/// </summary>
		public Exception PipeFault { get; private set; }

		/// <summary>
		/// Starts the processing loop on the pipe.
		/// </summary>
		public async void StartProcessing(PipeStreamWrapper pipeStreamWrapper)
		{
			if (this.State != PipeState.NotOpened)
			{
				throw new InvalidOperationException("Can only call connect once");
			}

			this.State = PipeState.Connected;

			try
			{
				this.workLoopCancellationTokenSource = new CancellationTokenSource();

				// Process messages until canceled.
				while (true)
				{
					this.workLoopCancellationTokenSource.Token.ThrowIfCancellationRequested();
					await pipeStreamWrapper.ProcessMessageAsync(this.workLoopCancellationTokenSource.Token).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				// This is a normal dispose.
				this.State = PipeState.Closed;
				if (this.pipeCloseCompletionSource != null)
				{
					this.pipeCloseCompletionSource.TrySetResult(null);
				}
			}
			catch (Exception exception)
			{
				this.State = PipeState.Faulted;
				this.PipeFault = exception;
				if (this.pipeCloseCompletionSource != null)
				{
					if (!(exception is IOException))
					{
						exception = new IOException("Pipe closed with error", exception);
					}

					this.pipeCloseCompletionSource.TrySetException(exception);
				}
			}
		}

		/// <summary>
		/// Wait for the other end to close the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <exception cref="IOException">Thrown when the pipe has closed due to an unknown error.</exception>
		/// <remarks>This does not throw when the other end closes the pipe.</remarks>
		public Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default)
		{
			if (this.State == PipeState.Closed)
			{
				return Task.CompletedTask;
			}

			if (this.State == PipeState.Faulted)
			{
				return Task.FromException(this.PipeFault);
			}

			if (this.pipeCloseCompletionSource == null)
			{
				this.pipeCloseCompletionSource = new TaskCompletionSource<object>();
			}

			cancellationToken.Register(() =>
			{
				this.pipeCloseCompletionSource.TrySetCanceled();
			});

			return this.pipeCloseCompletionSource.Task;
		}

		#region IDisposable Support
		private bool disposed = false;

		/// <summary>
		/// Disposes resources associated with the instance.
		/// </summary>
		public void Dispose()
		{
			if (!disposed)
			{
				if (this.workLoopCancellationTokenSource != null)
				{
					this.workLoopCancellationTokenSource.Cancel();
				}

				disposed = true;
			}
		}
		#endregion
	}
}
