namespace PipeMethodCalls
{
	/// <summary>
	/// Handles a request message received from a remote endpoint.
	/// </summary>
	internal interface IRequestHandler
	{
		/// <summary>
		/// Handles a request message received from a remote endpoint.
		/// </summary>
		/// <param name="request">The request message.</param>
		void HandleRequest(SerializedPipeRequest request);
	}
}
