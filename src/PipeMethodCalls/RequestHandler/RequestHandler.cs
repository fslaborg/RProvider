using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// Handles incoming method requests over a pipe stream.
	/// </summary>
	/// <typeparam name="THandling">The interface for the method requests.</typeparam>
	internal class RequestHandler<THandling> : IRequestHandler
	{
		private readonly Func<THandling> handlerFactoryFunc;
		private readonly PipeStreamWrapper pipeStreamWrapper;
		private readonly IPipeSerializer serializer;
		private readonly Action<string> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestHandler"/> class.
		/// </summary>
		/// <param name="pipeStreamWrapper">The underlying pipe stream wrapper.</param>
		/// <param name="handlerFactoryFunc"></param>
		/// <param name="serializer">The serializer to use.</param>
		/// <param name="logger">The action to run to log events.</param>
		public RequestHandler(PipeStreamWrapper pipeStreamWrapper, Func<THandling> handlerFactoryFunc, IPipeSerializer serializer, Action<string> logger)
		{
			this.pipeStreamWrapper = pipeStreamWrapper;
			this.handlerFactoryFunc = handlerFactoryFunc;
			this.serializer = serializer;
			this.logger = logger;
			this.pipeStreamWrapper.RequestHandler = this;
		}

		/// <summary>
		/// Handles a request message received from a remote endpoint.
		/// </summary>
		/// <param name="request">The request message.</param>
		public async void HandleRequest(SerializedPipeRequest request)
		{
			try
			{
				SerializedPipeResponse response = await this.HandleSerializedRequestAsync(request).ConfigureAwait(false);
				await this.pipeStreamWrapper.SendResponseAsync(response, CancellationToken.None).ConfigureAwait(false);
			}
			catch (Exception)
			{
				// If the pipe has closed and can't hear the response, we can't let the other end know about it, so we just eat the exception.
			}
		}

		/// <summary>
		/// Handles a request from a remote endpoint and returns a serialized pipe response.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns>The serialized response.</returns>
		private async Task<SerializedPipeResponse> HandleSerializedRequestAsync(SerializedPipeRequest request)
		{
			try
			{
				TypedPipeResponse typedResponse = await this.GetTypedResponseForRequest(request).ConfigureAwait(false);
				SerializedPipeResponse response = typedResponse.Serialize(this.serializer);
				this.logger.Log(() => "Sending " + typedResponse.ToString());
				return response;
			}
			catch (Exception exception)
			{
				this.logger.Log(() => {
					TypedPipeResponse typedResponse = TypedPipeResponse.Failure(request.CallId, exception.ToString());
					return "Sending " + typedResponse.ToString();
				});
				return SerializedPipeResponse.Failure(request.CallId, exception.ToString());
			}
		}

		/// <summary>
		/// Handles a request from a remote endpoint and returns a typed pipe response.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns>The typed response.</returns>
		private async Task<TypedPipeResponse> GetTypedResponseForRequest(SerializedPipeRequest request)
		{
			if (this.handlerFactoryFunc == null)
			{
				return TypedPipeResponse.Failure(request.CallId, $"No handler implementation registered for interface '{typeof(THandling).FullName}' found.");
			}

			THandling handlerInstance = this.handlerFactoryFunc();
			if (handlerInstance == null)
			{
				return TypedPipeResponse.Failure(request.CallId, $"Handler implementation returned null for interface '{typeof(THandling).FullName}'");
			}

			MethodInfo method = handlerInstance.GetType().GetRuntimeMethods().FirstOrDefault(x => x.Name.Split('.').Last() == request.MethodName);
			string methods = String.Concat(handlerInstance.GetType().GetRuntimeMethods().Select(m => m.Name));
			if (method == null)
			{
				return TypedPipeResponse.Failure(request.CallId, $"Method '{request.MethodName}' not found in interface '{typeof(THandling).FullName}'.");
			}

			ParameterInfo[] paramInfoList = method.GetParameters();
			if (paramInfoList.Length != request.Parameters.Length)
			{
				return TypedPipeResponse.Failure(request.CallId, $"Parameter count mismatch for method '{request.MethodName}'.");
			}

			Type[] genericArguments = method.GetGenericArguments();
			if (genericArguments.Length != request.GenericArguments.Length)
			{
				return TypedPipeResponse.Failure(request.CallId, $"Generic argument count mismatch for method '{request.MethodName}'.");
			}

			if (paramInfoList.Any(info => info.IsOut || info.ParameterType.IsByRef))
			{
				return TypedPipeResponse.Failure(request.CallId, $"ref parameters are not supported. Method: '{request.MethodName}'");
			}

			object[] parameters = new object[paramInfoList.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				Type destType = paramInfoList[i].ParameterType;
				if (destType.IsGenericParameter)
				{
					destType = request.GenericArguments[destType.GenericParameterPosition];
				}

				byte[] parameterBytes = request.Parameters[i];
				parameters[i] = this.serializer.Deserialize(parameterBytes, destType);
			}

			try
			{
				// Log - handling request
				this.logger.Log(() =>
				{
					TypedPipeRequest typedRequest = new TypedPipeRequest
					{
						CallId = request.CallId,
						MethodName = request.MethodName,
						Parameters = parameters,
						GenericArguments = request.GenericArguments
					};

					return "Received " + typedRequest.ToString();
				});

				if (method.IsGenericMethod)
				{
					method = method.MakeGenericMethod(request.GenericArguments);
				}

				object resultOrTask = method.Invoke(handlerInstance, parameters);
				object result;

				if (resultOrTask is Task)
				{
					await ((Task)resultOrTask).ConfigureAwait(false);

					var resultProperty = resultOrTask.GetType().GetProperty("Result");
					result = resultProperty?.GetValue(resultOrTask);
				}
				else
				{
					result = resultOrTask;
				}

				return TypedPipeResponse.Success(request.CallId, result);
			}
			catch (Exception exception)
			{
				return TypedPipeResponse.Failure(request.CallId, exception.ToString());
			}
		}
	}
}
