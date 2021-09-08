using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// A named pipe server.
	/// </summary>
	/// <typeparam name="THandling">The interface for requests that this server will be handling.</typeparam>
	public class PipeServer<THandling> : IPipeServer, IDisposable
		where THandling : class
	{
		private readonly IPipeSerializer serializer;
		private readonly string pipeName;
		private readonly Func<THandling> handlerFactoryFunc;
		private readonly PipeOptions? options;
		private NamedPipeServerStream rawPipeStream;
		private Action<string> logger;
		private PipeMessageProcessor messageProcessor = new PipeMessageProcessor();

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeServer{THandling}"/> class.
		/// </summary>
		/// <param name="serializer">
		/// The serializer to use for the pipe. You can include a library like PipeMethodCalls.NetJson and pass in <c>new NetJsonPipeSerializer()</c>.
		/// This will serialize and deserialize method parameters and return values so they can be passed over the pipe.
		/// </param>
		/// <param name="pipeName">The pipe name.</param>
		/// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
		/// <param name="options">Extra options for the pipe.</param>
		public PipeServer(IPipeSerializer serializer, string pipeName, Func<THandling> handlerFactoryFunc, PipeOptions? options = null)
		{
			this.serializer = serializer;
			this.pipeName = pipeName;
			this.handlerFactoryFunc = handlerFactoryFunc;
			this.options = options;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeServer{THandling}"/> class.
		/// </summary>
		/// <param name="serializer">
		/// The serializer to use for the pipe. You can include a library like PipeMethodCalls.NetJson and pass in <c>new NetJsonPipeSerializer()</c>.
		/// This will serialize and deserialize method parameters and return values so they can be passed over the pipe.
		/// </param>
		/// <param name="rawPipe">Raw pipe stream to wrap with method call capability. Must be set up with PipeDirection - <see cref="PipeDirection.InOut"/>, PipeOptions - <see cref="PipeOptions.Asynchronous"/>, and PipeTransmissionMode - <see cref="PipeTransmissionMode.Byte"/></param>
		/// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
		/// <exception cref="ArgumentException">Provided pipe cannot be wrapped. Provided pipe must be setup with the following: PipeDirection - <see cref="PipeDirection.InOut"/>, PipeOptions - <see cref="PipeOptions.Asynchronous"/>, and PipeTransmissionMode - <see cref="PipeTransmissionMode.Byte"/></exception>
		public PipeServer(IPipeSerializer serializer, NamedPipeServerStream rawPipe, Func<THandling> handlerFactoryFunc)
		{
			Utilities.ValidateRawServerPipe(rawPipe);

			this.serializer = serializer;
			this.rawPipeStream = rawPipe;
			this.handlerFactoryFunc = handlerFactoryFunc;
		}

		/// <summary>
		/// Get the raw named pipe. This will automatically create if it hasn't been instantiated yet and is accessed.
		/// </summary>
		public NamedPipeServerStream RawPipe
		{
			get
			{
				if (this.rawPipeStream == null)
				{
					this.CreatePipe();
				}

				return this.rawPipeStream;
			}
		}

		/// <summary>
		/// Gets the state of the pipe.
		/// </summary>
		public PipeState State => this.messageProcessor.State;

		/// <summary>
		/// Sets up the given action as a logger for the module.
		/// </summary>
		/// <param name="logger">The logger action.</param>
		public void SetLogger(Action<string> logger)
		{
			this.logger = logger;
		}

		/// <summary>
		/// Waits for a client to connect to the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <exception cref="IOException">Thrown when the connection fails.</exception>
		public async Task WaitForConnectionAsync(CancellationToken cancellationToken = default)
		{
			if (this.rawPipeStream == null)
			{
				this.CreatePipe();	
			}

			await this.rawPipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

			this.logger.Log(() => "Connected to client.");

			var wrappedPipeStream = new PipeStreamWrapper(this.rawPipeStream, this.logger);
			var requestHandler = new RequestHandler<THandling>(wrappedPipeStream, this.handlerFactoryFunc, this.serializer, this.logger);

			this.messageProcessor.StartProcessing(wrappedPipeStream);
		}

		/// <summary>
		/// Wait for the other end to close the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <exception cref="IOException">Thrown when the pipe has closed due to an unknown error.</exception>
		/// <remarks>This does not throw when the other end closes the pipe.</remarks>
		public Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default)
		{
			return this.messageProcessor.WaitForRemotePipeCloseAsync(cancellationToken);
		}

		/// <summary>
		/// Initialize new named pipe stream with preset options respected
		/// </summary>
		private void CreatePipe()
		{
			PipeOptions pipeOptionsToPass;
			if (this.options == null)
			{
				pipeOptionsToPass = PipeOptions.Asynchronous;
			}
			else
			{
				pipeOptionsToPass = this.options.Value | PipeOptions.Asynchronous;
			}

			this.rawPipeStream = new NamedPipeServerStream(
				this.pipeName,
				PipeDirection.InOut,
				1,
				PipeTransmissionMode.Byte,
				pipeOptionsToPass);

			this.logger.Log(() => $"Set up named pipe server '{this.pipeName}'.");
		}

		#region IDisposable Support
		private bool disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.messageProcessor != null)
					{
						this.messageProcessor.Dispose();
					}

					if (this.rawPipeStream != null)
					{
						this.rawPipeStream.Dispose();
					}
				}

				this.disposed = true;
			}
		}

		/// <summary>
		/// Closes the pipe.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
		}
		#endregion
	}
}
