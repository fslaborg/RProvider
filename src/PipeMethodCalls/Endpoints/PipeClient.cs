using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// A named pipe client.
	/// </summary>
	/// <typeparam name="TRequesting">The interface that the client will be invoking on the server.</typeparam>
	public class PipeClient<TRequesting> : IPipeClient<TRequesting>, IDisposable
		where TRequesting : class
	{
		private readonly string pipeName;
		private readonly string serverName;
		private readonly PipeOptions? options;
		private readonly TokenImpersonationLevel? impersonationLevel;
		private readonly HandleInheritability? inheritability;
		private readonly IPipeSerializer serializer;
		private NamedPipeClientStream rawPipeStream;
		private PipeStreamWrapper wrappedPipeStream;
		private Action<string> logger;
		private PipeMessageProcessor messageProcessor = new PipeMessageProcessor();

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClient{TRequesting}"/> class.
		/// </summary>
		/// <param name="serializer">
		/// The serializer to use for the pipe. You can include a library like PipeMethodCalls.NetJson and pass in <c>new NetJsonPipeSerializer()</c>.
		/// This will serialize and deserialize method parameters and return values so they can be passed over the pipe.
		/// </param>
		/// <param name="pipeName">The name of the pipe.</param>
		public PipeClient(IPipeSerializer serializer, string pipeName)
			: this(serializer, ".", pipeName)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClient{TRequesting}"/> class.
		/// </summary>
		/// <param name="serializer">
		/// The serializer to use for the pipe. You can include a library like PipeMethodCalls.NetJson and pass in <c>new NetJsonPipeSerializer()</c>.
		/// This will serialize and deserialize method parameters and return values so they can be passed over the pipe.
		/// </param>
		/// <param name="pipeName">The name of the pipe.</param>
		/// <param name="options">One of the enumeration values that determines how to open or create the pipe.</param>
		/// <param name="impersonationLevel">One of the enumeration values that determines the security impersonation level.</param>
		/// <param name="inheritability">One of the enumeration values that determines whether the underlying handle will be inheritable by child processes.</param>
		public PipeClient(IPipeSerializer serializer, string pipeName, PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
			: this(serializer, ".", pipeName, options, impersonationLevel, inheritability)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClient{TRequesting}"/> class.
		/// </summary>
		/// <param name="serializer">
		/// The serializer to use for the pipe. You can include a library like PipeMethodCalls.NetJson and pass in <c>new NetJsonPipeSerializer()</c>.
		/// This will serialize and deserialize method parameters and return values so they can be passed over the pipe.
		/// </param>
		/// <param name="serverName">The name of the server to connect to.</param>
		/// <param name="pipeName">The name of the pipe.</param>
		public PipeClient(IPipeSerializer serializer, string serverName, string pipeName)
		{
			this.serializer = serializer;
			this.pipeName = pipeName;
			this.serverName = serverName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClient{TRequesting}"/> class.
		/// </summary>
		/// <param name="serializer">
		/// The serializer to use for the pipe. You can include a library like PipeMethodCalls.NetJson and pass in <c>new NetJsonPipeSerializer()</c>.
		/// This will serialize and deserialize method parameters and return values so they can be passed over the pipe.
		/// </param>
		/// <param name="serverName">The name of the server to connect to.</param>
		/// <param name="pipeName">The name of the pipe.</param>
		/// <param name="options">One of the enumeration values that determines how to open or create the pipe.</param>
		/// <param name="impersonationLevel">One of the enumeration values that determines the security impersonation level.</param>
		/// <param name="inheritability">One of the enumeration values that determines whether the underlying handle will be inheritable by child processes.</param>
		public PipeClient(IPipeSerializer serializer, string serverName, string pipeName, PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
		{
			this.serializer = serializer;
			this.pipeName = pipeName;
			this.serverName = serverName;
			this.options = options;
			this.impersonationLevel = impersonationLevel;
			this.inheritability = inheritability;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClient{TRequesting}"/> class.
		/// </summary>
		/// <param name="serializer">
		/// The serializer to use for the pipe. You can include a library like PipeMethodCalls.NetJson and pass in <c>new NetJsonPipeSerializer()</c>.
		/// This will serialize and deserialize method parameters and return values so they can be passed over the pipe.
		/// </param>
		/// <param name="rawPipe">Raw pipe stream to wrap with method call capability. Must be set up with PipeDirection - <see cref="PipeDirection.InOut"/> and PipeOptions - <see cref="PipeOptions.Asynchronous"/></param>
		/// <exception cref="ArgumentException">Provided pipe cannot be wrapped. Provided pipe must be setup with the following: PipeDirection - <see cref="PipeDirection.InOut"/> and PipeOptions - <see cref="PipeOptions.Asynchronous"/></exception>
		public PipeClient(IPipeSerializer serializer, NamedPipeClientStream rawPipe)
		{
			Utilities.ValidateRawClientPipe(rawPipe);

			this.serializer = serializer;
			this.rawPipeStream = rawPipe;
		}
		
		/// <summary>
		/// Get the raw named pipe. This will automatically create if it hasn't been instantiated yet and is accessed.
		/// </summary>
		public NamedPipeClientStream RawPipe
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
		/// Gets the method invoker.
		/// </summary>
		/// <remarks>This is null before connecting.</remarks>
		public IPipeInvoker<TRequesting> Invoker { get; private set; }

		/// <summary>
		/// Sets up the given action as a logger for the module.
		/// </summary>
		/// <param name="logger">The logger action.</param>
		public void SetLogger(Action<string> logger)
		{
			this.logger = logger;
		}

		/// <summary>
		/// Connects the pipe to the server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <exception cref="IOException">Thrown when the connection fails.</exception>
		public async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			if (this.State != PipeState.NotOpened)
			{
				throw new InvalidOperationException("Can only call ConnectAsync once");
			}

			if (this.pipeName != null)
			{
				this.logger.Log(() => $"Connecting to named pipe '{this.pipeName}' on machine '{this.serverName}'");
			}
			else
			{
				this.logger.Log(() => $"Connecting to named pipe");
			}

			if (this.rawPipeStream == null)
			{
				this.CreatePipe();	
			}

			await this.rawPipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
			this.logger.Log(() => "Connected.");

			this.wrappedPipeStream = new PipeStreamWrapper(this.rawPipeStream, this.logger);
			this.Invoker = new MethodInvoker<TRequesting>(this.wrappedPipeStream, this.messageProcessor, this.serializer, this.logger);

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
			if (this.options != null)
			{
				this.rawPipeStream = new NamedPipeClientStream(
					this.serverName,
					this.pipeName,
					PipeDirection.InOut,
					this.options.Value | PipeOptions.Asynchronous,
					this.impersonationLevel.Value,
					this.inheritability.Value);
			}
			else
			{
				this.rawPipeStream = new NamedPipeClientStream(this.serverName, this.pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
			}
		}

		#region IDisposable Support
		private bool disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.messageProcessor.Dispose();

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
